using NUnit.Framework;
using UnityEngine;

namespace Fang.Framework.CommandConsole.Editor.Tests
{
    /// <summary>
    /// <see cref="DebugProjectSo"/>：新建资产的字段默认值，以及「留空回落」规则（回落逻辑在
    /// <see cref="DebugSettings"/>，这里测资产侧读出来的原始值）。
    /// </summary>
    public class DebugProjectSoTests
    {
        private DebugProjectSo _project;

        [TearDown]
        public void TearDown()
        {
            DebugProjectProbe.Destroy(_project);
            _project = null;
        }

        [Test]
        public void NewAsset_CarriesBuiltInDefaults()
        {
            _project = DebugProjectProbe.New();

            Assert.IsTrue(_project.AutoStart);
            Assert.AreEqual(DebugCommandDefaults.ListenAddress, _project.ListenAddress);
            Assert.AreEqual(DebugCommandDefaults.Port, _project.Port);
            Assert.IsTrue(_project.ConsoleEnabled);
            Assert.AreEqual(DebugCommandDefaults.ConsoleToggleKey, _project.ConsoleToggleKey);
            Assert.AreEqual(0, _project.QuickButtons.Count);
            Assert.AreEqual(0, _project.CommandAssemblyNamePrefixes.Count);
            Assert.AreEqual(0, _project.CategoryOrder.Count);
            Assert.AreEqual(DebugCommandDefaults.DocFolder, _project.DocFolder);
            Assert.AreEqual(DebugCommandDefaults.DocFileName, _project.DocFileName);
            Assert.IsTrue(_project.DocSplitByCategory);
        }

        [Test]
        public void BlankStrings_FallBackToDefaults()
        {
            _project = DebugProjectProbe.New();
            DebugProjectProbe.SetString(_project, "_listenAddress", "   ");
            DebugProjectProbe.SetString(_project, "_docFolder", "");
            DebugProjectProbe.SetString(_project, "_docFileName", "  ");
            DebugProjectProbe.SetInt(_project, "_port", 0);

            var settings = DebugSettings.From(_project);

            Assert.AreEqual(DebugCommandDefaults.ListenAddress, settings.ListenAddress);
            Assert.AreEqual(DebugCommandDefaults.DocFolder, settings.DocFolder);
            Assert.AreEqual(DebugCommandDefaults.DocFileName, settings.DocFileName);
            Assert.AreEqual(DebugCommandDefaults.Port, settings.Port);
        }

        [Test]
        public void OutOfRangePort_FallsBackToDefault()
        {
            _project = DebugProjectProbe.New();
            DebugProjectProbe.SetInt(_project, "_port", 70000);

            Assert.AreEqual(DebugCommandDefaults.Port, DebugSettings.From(_project).Port);
        }

        [Test]
        public void EmptyLists_FallBackToDefaults()
        {
            _project = DebugProjectProbe.New();
            DebugProjectProbe.SetStringList(_project, "_categoryOrder", new[] { "  ", "" });
            DebugProjectProbe.SetStringList(_project, "_commandAssemblyNamePrefixes", new string[0]);

            var settings = DebugSettings.From(_project);

            CollectionAssert.AreEqual(
                DebugCommandDefaults.CategoryOrder,
                settings.CategoryOrder);
            Assert.AreEqual(0, settings.CommandAssemblyNamePrefixes.Count);
        }

        [Test]
        public void CategoryOrder_IsDeduplicatedAndTrimmed_PreservingOrder()
        {
            _project = DebugProjectProbe.New();
            DebugProjectProbe.SetStringList(_project, "_categoryOrder", new[] { "hero", " world ", "hero", "system" });

            var settings = DebugSettings.From(_project);

            CollectionAssert.AreEqual(new[] { "hero", "world", "system" }, settings.CategoryOrder);
        }

        [Test]
        public void Settings_Property_MatchesFromFactory()
        {
            _project = DebugProjectProbe.New();
            DebugProjectProbe.SetInt(_project, "_port", 47811);

            Assert.AreEqual(47811, _project.Settings.Port);
            Assert.AreEqual(47811, DebugSettings.From(_project).Port);
        }

        [Test]
        public void QuickButtons_WithEmptyCommandLine_AreDropped()
        {
            _project = DebugProjectProbe.New();
            DebugProjectProbe.SetQuickButtons(_project, new[]
            {
                new DebugQuickButton("有效", "ping"),
                new DebugQuickButton("无效", "   "),
            });

            var settings = DebugSettings.From(_project);

            Assert.AreEqual(1, settings.QuickButtons.Count);
            Assert.AreEqual("ping", settings.QuickButtons[0].CommandLine);
        }

        [Test]
        public void NullProject_YieldsUsableDefaults()
        {
            var settings = DebugSettings.From(null);

            Assert.AreEqual(DebugCommandDefaults.Port, settings.Port);
            Assert.AreEqual(DebugCommandDefaults.ListenAddress, settings.ListenAddress);
            Assert.IsTrue(settings.AutoStart);
            Assert.IsNotNull(settings.CategoryOrder);
            Assert.IsNotNull(settings.QuickButtons);
            Assert.IsNotNull(settings.CommandAssemblyNamePrefixes);
        }

        [Test]
        public void QuickButton_LabelFallsBackToCommandLine()
        {
            var button = new DebugQuickButton("  ", "command_log 5");

            Assert.AreEqual("command_log 5", button.Label);
        }
    }
}
