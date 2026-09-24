#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fang.Framework.CommandConsole
{
    /// <summary>
    /// 一行请求的解析结果。解析失败时 <see cref="Ok"/> 为 false、<see cref="Error"/> 说明原因，
    /// 而 <see cref="Id"/> 仍会「尽力」从原文里取出来 —— 这样错误响应也能带上请求方的 id，
    /// 调用方（AI / 测试脚本）不会因为 id 丢了而对不上号。
    /// </summary>
    public sealed class DebugCommandRequest
    {
        private DebugCommandRequest(bool ok, string error, JToken id, string command, string[] args)
        {
            Ok = ok;
            Error = error;
            Id = id;
            Command = command;
            Args = args;
        }

        public bool Ok { get; }

        public string Error { get; }

        /// <summary>原样回填的 id（可能是数字、字符串或 null）。</summary>
        public JToken Id { get; }

        public string Command { get; }

        public string[] Args { get; }

        public static DebugCommandRequest Parse(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return Failure(null, "请求为空");
            }

            JObject payload;
            JToken id = null;

            try
            {
                payload = JObject.Parse(line);
                id = payload["id"];
            }
            catch (Exception)
            {
                return Failure(null, "请求不是合法 JSON (格式: " + DebugCommandProtocol.RequestExample + ")");
            }

            var command = payload["cmd"]?.Value<string>();
            if (string.IsNullOrEmpty(command))
            {
                return Failure(id, "缺少字段 cmd");
            }

            var argsToken = payload["args"];
            var args = Array.Empty<string>();

            if (argsToken != null && argsToken.Type != JTokenType.Null)
            {
                if (argsToken.Type != JTokenType.Array)
                {
                    return Failure(id, "args 必须是数组 (示例: \"args\":[\"a\",\"b\"])");
                }

                var array = (JArray)argsToken;
                args = new string[array.Count];
                for (var i = 0; i < array.Count; i++)
                {
                    args[i] = DebugCommandProtocol.ToArgString(array[i]);
                }
            }

            return new DebugCommandRequest(true, null, id, command, args);
        }

        private static DebugCommandRequest Failure(JToken id, string error)
        {
            return new DebugCommandRequest(false, error, id, null, Array.Empty<string>());
        }
    }

    /// <summary>
    /// 线协议（纯逻辑，唯一解析/格式化 JSON 请求的地方）。
    /// 请求一行：<c>{"id":1,"cmd":"ping","args":["a"]}</c>
    /// 响应一行：<c>{"ok":true,"data":{...},"id":1}</c> 或 <c>{"ok":false,"error":"...","id":1}</c>
    /// id 由请求方自定义、原样回填（数字 / 字符串都行）。
    /// </summary>
    public static class DebugCommandProtocol
    {
        public const string RequestExample = "{\"id\":1,\"cmd\":\"ping\",\"args\":[]}";

        /// <summary>把注册表的响应序列化成一行，并回填请求里的 id。</summary>
        public static string FormatResponse(JObject response, JToken id)
        {
            var payload = response ?? DebugCommandRegistry.Error("指令没有返回响应");

            if (id != null && id.Type != JTokenType.Null)
            {
                payload["id"] = id;
            }

            return payload.ToString(Formatting.None);
        }

        /// <summary>只要错误、不要注册表时用这个（传输层自己发现的错误，例如 JSON 不合法）。</summary>
        public static string FormatError(string message, JToken id)
        {
            return FormatResponse(DebugCommandRegistry.Error(message), id);
        }

        /// <summary>数组元素 → 字符串参数：字符串原样，数字 / 布尔取字面值，其余取紧凑 JSON。</summary>
        internal static string ToArgString(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
            {
                return string.Empty;
            }

            if (token.Type == JTokenType.String)
            {
                return token.Value<string>();
            }

            return token.ToString(Formatting.None);
        }

        /// <summary>UTF-8 无 BOM 编码（写回响应时用，避免客户端读到 BOM）。</summary>
        internal static readonly Encoding WireEncoding = new UTF8Encoding(false);
    }
}
#endif
