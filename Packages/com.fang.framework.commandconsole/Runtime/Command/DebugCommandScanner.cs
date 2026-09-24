#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Fang.Framework.CommandConsole
{
    /// <summary>
    /// 纯逻辑扫描器：把「程序集集合」变成「指令条目列表」。
    /// 运行时（<see cref="DebugCommandService"/>）与编辑器文档生成器都调它 —— 单一来源，两侧清单永远一致。
    /// 不碰场景、不写 Console（要日志就传 <c>warn</c> 回调），所以可以直接单测。
    /// </summary>
    public static class DebugCommandScanner
    {
        /// <summary>扫描给定程序集。程序集名前缀白名单为空 = 全部扫；重名后出现的覆盖先出现的。</summary>
        /// <param name="assemblies">要扫的程序集（null 当空处理）。</param>
        /// <param name="assemblyNamePrefixes">程序集名前缀白名单；null / 空 = 不过滤。</param>
        /// <param name="warn">警告回调（签名不符 / 空名 / 重名 / 读程序集失败）；null = 静默。</param>
        /// <returns>按名称（序数）排序的条目列表。</returns>
        public static IReadOnlyList<DebugCommandEntry> Scan(
            IEnumerable<Assembly> assemblies,
            IEnumerable<string> assemblyNamePrefixes = null,
            Action<string> warn = null)
        {
            var byName = new Dictionary<string, DebugCommandEntry>(StringComparer.Ordinal);

            if (assemblies == null)
            {
                return Array.Empty<DebugCommandEntry>();
            }

            var prefixes = NormalizePrefixes(assemblyNamePrefixes);

            foreach (var assembly in assemblies)
            {
                if (assembly == null || !MatchesPrefixes(assembly, prefixes))
                {
                    continue;
                }

                foreach (var type in GetTypes(assembly, warn))
                {
                    if (type == null)
                    {
                        continue;
                    }

                    foreach (var method in GetMethods(type))
                    {
                        var attribute = method.GetCustomAttribute<DebugCommandAttribute>();
                        if (attribute == null)
                        {
                            continue;
                        }

                        Collect(attribute, method, byName, warn);
                    }
                }
            }

            var result = new List<DebugCommandEntry>(byName.Values);
            result.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            return result;
        }

        /// <summary>扫描当前已加载的<b>引用本包</b>的程序集（默认范围，见 <see cref="ScanReferencingAssemblies"/>）。</summary>
        public static IReadOnlyList<DebugCommandEntry> ScanReferencingAssemblies(Action<string> warn = null)
        {
            return ScanReferencingAssemblies(AppDomain.CurrentDomain.GetAssemblies(), warn);
        }

        /// <summary>
        /// 默认扫描范围：从给定程序集里挑出「引用了本包（或就是本包）」的那些再扫。
        ///
        /// 为什么不用「全部已加载」：本仓库实测（Unity 6000.3.2f1，编辑器里 250 个已加载程序集）
        /// 全量扫要 <b>3291 ms</b>，按引用过滤后是 <b>0 ms</b>，而两次扫到的指令<b>完全一致</b>。
        /// 之所以一致：C# 里写 <c>[DebugCommand]</c> 必须能解析到这个类型，所以使用者程序集
        /// 必然直接引用本程序集。唯一漏网的是「用 TypeForwardedTo 把特性转发出去」这种写法，实践中不存在；
        /// 真要全量扫，用 <see cref="Scan"/> 自己传程序集列表。
        /// </summary>
        public static IReadOnlyList<DebugCommandEntry> ScanReferencingAssemblies(
            IEnumerable<Assembly> assemblies,
            Action<string> warn = null)
        {
            if (assemblies == null)
            {
                return Array.Empty<DebugCommandEntry>();
            }

            var candidates = new List<Assembly>();
            foreach (var assembly in assemblies)
            {
                if (assembly != null && ReferencesPackage(assembly))
                {
                    candidates.Add(assembly);
                }
            }

            return Scan(candidates, null, warn);
        }

        /// <summary>这个程序集是否引用了本包（或就是本包）。</summary>
        public static bool ReferencesPackage(Assembly assembly)
        {
            if (assembly == null)
            {
                return false;
            }

            var self = typeof(DebugCommandScanner).Assembly;
            if (assembly == self)
            {
                return true;
            }

            var selfName = self.GetName().Name;

            try
            {
                var references = assembly.GetReferencedAssemblies();
                for (var i = 0; i < references.Length; i++)
                {
                    if (string.Equals(references[i].Name, selfName, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                // 动态程序集等读引用会抛；当作「不引用」。
            }

            return false;
        }

        /// <summary>签名是否是指令约定：<c>static M(DebugCommandContext, DebugCommandArgs)</c>。</summary>
        public static bool HasCommandSignature(MethodInfo method)
        {
            if (method == null || !method.IsStatic || method.IsGenericMethodDefinition)
            {
                return false;
            }

            var parameters = method.GetParameters();
            return parameters.Length == 2
                && parameters[0].ParameterType == typeof(DebugCommandContext)
                && parameters[1].ParameterType == typeof(DebugCommandArgs);
        }

        /// <summary>把签名渲染成可读文本（警告与校验器共用）。</summary>
        public static string DescribeSignature(MethodInfo method)
        {
            if (method == null)
            {
                return "(null)";
            }

            var parts = new List<string>();
            var parameters = method.GetParameters();
            for (var i = 0; i < parameters.Length; i++)
            {
                parts.Add(parameters[i].ParameterType.Name);
            }

            var prefix = method.IsStatic ? "static " : string.Empty;
            return prefix + method.ReturnType.Name + " " + method.Name + "(" + string.Join(", ", parts) + ")";
        }

        private static void Collect(
            DebugCommandAttribute attribute,
            MethodInfo method,
            Dictionary<string, DebugCommandEntry> byName,
            Action<string> warn)
        {
            var source = DescribeSource(method);
            var label = string.IsNullOrWhiteSpace(attribute.Name)
                ? "(空名)"
                : "'" + attribute.Name.Trim() + "'";

            if (!HasCommandSignature(method))
            {
                warn?.Invoke(
                    $"[DebugCommand] 跳过 {label}（{source}）：签名必须是 static (DebugCommandContext, DebugCommandArgs)，"
                    + $"实际是 {DescribeSignature(method)}");
                return;
            }

            if (string.IsNullOrWhiteSpace(attribute.Name))
            {
                warn?.Invoke($"[DebugCommand] 跳过 {source}：指令名是空的。");
                return;
            }

            var name = attribute.Name.Trim();

            if (string.IsNullOrWhiteSpace(attribute.Description))
            {
                warn?.Invoke($"[DebugCommand] 指令 '{name}'（{source}）没有描述；调用方看不到它做什么。");
            }

            var entry = new DebugCommandEntry(name, attribute.Description, attribute.Category, method);

            if (byName.TryGetValue(name, out var existing) && existing.Source != entry.Source)
            {
                warn?.Invoke($"[DebugCommand] 指令重名 '{name}' 被覆盖（原: {existing.Source}，新: {entry.Source}）");
            }

            byName[name] = entry;
        }

        private static string DescribeSource(MethodInfo method)
        {
            if (method == null)
            {
                return "(null)";
            }

            var type = method.DeclaringType;
            return type == null ? method.Name : type.FullName + "." + method.Name;
        }

        private static MethodInfo[] GetMethods(Type type)
        {
            try
            {
                // 刻意连 Instance 一起枚举：只枚举 static 的话，「把 [DebugCommand] 贴到非 static 方法上」
                // 会变成静默失效 —— 那正是本包要消灭的失败方式。多出来的实例方法没有特性，代价只是多几次
                // GetCustomAttribute。
                // DeclaredOnly：静态方法不会被继承使用，加上它可以避免同一个方法在基类/派生类上被重复扫到。
                return type.GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic
                    | BindingFlags.Static | BindingFlags.Instance
                    | BindingFlags.DeclaredOnly);
            }
            catch (Exception)
            {
                // 某些动态类型 / 泛型参数类型读成员会抛；跳过即可。
                return Array.Empty<MethodInfo>();
            }
        }

        private static IReadOnlyList<Type> GetTypes(Assembly assembly, Action<string> warn)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                warn?.Invoke($"[DebugCommand] 读取程序集 '{assembly.GetName().Name}' 的部分类型失败，已跳过它们：{e.Message}");

                var loaded = new List<Type>();
                foreach (var type in e.Types)
                {
                    if (type != null)
                    {
                        loaded.Add(type);
                    }
                }

                return loaded;
            }
            catch (Exception e)
            {
                warn?.Invoke($"[DebugCommand] 读取程序集 '{assembly.GetName().Name}' 失败，已跳过：{e.Message}");
                return Array.Empty<Type>();
            }
        }

        private static string[] NormalizePrefixes(IEnumerable<string> prefixes)
        {
            if (prefixes == null)
            {
                return Array.Empty<string>();
            }

            var result = new List<string>();
            foreach (var prefix in prefixes)
            {
                if (!string.IsNullOrWhiteSpace(prefix))
                {
                    result.Add(prefix.Trim());
                }
            }

            return result.ToArray();
        }

        private static bool MatchesPrefixes(Assembly assembly, string[] prefixes)
        {
            if (prefixes.Length == 0)
            {
                return true;
            }

            var name = assembly.GetName().Name ?? string.Empty;
            for (var i = 0; i < prefixes.Length; i++)
            {
                if (name.StartsWith(prefixes[i], StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
#endif
