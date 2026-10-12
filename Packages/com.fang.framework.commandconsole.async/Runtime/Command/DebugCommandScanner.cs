#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Fang.Framework.CommandConsole.Async
{
    /// <summary>
    /// 指令扫描器：只认「public static + 返回 object + 形参精确 (DebugCommandContext, DebugCommandArgs)」的方法；
    /// 不合规的一律不注册，并通过 <c>warn</c> 回调给出一条带原因的消息。
    /// </summary>
    public static class DebugCommandScanner
    {
        /// <summary>本包的程序集名；「引用了本包」就是判定扫描范围用的锚点。</summary>
        public const string PackageAssemblyName = "Fang.Framework.CommandConsole.Async";

        /// <summary>给定程序集里是否有非空前缀。</summary>
        public static bool HasAnyPrefix(IEnumerable<string> prefixes)
        {
            return NormalizePrefixes(prefixes).Count > 0;
        }

        /// <summary>
        /// 是否是测试程序集（引用了 <c>nunit.framework</c>）。
        /// 默认扫描范围会把它排除掉，免得测试固定装置混进指令清单与文档。
        /// </summary>
        public static bool IsTestAssembly(Assembly assembly)
        {
            if (assembly == null)
            {
                return false;
            }

            try
            {
                foreach (var reference in assembly.GetReferencedAssemblies())
                {
                    if (string.Equals(reference.Name, "nunit.framework", StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                // 动态程序集等拿不到引用表，视为不是测试程序集。
            }

            return false;
        }

        /// <summary>
        /// 扫描指定程序集。前缀为空时不做前缀过滤（调用方通常已用 <see cref="ScanReferencingAssemblies"/> 收窄过范围）。
        /// </summary>
        public static List<DebugCommandEntry> Scan(IEnumerable<Assembly> assemblies, IEnumerable<string> prefixes, Action<string> warn)
        {
            var entries = new List<DebugCommandEntry>();

            if (assemblies == null)
            {
                return entries;
            }

            var prefixList = NormalizePrefixes(prefixes);
            var indexByName = new Dictionary<string, int>(StringComparer.Ordinal);
            var scanned = new HashSet<string>(StringComparer.Ordinal);

            foreach (var assembly in OrderAssemblies(assemblies))
            {
                var assemblyName = assembly.GetName().Name;

                if (!scanned.Add(assemblyName))
                {
                    continue;
                }

                if (prefixList.Count > 0 && !MatchesPrefix(assemblyName, prefixList))
                {
                    continue;
                }

                foreach (var type in GetLoadableTypes(assembly, warn))
                {
                    ScanType(type, entries, indexByName, warn);
                }
            }

            return entries;
        }

        /// <summary>
        /// 扫描「引用了本包程序集」的程序集（本包自身也算，内置指令就住在里面）。
        /// </summary>
        public static List<DebugCommandEntry> ScanReferencingAssemblies(IEnumerable<Assembly> assemblies, Action<string> warn)
        {
            if (assemblies == null)
            {
                return new List<DebugCommandEntry>();
            }

            var candidates = new List<Assembly>();

            foreach (var assembly in assemblies)
            {
                if (assembly == null)
                {
                    continue;
                }

                if (IsPackageAssembly(assembly) || ReferencesPackage(assembly))
                {
                    candidates.Add(assembly);
                }
            }

            return Scan(candidates, null, warn);
        }

        private static void ScanType(Type type, List<DebugCommandEntry> entries, Dictionary<string, int> indexByName, Action<string> warn)
        {
            MethodInfo[] methods;

            try
            {
                // 刻意把 Instance / NonPublic 也枚举出来：这样「标了特性但忘了 public/static」的方法
                // 会得到一条明确的警告，而不是被静默忽略。
                methods = type.GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            }
            catch (Exception exception)
            {
                Warn(warn, $"类型 '{type.FullName}' 的方法枚举失败：{exception.Message}");
                return;
            }

            foreach (var method in methods)
            {
                DebugCommandAttribute attribute;

                try
                {
                    attribute = method.GetCustomAttribute<DebugCommandAttribute>(false);
                }
                catch (Exception exception)
                {
                    Warn(warn, $"方法 '{Describe(method)}' 读取 [DebugCommand] 失败：{exception.Message}");
                    continue;
                }

                if (attribute == null)
                {
                    continue;
                }

                if (!TryBuildEntry(attribute, method, warn, out var entry))
                {
                    continue;
                }

                if (indexByName.TryGetValue(entry.Name, out var existingIndex))
                {
                    Warn(warn, $"指令名 '{entry.Name}' 重复：'{entries[existingIndex].Source}' 被 '{entry.Source}' 覆盖。");
                    entries[existingIndex] = entry;
                }
                else
                {
                    indexByName[entry.Name] = entries.Count;
                    entries.Add(entry);
                }
            }
        }

        private static bool TryBuildEntry(DebugCommandAttribute attribute, MethodInfo method, Action<string> warn, out DebugCommandEntry entry)
        {
            entry = null;

            var source = Describe(method);

            if (string.IsNullOrWhiteSpace(attribute.Name))
            {
                Warn(warn, $"{source} 的 [DebugCommand] 未指定名称，已跳过。");
                return false;
            }

            if (!method.IsPublic)
            {
                Warn(warn, $"指令 '{attribute.Name}'（{source}）不是 public，无法调用，已跳过。");
                return false;
            }

            if (!method.IsStatic)
            {
                Warn(warn, $"指令 '{attribute.Name}'（{source}）不是 static，无法调用，已跳过。");
                return false;
            }

            if (method.IsGenericMethodDefinition)
            {
                Warn(warn, $"指令 '{attribute.Name}'（{source}）是泛型方法，无法调用，已跳过。");
                return false;
            }

            if (method.ReturnType != typeof(object))
            {
                Warn(warn, $"指令 '{attribute.Name}'（{source}）返回类型是 '{method.ReturnType.Name}'，必须精确为 object，已跳过。");
                return false;
            }

            var parameters = method.GetParameters();

            if (parameters.Length != 2 ||
                parameters[0].ParameterType != typeof(DebugCommandContext) ||
                parameters[1].ParameterType != typeof(DebugCommandArgs))
            {
                Warn(warn, $"指令 '{attribute.Name}'（{source}）签名必须精确是 (DebugCommandContext, DebugCommandArgs)，实际是 ({DescribeParameters(parameters)})，已跳过。");
                return false;
            }

            entry = new DebugCommandEntry(attribute.Name, attribute.Description, attribute.Category, method);
            return true;
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly, Action<string> warn)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                Warn(warn, $"程序集 '{assembly.GetName().Name}' 部分类型加载失败：{Describe(exception)}");
                return exception.Types.Where(type => type != null);
            }
            catch (Exception exception)
            {
                Warn(warn, $"程序集 '{assembly.GetName().Name}' 无法枚举类型：{exception.Message}");
                return Enumerable.Empty<Type>();
            }
        }

        private static List<string> NormalizePrefixes(IEnumerable<string> prefixes)
        {
            var result = new List<string>();

            if (prefixes == null)
            {
                return result;
            }

            foreach (var prefix in prefixes)
            {
                if (string.IsNullOrWhiteSpace(prefix))
                {
                    continue;
                }

                var trimmed = prefix.Trim();

                if (!result.Contains(trimmed))
                {
                    result.Add(trimmed);
                }
            }

            return result;
        }

        private static bool MatchesPrefix(string assemblyName, List<string> prefixes)
        {
            for (var i = 0; i < prefixes.Count; i++)
            {
                if (assemblyName.StartsWith(prefixes[i], StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<Assembly> OrderAssemblies(IEnumerable<Assembly> assemblies)
        {
            return assemblies
                .Where(assembly => assembly != null)
                .OrderBy(assembly => assembly.GetName().Name, StringComparer.Ordinal);
        }

        private static bool IsPackageAssembly(Assembly assembly)
        {
            return string.Equals(assembly.GetName().Name, PackageAssemblyName, StringComparison.Ordinal);
        }

        private static bool ReferencesPackage(Assembly assembly)
        {
            try
            {
                foreach (var reference in assembly.GetReferencedAssemblies())
                {
                    if (string.Equals(reference.Name, PackageAssemblyName, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                // 动态程序集等拿不到引用表，视为不引用。
            }

            return false;
        }

        private static string Describe(MethodInfo method)
        {
            return method.DeclaringType == null ? method.Name : method.DeclaringType.Name + "." + method.Name;
        }

        private static string DescribeParameters(ParameterInfo[] parameters)
        {
            if (parameters == null || parameters.Length == 0)
            {
                return string.Empty;
            }

            return string.Join(", ", parameters.Select(parameter => parameter.ParameterType.Name));
        }

        private static string Describe(ReflectionTypeLoadException exception)
        {
            var messages = exception.LoaderExceptions
                .Where(loaderException => loaderException != null)
                .Select(loaderException => loaderException.Message)
                .Distinct()
                .ToArray();

            return messages.Length == 0 ? exception.Message : string.Join(" | ", messages);
        }

        private static void Warn(Action<string> warn, string message)
        {
            warn?.Invoke(message);
        }
    }
}
#endif
