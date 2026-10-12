#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Fang.Framework.CommandConsole.Async
{
    /// <summary>
    /// 指令注册表：注册 / 查找 / 执行 / 调用日志。
    /// 执行结果统一是 <c>{ok:true,data}</c> 或 <c>{ok:false,error}</c>。
    /// </summary>
    public sealed class DebugCommandRegistry
    {
        /// <summary>日志前缀。</summary>
        public const string LogTag = "[Fang.Framework.CommandConsole.Async]";

        /// <summary>调用日志的环形缓冲上限。</summary>
        public const int CallLogCapacity = 200;

        private const int PreviewLength = 240;
        private static readonly char[] WhiteSpaceSeparators = { ' ', '\t', '\r', '\n' };

        private readonly Dictionary<string, DebugCommandEntry> _commands = new Dictionary<string, DebugCommandEntry>(StringComparer.Ordinal);
        private readonly List<DebugCommandEntry> _ordered = new List<DebugCommandEntry>();
        private readonly LinkedList<DebugCommandCallRecord> _callLog = new LinkedList<DebugCommandCallRecord>();

        private bool _orderedDirty = true;
        private int _sequence;

        /// <summary>
        /// 上下文工厂。服务会在初始化时把它换成「以装它的那个异步 Scope 为起点」的异步上下文。
        /// 为 null 时执行会返回 <c>{ok:false,error:"指令上下文不可用…"}</c>。
        /// </summary>
        public Func<DebugCommandContext> ContextFactory { get; set; }

        /// <summary>已注册指令，按名称 Ordinal 升序（惰性排序并缓存）。</summary>
        public IReadOnlyList<DebugCommandEntry> Commands
        {
            get
            {
                RebuildOrderIfDirty();
                return _ordered;
            }
        }

        /// <summary>已注册指令数。</summary>
        public int Count => _commands.Count;

        /// <summary>
        /// 重新扫描已加载程序集并替换现有注册表。
        /// 前缀为空 → 只扫「引用了本包程序集、且不是测试程序集」的程序集；前缀非空 → 按前缀过滤（保留测试程序集，因为是调用方显式指定的）。
        /// </summary>
        /// <returns>扫描后的指令数。</returns>
        public int ScanLoadedAssemblies(IEnumerable<string> prefixes = null)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            var entries = DebugCommandScanner.HasAnyPrefix(prefixes)
                ? DebugCommandScanner.Scan(assemblies, prefixes, Warn)
                : DebugCommandScanner.ScanReferencingAssemblies(
                    assemblies.Where(assembly => !DebugCommandScanner.IsTestAssembly(assembly)),
                    Warn);

            Clear();

            for (var i = 0; i < entries.Count; i++)
            {
                Register(entries[i]);
            }

            return _commands.Count;
        }

        /// <summary>
        /// 登记一条指令。同名覆盖（去重警告由 <see cref="DebugCommandScanner"/> 负责，这里只保证「后来的赢」）。
        /// </summary>
        public void Register(DebugCommandEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            _commands[entry.Name] = entry;
            _orderedDirty = true;
        }

        /// <summary>按名查找；找不到返回 null。</summary>
        public DebugCommandEntry Find(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            return _commands.TryGetValue(name, out var entry) ? entry : null;
        }

        /// <summary>清空注册表（不清调用日志）。</summary>
        public void Clear()
        {
            _commands.Clear();
            _ordered.Clear();
            _orderedDirty = true;
        }

        /// <summary>
        /// 执行一条指令。
        /// <see cref="DebugCommandException"/> → <c>{ok:false,error}</c> 且不打 Error 日志；
        /// 其它异常 → 打 Error 日志后返回 <c>{ok:false,error}</c>。
        /// </summary>
        public JObject Execute(string name, string[] args)
        {
            var stopwatch = Stopwatch.StartNew();
            JObject result;

            if (string.IsNullOrWhiteSpace(name))
            {
                result = Failure("指令名不能为空。");
            }
            else if (!_commands.TryGetValue(name, out var entry))
            {
                result = Failure($"未知指令：{name}");
            }
            else
            {
                result = Invoke(entry, args);
            }

            stopwatch.Stop();
            RecordCall(name, args, result, stopwatch.Elapsed.TotalMilliseconds);

            return result;
        }

        /// <summary>执行一行「空格分隔」的纯文本指令，返回单行 JSON。</summary>
        public string ExecuteText(string line)
        {
            var tokens = SplitTokens(line);

            if (tokens.Count == 0)
            {
                return Failure("指令名不能为空。").ToString(Formatting.None);
            }

            var args = new string[tokens.Count - 1];

            for (var i = 1; i < tokens.Count; i++)
            {
                args[i - 1] = tokens[i];
            }

            return Execute(tokens[0], args).ToString(Formatting.None);
        }

        /// <summary>执行一条指令，返回单行 JSON。</summary>
        public string ExecuteJson(string name, string[] args)
        {
            return Execute(name, args).ToString(Formatting.None);
        }

        /// <summary>
        /// 传输层入口：同时接受 NDJSON（<c>{"cmd":"ping","args":[]}</c>）与纯文本行（<c>ping a b</c>），返回单行 JSON。
        /// </summary>
        public string ExecuteRequest(string line)
        {
            var trimmed = line?.Trim();

            if (string.IsNullOrEmpty(trimmed))
            {
                return Failure("请求为空。").ToString(Formatting.None);
            }

            if (trimmed[0] != '{')
            {
                return ExecuteText(trimmed);
            }

            try
            {
                var request = JObject.Parse(trimmed);
                var command = request.Value<string>("cmd");

                if (string.IsNullOrWhiteSpace(command))
                {
                    command = request.Value<string>("command");
                }

                return ExecuteJson(command, ReadArgs(request["args"]));
            }
            catch (JsonException exception)
            {
                return Failure($"请求 JSON 解析失败：{exception.Message}").ToString(Formatting.None);
            }
        }

        /// <summary>调用日志，最新在前。</summary>
        public IReadOnlyList<DebugCommandCallRecord> GetCallLog()
        {
            var records = new DebugCommandCallRecord[_callLog.Count];
            var index = 0;

            for (var node = _callLog.First; node != null; node = node.Next)
            {
                records[index++] = node.Value;
            }

            return records;
        }

        /// <summary>清空调用日志。</summary>
        public void ClearCallLog()
        {
            _callLog.Clear();
        }

        private JObject Invoke(DebugCommandEntry entry, string[] args)
        {
            var context = ContextFactory?.Invoke();

            if (context == null)
            {
                return Failure("指令上下文不可用：当前没有可用的异步 Scope。");
            }

            try
            {
                return Success(entry.Invoke(context, new DebugCommandArgs(args)));
            }
            catch (DebugCommandException business)
            {
                return Failure(business.Message);
            }
            catch (TargetInvocationException wrapper) when (wrapper.InnerException is DebugCommandException business)
            {
                return Failure(business.Message);
            }
            catch (TargetInvocationException wrapper)
            {
                var inner = wrapper.InnerException ?? wrapper;
                Debug.LogError($"{LogTag} 指令 '{entry.Name}' 执行失败（{entry.Source}）：{inner}");
                return Failure(inner.Message);
            }
            catch (Exception exception)
            {
                Debug.LogError($"{LogTag} 指令 '{entry.Name}' 执行失败（{entry.Source}）：{exception}");
                return Failure(exception.Message);
            }
        }

        private static JObject Success(object data)
        {
            return new JObject
            {
                ["ok"] = true,
                ["data"] = data == null ? JValue.CreateNull() : data as JToken ?? JToken.FromObject(data)
            };
        }

        private static JObject Failure(string error)
        {
            return new JObject
            {
                ["ok"] = false,
                ["error"] = error ?? "未知错误。"
            };
        }

        private static string[] ReadArgs(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
            {
                return Array.Empty<string>();
            }

            if (token is JArray array)
            {
                var values = new string[array.Count];

                for (var i = 0; i < array.Count; i++)
                {
                    var item = array[i];
                    values[i] = item == null || item.Type == JTokenType.Null ? null : item.ToString();
                }

                return values;
            }

            // 兼容把参数写成字符串的写法：{"cmd":"ping","args":"a b"}
            var tokens = SplitTokens(token.ToString());
            return tokens.ToArray();
        }

        private static List<string> SplitTokens(string line)
        {
            var tokens = new List<string>();

            if (string.IsNullOrEmpty(line))
            {
                return tokens;
            }

            var parts = line.Split(WhiteSpaceSeparators, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < parts.Length; i++)
            {
                tokens.Add(parts[i]);
            }

            return tokens;
        }

        private void RecordCall(string name, string[] args, JObject result, double elapsedMs)
        {
            var succeeded = result["ok"] != null && result["ok"].Type == JTokenType.Boolean && result["ok"].Value<bool>();

            var record = new DebugCommandCallRecord(
                ++_sequence,
                DateTime.Now,
                string.IsNullOrEmpty(name) ? "(空)" : name,
                args == null || args.Length == 0 ? string.Empty : string.Join(" ", args),
                succeeded,
                succeeded ? null : result["error"]?.ToString(),
                elapsedMs,
                Preview(result.ToString(Formatting.None)));

            _callLog.AddFirst(record);

            while (_callLog.Count > CallLogCapacity)
            {
                _callLog.RemoveLast();
            }
        }

        private static string Preview(string json)
        {
            if (string.IsNullOrEmpty(json) || json.Length <= PreviewLength)
            {
                return json ?? string.Empty;
            }

            return json.Substring(0, PreviewLength) + "…";
        }

        private void RebuildOrderIfDirty()
        {
            if (!_orderedDirty)
            {
                return;
            }

            _ordered.Clear();
            _ordered.AddRange(_commands.Values);
            _ordered.Sort((left, right) => string.CompareOrdinal(left.Name, right.Name));
            _orderedDirty = false;
        }

        private static void Warn(string message)
        {
            Debug.LogWarning($"{LogTag} {message}");
        }
    }
}
#endif
