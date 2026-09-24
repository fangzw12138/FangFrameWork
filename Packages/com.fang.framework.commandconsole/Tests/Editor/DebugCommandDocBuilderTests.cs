using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Fang.Framework.CommandConsole.Editor.Tests
{
    /// <summary>文档生成的纯逻辑：分类顺序、空域跳过、拆文件、转义、路径规范化。</summary>
    public class DebugCommandDocBuilderTests
    {
        private static MethodInfo Probe(string methodName)
        {
            return typeof(DebugCommandProbes).GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
        }

        private static DebugCommandEntry Entry(string name, string description, string category)
        {
            return new DebugCommandEntry(name, description, category, Probe(nameof(DebugCommandProbes.Ok)));
        }

        private static DebugSettings Settings(
            string folder = "Docs",
            string fileName = "DebugCommands.md",
            bool split = true,
            params string[] order)
        {
            return new DebugSettings
            {
                AutoStart = true,
                ListenAddress = "127.0.0.1",
                Port = 7777,
                ConsoleEnabled = true,
                ConsoleToggleKey = KeyCode.BackQuote,
                QuickButtons = Array.Empty<DebugQuickButton>(),
                CommandAssemblyNamePrefixes = Array.Empty<string>(),
                CategoryOrder = order == null || order.Length == 0
                    ? DebugCommandDefaults.CategoryOrder
                    : order,
                DocFolder = folder,
                DocFileName = fileName,
                DocSplitByCategory = split,
            };
        }

        private static readonly DebugCommandEntry[] Mixed =
        {
            Entry("world_teleport", "传送 <x> <y> <z>", "world"),
            Entry("ping", "探活", "system"),
            Entry("hero_heal", "回血 <value>", "hero"),
        };

        [Test]
        public void Build_EmptyEntries_ProducesNothing()
        {
            var documents = DebugCommandDocBuilder.Build(Array.Empty<DebugCommandEntry>(), Settings());

            Assert.AreEqual(0, documents.Count);
        }

        [Test]
        public void Build_IndexPath_FollowsConfiguredFolderAndName()
        {
            var documents = DebugCommandDocBuilder.Build(
                Mixed,
                Settings(folder: "Docs/Debug", fileName: "Commands.md", split: false));

            CollectionAssert.Contains(documents.Keys.ToList(), "Docs/Debug/Commands.md");
        }

        [Test]
        public void Build_SplitDisabled_WritesOnlyIndex()
        {
            var documents = DebugCommandDocBuilder.Build(Mixed, Settings(split: false));

            Assert.AreEqual(1, documents.Count);
            CollectionAssert.Contains(documents.Keys.ToList(), "Docs/DebugCommands.md");
        }

        [Test]
        public void Build_SplitEnabled_WritesIndexPlusOneFilePerCategory()
        {
            var documents = DebugCommandDocBuilder.Build(Mixed, Settings());

            Assert.AreEqual(4, documents.Count);
            CollectionAssert.Contains(documents.Keys.ToList(), "Docs/DebugCommands.md");
            CollectionAssert.Contains(documents.Keys.ToList(), "Docs/DebugCommands/system.md");
            CollectionAssert.Contains(documents.Keys.ToList(), "Docs/DebugCommands/hero.md");
            CollectionAssert.Contains(documents.Keys.ToList(), "Docs/DebugCommands/world.md");
        }

        [Test]
        public void Build_OrdersCategories_ConfiguredFirst_ThenAlphabetical()
        {
            var documents = DebugCommandDocBuilder.Build(Mixed, Settings(order: new[] { "hero", "system" }));
            var index = documents["Docs/DebugCommands.md"];

            var heroAt = index.IndexOf("## hero", StringComparison.Ordinal);
            var systemAt = index.IndexOf("## system", StringComparison.Ordinal);
            var worldAt = index.IndexOf("## world", StringComparison.Ordinal);

            Assert.Greater(heroAt, -1);
            Assert.Greater(systemAt, heroAt, "配置顺序里 system 在 hero 之后");
            Assert.Greater(worldAt, systemAt, "没列出的分类排在已列出的后面");
        }

        [Test]
        public void Build_DefaultOrder_PutsBuiltInFirst()
        {
            var entries = new[]
            {
                Entry("world_teleport", "传送 <x> <y> <z>", "world"),
                Entry("ping", "探活", DebugCommandDefaults.BuiltInCategory),
                Entry("hero_heal", "回血 <value>", "hero"),
            };

            var documents = DebugCommandDocBuilder.Build(entries, Settings());
            var index = documents["Docs/DebugCommands.md"];

            var builtInAt = index.IndexOf("## " + DebugCommandDefaults.BuiltInCategory, StringComparison.Ordinal);
            var heroAt = index.IndexOf("## hero", StringComparison.Ordinal);

            Assert.Greater(builtInAt, -1);
            Assert.Greater(heroAt, builtInAt, "默认顺序里内置指令分类在最前，其余按名称排序");
        }

        [Test]
        public void Build_ConfiguredCategoryWithoutCommands_GetsNoSectionAndNoFile()
        {
            var documents = DebugCommandDocBuilder.Build(Mixed, Settings(order: new[] { "hero", "empty", "system" }));

            CollectionAssert.DoesNotContain(documents.Keys.ToList(), "Docs/DebugCommands/empty.md");
            Assert.IsFalse(
                documents["Docs/DebugCommands.md"].Contains("## empty", StringComparison.Ordinal),
                "空域不该生成小节");
        }

        [Test]
        public void Build_EscapesPipesAndNewlinesInDescription()
        {
            var entries = new[] { Entry("weird", "第一行\n第二行 | 带竖线", "system") };

            var documents = DebugCommandDocBuilder.Build(entries, Settings(split: false));
            var index = documents["Docs/DebugCommands.md"];

            StringAssert.Contains("第一行 第二行 \\| 带竖线", index);
            Assert.IsFalse(index.Contains("第一行\n第二行", StringComparison.Ordinal), "换行必须被压平");
        }

        [Test]
        public void Build_IndexLinksToDomainFile()
        {
            var documents = DebugCommandDocBuilder.Build(Mixed, Settings());

            StringAssert.Contains("DebugCommands/hero.md", documents["Docs/DebugCommands.md"]);
            StringAssert.Contains("../DebugCommands.md", documents["Docs/DebugCommands/hero.md"]);
        }

        [Test]
        public void Build_IndexMentionsConnectionAndMenu()
        {
            var documents = DebugCommandDocBuilder.Build(Mixed, Settings());
            var index = documents["Docs/DebugCommands.md"];

            StringAssert.Contains("127.0.0.1:7777", index);
            StringAssert.Contains(DebugCommandDocBuilder.ExportMenuPath, index);
            StringAssert.Contains("list_commands", index);
        }

        [Test]
        public void Build_CategoryFileName_IsSanitized()
        {
            var entries = new[] { Entry("odd", "奇怪的分类", "a/b:c") };

            var documents = DebugCommandDocBuilder.Build(entries, Settings());

            CollectionAssert.Contains(documents.Keys.ToList(), "Docs/DebugCommands/a_b_c.md");
        }

        [Test]
        public void OrderCategories_KeepsUnlistedCategoriesAtEnd_Alphabetically()
        {
            var entries = new[]
            {
                Entry("a", "", "zeta"),
                Entry("b", "", "alpha"),
                Entry("c", "", "system"),
                Entry("d", "", "hero"),
            };

            var order = DebugCommandDocBuilder.OrderCategories(entries, new[] { "hero", "system" });

            CollectionAssert.AreEqual(new[] { "hero", "system", "alpha", "zeta" }, order);
        }

        [Test]
        public void OrderCategories_IgnoresConfiguredNamesWithNoCommands()
        {
            var entries = new[] { Entry("a", "", "system") };

            var order = DebugCommandDocBuilder.OrderCategories(entries, new[] { "ghost", "system" });

            CollectionAssert.AreEqual(new[] { "system" }, order);
        }

        [Test]
        public void NormalizeFolder_StripsSlashesAndFallsBack()
        {
            Assert.AreEqual("Docs/Debug", DebugCommandDocBuilder.NormalizeFolder("  Docs\\Debug/  "));
            Assert.AreEqual(DebugCommandDefaults.DocFolder, DebugCommandDocBuilder.NormalizeFolder("   "));
            Assert.AreEqual(DebugCommandDefaults.DocFolder, DebugCommandDocBuilder.NormalizeFolder(null));
        }

        [Test]
        public void NormalizeFileName_KeepsOnlyFileNameAndFallsBack()
        {
            Assert.AreEqual("Commands.md", DebugCommandDocBuilder.NormalizeFileName("Docs/Sub/Commands.md"));
            Assert.AreEqual(DebugCommandDefaults.DocFileName, DebugCommandDocBuilder.NormalizeFileName("  "));
        }

        [Test]
        public void IndexPath_MatchesTheIndexDocumentKey()
        {
            var settings = Settings(folder: "Docs/Out", fileName: "Cmd.md", split: true);

            var documents = DebugCommandDocBuilder.Build(Mixed, settings);

            Assert.AreEqual(DebugCommandDocBuilder.IndexPath(settings), "Docs/Out/Cmd.md");
            CollectionAssert.Contains(documents.Keys.ToList(), DebugCommandDocBuilder.IndexPath(settings));
        }

        [Test]
        public void Build_IsDeterministic_ForSameInput()
        {
            var first = DebugCommandDocBuilder.Build(Mixed, Settings());
            var second = DebugCommandDocBuilder.Build(Mixed, Settings());

            CollectionAssert.AreEqual(
                first.Keys.ToList(),
                second.Keys.ToList());

            foreach (var pair in first)
            {
                Assert.AreEqual(pair.Value, second[pair.Key]);
            }
        }
    }
}
