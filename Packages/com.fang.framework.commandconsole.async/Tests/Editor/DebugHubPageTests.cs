#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Linq;
using Fang.Framework.Editor.Hub;
using NUnit.Framework;

namespace Fang.Framework.CommandConsole.Async.Tests
{
    public class DebugHubPageTests
    {
        [Test]
        public void Hub_page_is_discovered_with_expected_metadata()
        {
            var page = FangHubPageRegistry.Discover()
                .FirstOrDefault(candidate => candidate.Id == "framework-command-console-async");

            Assert.IsNotNull(page, "FangHub 没有发现「指令控制台（异步）」页。");
            Assert.AreEqual("指令控制台（异步）", page.Title);
            Assert.AreEqual("Framework", page.Category);
            Assert.AreEqual("异步指令层：指令清单、来源与调用方式。", page.Description);
            Assert.AreEqual(3, page.Order);
            Assert.AreEqual("Fang.Framework.CommandConsole.Async.Editor", page.PageType.Assembly.GetName().Name);
        }

        [Test]
        public void Built_in_commands_use_the_built_in_category()
        {
            var registry = new DebugCommandRegistry();
            registry.ScanLoadedAssemblies();

            Assert.AreEqual("builtin", DebugCommandDefaults.BuiltInCategory);
            Assert.AreEqual("builtin", registry.Find("ping")?.Category);
            Assert.AreEqual("builtin", registry.Find("list_commands")?.Category);
            Assert.AreEqual("builtin", registry.Find("command_log")?.Category);
        }
    }
}
#endif
