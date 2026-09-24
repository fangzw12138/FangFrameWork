using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Fang.Framework.CommandConsole.Editor.Tests
{
    /// <summary>内置指令的直调路径：没有注册表 / 没有服务时的表现（服务路径在 ServiceTests 里测）。</summary>
    public class DebugBuiltInCommandsTests
    {
        private static string Json(object result)
        {
            return result == null ? "null" : JToken.FromObject(result).ToString(Formatting.None);
        }

        private static DebugCommandArgs NoArgs()
        {
            return new DebugCommandArgs(Array.Empty<string>());
        }

        [Test]
        public void Ping_WithoutService_ReportsNotListeningAndNotTicking()
        {
            var registry = new DebugCommandRegistry();
            var context = new DebugCommandContext(registry, () => null);

            var json = Json(DebugBuiltInCommands.Ping(context, NoArgs()));

            StringAssert.Contains("\"pong\":true", json);
            StringAssert.Contains("\"listening\":false", json);
            StringAssert.Contains("\"ticking\":false", json);
            StringAssert.Contains("\"commands\":0", json);
        }

        [Test]
        public void ListCommands_WithoutRegistry_ThrowsBusinessError()
        {
            var context = new DebugCommandContext(() => null);

            var ex = Assert.Throws<DebugCommandException>(
                () => DebugBuiltInCommands.ListCommands(context, NoArgs()));

            StringAssert.Contains("注册表不可用", ex.Message);
        }

        [Test]
        public void CommandLog_WithoutRegistry_ThrowsBusinessError()
        {
            var context = new DebugCommandContext(() => null);

            Assert.Throws<DebugCommandException>(() => DebugBuiltInCommands.CommandLog(context, NoArgs()));
        }

        [Test]
        public void CommandLog_CountOutOfRange_ThrowsBusinessError()
        {
            var registry = new DebugCommandRegistry();
            var context = new DebugCommandContext(registry, () => null);

            var tooBig = Assert.Throws<DebugCommandException>(
                () => DebugBuiltInCommands.CommandLog(context, new DebugCommandArgs(new[] { "999" })));
            StringAssert.Contains("1~200", tooBig.Message);

            var zero = Assert.Throws<DebugCommandException>(
                () => DebugBuiltInCommands.CommandLog(context, new DebugCommandArgs(new[] { "0" })));
            StringAssert.Contains("1~200", zero.Message);
        }

        [Test]
        public void CommandLog_DefaultCount_ReturnsEntriesNewestFirst()
        {
            var registry = new DebugCommandRegistry();
            registry.Register("noop", "什么都不做", "probe", typeof(DebugCommandProbes).GetMethod(nameof(DebugCommandProbes.Null)));

            registry.ExecuteJson("noop", Array.Empty<string>());
            registry.ExecuteJson("noop", Array.Empty<string>());

            var context = new DebugCommandContext(registry, () => null);
            var json = Json(DebugBuiltInCommands.CommandLog(context, NoArgs()));

            StringAssert.Contains("\"total\":2", json);
            StringAssert.Contains("\"shown\":2", json);
        }

        [Test]
        public void ListCommands_EmptyFilter_ReturnsEverything()
        {
            var registry = new DebugCommandRegistry();
            registry.Register("a_one", "第一个", "probe", typeof(DebugCommandProbes).GetMethod(nameof(DebugCommandProbes.Null)));
            registry.Register("b_two", "第二个", "probe", typeof(DebugCommandProbes).GetMethod(nameof(DebugCommandProbes.Null)));

            var context = new DebugCommandContext(registry, () => null);
            var json = Json(DebugBuiltInCommands.ListCommands(context, NoArgs()));

            StringAssert.Contains("\"total\":2", json);
            StringAssert.Contains("\"shown\":2", json);
            StringAssert.Contains("a_one", json);
            StringAssert.Contains("b_two", json);
        }

        [Test]
        public void ListCommands_FilterMatchesNameCategoryOrDescription()
        {
            var registry = new DebugCommandRegistry();
            registry.Register("a_one", "第一个", "hero", typeof(DebugCommandProbes).GetMethod(nameof(DebugCommandProbes.Null)));
            registry.Register("b_two", "第二个", "world", typeof(DebugCommandProbes).GetMethod(nameof(DebugCommandProbes.Null)));

            var context = new DebugCommandContext(registry, () => null);

            StringAssert.Contains("\"shown\":1", Json(DebugBuiltInCommands.ListCommands(
                context, new DebugCommandArgs(new[] { "hero" }))));
            StringAssert.Contains("\"shown\":1", Json(DebugBuiltInCommands.ListCommands(
                context, new DebugCommandArgs(new[] { "b_two" }))));
            StringAssert.Contains("\"shown\":2", Json(DebugBuiltInCommands.ListCommands(
                context, new DebugCommandArgs(new[] { "第" }))));
        }
    }
}
