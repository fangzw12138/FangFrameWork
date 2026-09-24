using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Fang.Framework.CommandConsole.Editor.Tests
{
    /// <summary>线协议：请求解析与响应格式化（纯逻辑，不碰网络）。</summary>
    public class DebugCommandProtocolTests
    {
        [Test]
        public void Parse_ValidRequest_WithArgs()
        {
            var request = DebugCommandRequest.Parse("{\"id\":1,\"cmd\":\"probe_ok\",\"args\":[\"a\",\"b\"]}");

            Assert.IsTrue(request.Ok, request.Error);
            Assert.AreEqual("probe_ok", request.Command);
            CollectionAssert.AreEqual(new[] { "a", "b" }, request.Args);
            Assert.AreEqual(1, request.Id.Value<int>());
        }

        [Test]
        public void Parse_MissingArgs_YieldsEmptyArray()
        {
            var request = DebugCommandRequest.Parse("{\"id\":1,\"cmd\":\"ping\"}");

            Assert.IsTrue(request.Ok, request.Error);
            Assert.AreEqual(0, request.Args.Length);
        }

        [Test]
        public void Parse_NullArgs_YieldsEmptyArray()
        {
            var request = DebugCommandRequest.Parse("{\"cmd\":\"ping\",\"args\":null}");

            Assert.IsTrue(request.Ok, request.Error);
            Assert.AreEqual(0, request.Args.Length);
        }

        [Test]
        public void Parse_NonStringArgs_AreConvertedToStrings()
        {
            var request = DebugCommandRequest.Parse("{\"cmd\":\"c\",\"args\":[1,true,2.5]}");

            Assert.IsTrue(request.Ok, request.Error);
            // 取 JSON 字面量（小写 true），这样 GetBool 的 bool.TryParse 也能吃。
            CollectionAssert.AreEqual(new[] { "1", "true", "2.5" }, request.Args);
        }

        [Test]
        public void Parse_ConvertedBoolArg_IsAcceptedByGetBool()
        {
            var request = DebugCommandRequest.Parse("{\"cmd\":\"c\",\"args\":[true]}");
            var args = new DebugCommandArgs(request.Args);

            Assert.IsTrue(args.GetBool(0, "flag"));
        }

        [Test]
        public void Parse_StringId_IsCarried()
        {
            var request = DebugCommandRequest.Parse("{\"id\":\"abc\",\"cmd\":\"ping\"}");

            Assert.IsTrue(request.Ok, request.Error);
            Assert.AreEqual("abc", request.Id.Value<string>());
        }

        [Test]
        public void Parse_InvalidJson_ReportsErrorAndMentionsFormat()
        {
            var request = DebugCommandRequest.Parse("not json at all");

            Assert.IsFalse(request.Ok);
            StringAssert.Contains("不是合法 JSON", request.Error);
            StringAssert.Contains("cmd", request.Error);
        }

        [Test]
        public void Parse_EmptyLine_ReportsError()
        {
            Assert.IsFalse(DebugCommandRequest.Parse("   ").Ok);
            Assert.IsFalse(DebugCommandRequest.Parse(null).Ok);
        }

        [Test]
        public void Parse_MissingCmd_ReportsError_AndKeepsIdForEcho()
        {
            var request = DebugCommandRequest.Parse("{\"id\":9,\"args\":[]}");

            Assert.IsFalse(request.Ok);
            StringAssert.Contains("缺少字段 cmd", request.Error);
            Assert.AreEqual(9, request.Id.Value<int>(), "错误响应也要能带回 id");
        }

        [Test]
        public void Parse_ArgsNotArray_ReportsErrorInsteadOfSilentlyIgnoring()
        {
            var request = DebugCommandRequest.Parse("{\"id\":1,\"cmd\":\"ping\",\"args\":\"ping\"}");

            Assert.IsFalse(request.Ok);
            StringAssert.Contains("args 必须是数组", request.Error);
            Assert.AreEqual(1, request.Id.Value<int>());
        }

        [Test]
        public void FormatResponse_EchoesId()
        {
            var response = DebugCommandProtocol.FormatResponse(
                new JObject { ["ok"] = true, ["data"] = new JObject { ["pong"] = true } },
                JValue.CreateString("req-1"));

            StringAssert.Contains("\"ok\":true", response);
            StringAssert.Contains("\"id\":\"req-1\"", response);
            StringAssert.Contains("pong", response);
        }

        [Test]
        public void FormatResponse_OmitsId_WhenRequestHadNone()
        {
            var response = DebugCommandProtocol.FormatResponse(new JObject { ["ok"] = true }, null);

            Assert.IsFalse(response.Contains("\"id\""), response);
        }

        [Test]
        public void FormatResponse_OmitsId_WhenIdIsJsonNull()
        {
            var response = DebugCommandProtocol.FormatResponse(new JObject { ["ok"] = true }, JValue.CreateNull());

            Assert.IsFalse(response.Contains("\"id\""), response);
        }

        [Test]
        public void FormatResponse_NullResponse_BecomesError()
        {
            var response = DebugCommandProtocol.FormatResponse(null, null);

            StringAssert.Contains("\"ok\":false", response);
            StringAssert.Contains("没有返回响应", response);
        }

        [Test]
        public void FormatError_ProducesErrorShapeWithId()
        {
            var response = DebugCommandProtocol.FormatError("炸了", new JValue(3));

            StringAssert.Contains("\"ok\":false", response);
            StringAssert.Contains("炸了", response);
            StringAssert.Contains("\"id\":3", response);
        }

        [Test]
        public void WireEncoding_HasNoBom()
        {
            var preamble = DebugCommandProtocol.WireEncoding.GetPreamble();

            Assert.AreEqual(0, preamble.Length, "响应不应带 UTF-8 BOM");
        }

        [Test]
        public void RequestExample_IsParsable()
        {
            var request = DebugCommandRequest.Parse(DebugCommandProtocol.RequestExample);

            Assert.IsTrue(request.Ok, request.Error);
            Assert.AreEqual("ping", request.Command);
        }

        [Test]
        public void Parse_Error_RegexIsStableForClients()
        {
            // 文案是给 AI / 测试脚本看的契约的一部分：确认关键短语没被顺手改掉。
            var invalidJson = DebugCommandRequest.Parse("{oops");
            var missingCmd = DebugCommandRequest.Parse("{\"id\":1}");

            Assert.IsTrue(Regex.IsMatch(invalidJson.Error, "不是合法 JSON"));
            Assert.IsTrue(Regex.IsMatch(missingCmd.Error, "缺少字段 cmd"));
        }
    }
}
