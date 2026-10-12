#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;

namespace Fang.Framework.CommandConsole.Async
{
    /// <summary>
    /// 一次指令调用的日志记录（环形缓冲的条目，只读）。
    /// </summary>
    public sealed class DebugCommandCallRecord
    {
        public DebugCommandCallRecord(
            int sequence,
            DateTime timestamp,
            string command,
            string argsText,
            bool succeeded,
            string error,
            double elapsedMs,
            string resultPreview)
        {
            Sequence = sequence;
            Timestamp = timestamp;
            Command = command;
            ArgsText = argsText ?? string.Empty;
            Succeeded = succeeded;
            Error = error;
            ElapsedMs = elapsedMs;
            ResultPreview = resultPreview ?? string.Empty;
        }

        /// <summary>自注册表创建以来的调用序号（从 1 开始递增）。</summary>
        public int Sequence { get; }

        /// <summary>调用时刻（本地时间）。</summary>
        public DateTime Timestamp { get; }

        /// <summary>指令名。</summary>
        public string Command { get; }

        /// <summary>参数原文（空格连接）。</summary>
        public string ArgsText { get; }

        /// <summary>是否成功（<c>ok == true</c>）。</summary>
        public bool Succeeded { get; }

        /// <summary>失败原因；成功时为 null。</summary>
        public string Error { get; }

        /// <summary>耗时（毫秒）。</summary>
        public double ElapsedMs { get; }

        /// <summary>响应 JSON 的截断预览。</summary>
        public string ResultPreview { get; }

        public override string ToString()
        {
            var status = Succeeded ? "OK " : "ERR";
            var text = $"#{Sequence} {Timestamp:HH:mm:ss.fff} {status} {Command} {ArgsText} ({ElapsedMs:0.###}ms)";

            return Succeeded ? text.TrimEnd() : $"{text} -> {Error}";
        }
    }
}
#endif
