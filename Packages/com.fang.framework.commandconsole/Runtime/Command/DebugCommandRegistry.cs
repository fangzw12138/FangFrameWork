#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Fang.Framework.CommandConsole
{
    /// <summary>
    /// 指令注册表：登记条目、执行、记调用日志。
    /// 纯 C#（只在警告时写 Console），不依赖场景 / 服务实例，可以直接单测。
    /// 执行入口统一把结果序列化成 <c>{ok:true,data}</c> / <c>{ok:false,error}</c>。
    /// </summary>
    public sealed class DebugCommandRegistry
    {
        /// <summary>一次指令调用记录（可观测性：<c>command_log</c> 指令与编辑器控制台的日志区都用它）。</summary>
        public sealed class CallLogEntry
        {
            public string Time;
            public string Cmd;
            public string[] Args;
            public bool Ok;
            public string Summary;
        }

        private readonly Dictionary<string, DebugCommandEntry> _commands =
            new Dictionary<string, DebugCommandEntry>(StringComparer.Ordinal);

        private readonly List<CallLogEntry> _callLog = new List<CallLogEntry>();

        private List<DebugCommandEntry> _sortedCommands;

        /// <summary>
        /// 上下文工厂：每次执行指令时调一次。
        /// 默认 = 没有任何 Scope 的默认上下文；<see cref="DebugCommandService"/> 会把它换成
        /// 「宿主服务所在的 Scope」，工程也可以换成自己的上下文子类。
        /// </summary>
        public Func<DebugCommandContext> ContextFactory { get; set; }

        /// <summary>已登记指令，按名称（序数）排序 —— 顺序确定，方便展示与测试。</summary>
        public IReadOnlyList<DebugCommandEntry> Commands
        {
            get
            {
                if (_sortedCommands == null)
                {
                    _sortedCommands = new List<DebugCommandEntry>(_commands.Values);
                    _sortedCommands.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
                }

                return _sortedCommands;
            }
        }

        public int Count => _commands.Count;

        /// <summary>扫描并登记一批程序集；返回扫到的条目数（不含被覆盖的旧条目）。</summary>
        public int Scan(IEnumerable<Assembly> assemblies, IEnumerable<string> assemblyNamePrefixes = null)
        {
            var entries = DebugCommandScanner.Scan(assemblies, assemblyNamePrefixes, Warn);
            for (var i = 0; i < entries.Count; i++)
            {
                Register(entries[i]);
            }

            return entries.Count;
        }

        /// <summary>
        /// 按默认范围扫描并登记：程序集前缀白名单为空时 = 只扫「引用了本包」的程序集
        /// （快得多，且扫到的指令一致，理由见 <see cref="DebugCommandScanner.ScanReferencingAssemblies"/>）；
        /// 白名单非空时 = 在全部已加载程序集里按前缀过滤。
        /// </summary>
        public int ScanLoadedAssemblies(IEnumerable<string> assemblyNamePrefixes = null)
        {
            var prefixes = assemblyNamePrefixes == null ? null : new List<string>(assemblyNamePrefixes);

            if (prefixes == null || prefixes.Count == 0)
            {
                return ScanLoadedAssembliesCore(null);
            }

            return ScanLoadedAssembliesCore(prefixes);
        }

        private int ScanLoadedAssembliesCore(IReadOnlyList<string> prefixes)
        {
            var entries = prefixes == null
                ? DebugCommandScanner.ScanReferencingAssemblies(AppDomain.CurrentDomain.GetAssemblies(), Warn)
                : DebugCommandScanner.Scan(AppDomain.CurrentDomain.GetAssemblies(), prefixes, Warn);

            for (var i = 0; i < entries.Count; i++)
            {
                Register(entries[i]);
            }

            return entries.Count;
        }

        /// <summary>登记一条指令（手工注册用；重名覆盖并警告）。</summary>
        public void Register(DebugCommandEntry entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.Name))
            {
                return;
            }

            if (_commands.TryGetValue(entry.Name, out var existing) && existing.Source != entry.Source)
            {
                Warn($"[DebugCommand] 指令重名 '{entry.Name}' 被覆盖（原: {existing.Source}，新: {entry.Source}）");
            }

            _commands[entry.Name] = entry;
            _sortedCommands = null;
        }

        /// <summary>登记一条指令（直接给方法）。</summary>
        public void Register(string name, string description, string category, MethodInfo method)
        {
            Register(new DebugCommandEntry(name, description, category, method));
        }

        /// <summary>清空指令（调用日志保留）。</summary>
        public void Clear()
        {
            _commands.Clear();
            _sortedCommands = null;
        }

        /// <summary>按名称查指令；不存在返回 null。</summary>
        public DebugCommandEntry Find(string name)
        {
            return !string.IsNullOrEmpty(name) && _commands.TryGetValue(name, out var entry) ? entry : null;
        }

        /// <summary>
        /// 执行指令：统一捕获异常并序列化成响应。
        /// 指令方法签名是 <c>static object M(DebugCommandContext, DebugCommandArgs)</c>（扫描阶段已校验）。
        /// </summary>
        public JObject Execute(string commandName, string[] args)
        {
            if (string.IsNullOrEmpty(commandName) || !_commands.TryGetValue(commandName, out var entry))
            {
                var missing = $"未知指令: '{commandName}'（用 list_commands 查看全部指令）";
                Record(commandName, args, false, missing);
                return Error(missing);
            }

            try
            {
                var context = CreateContext();
                var argBox = new DebugCommandArgs(args);
                var result = entry.Method.Invoke(null, new object[] { context, argBox });
                var data = result == null ? JValue.CreateNull() : JToken.FromObject(result);
                Record(commandName, args, true, data.ToString(Formatting.None));
                return new JObject { ["ok"] = true, ["data"] = data };
            }
            catch (TargetInvocationException tie)
            {
                var inner = tie.InnerException;

                if (inner is DebugCommandException business)
                {
                    Record(commandName, args, false, business.Message);
                    return Error(business.Message);
                }

                Debug.LogError($"[DebugCommand] 指令 '{commandName}' 执行异常: {inner}");
                var message = $"指令执行异常: {inner?.Message ?? tie.Message}";
                Record(commandName, args, false, message);
                return Error(message);
            }
            catch (Exception e)
            {
                Debug.LogError($"[DebugCommand] 指令 '{commandName}' 调用失败: {e}");
                var message = $"指令调用失败: {e.Message}";
                Record(commandName, args, false, message);
                return Error(message);
            }
        }

        /// <summary>执行并直接拿到序列化 JSON（进程内 / 测试用，调用方不必引用 Newtonsoft 类型）。</summary>
        public string ExecuteJson(string commandName, string[] args)
        {
            return Execute(commandName, args).ToString(Formatting.None);
        }

        /// <summary>
        /// 执行「指令名 + 空格分隔参数」的一行文本（控制台里手工输入用）。
        /// 刻意不做引号 / 转义解析：手打的一行不值得引入一个 shell 语法，需要复杂参数就直接走 TCP 的 JSON 协议。
        /// </summary>
        public string ExecuteText(string commandLine)
        {
            if (string.IsNullOrWhiteSpace(commandLine))
            {
                return Error("指令为空").ToString(Formatting.None);
            }

            var parts = commandLine.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return Error("指令为空").ToString(Formatting.None);
            }

            var args = new string[parts.Length - 1];
            for (var i = 1; i < parts.Length; i++)
            {
                args[i - 1] = parts[i];
            }

            return ExecuteJson(parts[0], args);
        }

        /// <summary>最近调用记录，最新在前，最多 <see cref="DebugCommandDefaults.CallLogMaxSize"/> 条。</summary>
        public IReadOnlyList<CallLogEntry> GetCallLog()
        {
            var copy = new CallLogEntry[_callLog.Count];
            for (var i = 0; i < _callLog.Count; i++)
            {
                copy[i] = _callLog[_callLog.Count - 1 - i];
            }

            return copy;
        }

        public void ClearCallLog()
        {
            _callLog.Clear();
        }

        internal static JObject Error(string message)
        {
            return new JObject { ["ok"] = false, ["error"] = message };
        }

        private DebugCommandContext CreateContext()
        {
            var factory = ContextFactory;
            return factory != null ? factory() : new DebugCommandContext(this, null);
        }

        /// <summary>
        /// 记一条调用日志（环形缓冲，超限丢最旧）。
        /// 刻意不打 Console —— Console 留给代码自己的 debug 日志，指令记录去 <c>command_log</c> 看。
        /// </summary>
        private void Record(string commandName, string[] args, bool ok, string summary)
        {
            _callLog.Add(new CallLogEntry
            {
                Time = DateTime.Now.ToString("HH:mm:ss.fff"),
                Cmd = commandName ?? string.Empty,
                Args = args != null && args.Length > 0 ? (string[])args.Clone() : Array.Empty<string>(),
                Ok = ok,
                Summary = Truncate(summary),
            });

            if (_callLog.Count > DebugCommandDefaults.CallLogMaxSize)
            {
                _callLog.RemoveAt(0);
            }
        }

        private static string Truncate(string summary)
        {
            if (string.IsNullOrEmpty(summary))
            {
                return string.Empty;
            }

            return summary.Length > DebugCommandDefaults.SummaryMaxLength
                ? summary.Substring(0, DebugCommandDefaults.SummaryMaxLength) + "...(截断)"
                : summary;
        }

        private static void Warn(string message)
        {
            Debug.LogWarning(message);
        }
    }
}
#endif
