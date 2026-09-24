using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Fang.Framework.CommandConsole.Editor.Tests
{
    /// <summary>游戏内控制台：输出记录、快捷按钮、开关状态（不渲染，只验证数据与状态）。</summary>
    public class DebugCommandConsoleTests
    {
        private ProbeScope _scope;
        private DebugCommandService _service;
        private GameObject _host;
        private DebugCommandConsole _console;

        [TearDown]
        public void TearDown()
        {
            if (_host != null)
            {
                Object.DestroyImmediate(_host);
            }

            _host = null;
            _console = null;

            if (_scope != null)
            {
                _scope.OnDispose();
                Object.DestroyImmediate(_scope.gameObject);
            }

            _scope = null;
            _service = null;
        }

        private DebugCommandConsole NewConsole(DebugCommandService service)
        {
            _host = new GameObject("ProbeConsole");
            _console = _host.AddComponent<DebugCommandConsole>();
            _console.Initialize(service);
            return _console;
        }

        private DebugCommandConsole NewConsoleWithService()
        {
            _scope = ProbeScope.Create("DebugConsoleHost");
            _service = _scope.AddProbeService<DebugCommandService>();
            return NewConsole(_service);
        }

        [Test]
        public void Apply_PutsBuiltInButtonsFirst()
        {
            var console = NewConsole(null);

            console.Apply(KeyCode.F1, new[] { new DebugQuickButton("战斗", "probe_ok 1") });

            Assert.AreEqual(4, console.QuickButtons.Count);
            Assert.AreEqual("ping", console.QuickButtons[0].CommandLine);
            Assert.AreEqual("list_commands", console.QuickButtons[1].CommandLine);
            Assert.AreEqual("command_log", console.QuickButtons[2].CommandLine);
            Assert.AreEqual("probe_ok 1", console.QuickButtons[3].CommandLine);
            Assert.AreEqual(KeyCode.F1, console.ToggleKey);
        }

        [Test]
        public void Apply_Twice_DoesNotDuplicateBuiltIns()
        {
            var console = NewConsole(null);

            console.Apply(KeyCode.BackQuote, null);
            console.Apply(KeyCode.BackQuote, null);

            Assert.AreEqual(3, console.QuickButtons.Count);
        }

        [Test]
        public void RegisterQuickButton_Appends()
        {
            var console = NewConsole(null);
            console.Apply(KeyCode.BackQuote, null);

            console.RegisterQuickButton("加血", "probe_ok 99");

            Assert.AreEqual(4, console.QuickButtons.Count);
            Assert.AreEqual("加血", console.QuickButtons[3].Label);
        }

        [Test]
        public void RegisterQuickButton_EmptyCommand_IsIgnored()
        {
            var console = NewConsole(null);
            console.Apply(KeyCode.BackQuote, null);

            console.RegisterQuickButton("空", "   ");

            Assert.AreEqual(3, console.QuickButtons.Count);
        }

        [Test]
        public void Execute_RecordsInputAndResult()
        {
            var console = NewConsoleWithService();

            console.Execute("ping");

            Assert.AreEqual(2, console.Output.Count);
            StringAssert.StartsWith("> ping", console.Output[0]);
            StringAssert.Contains("\"pong\":true", console.Output[1]);
        }

        [Test]
        public void Execute_EmptyLine_IsIgnored()
        {
            var console = NewConsoleWithService();

            console.Execute("   ");

            Assert.AreEqual(0, console.Output.Count);
        }

        [Test]
        public void Execute_WithoutService_ReportsUnavailable()
        {
            var console = NewConsole(null);

            console.Execute("ping");

            StringAssert.Contains("指令服务不可用", console.Output[1]);
        }

        [Test]
        public void Output_IsCappedAtMaxLines()
        {
            var console = NewConsoleWithService();

            for (var i = 0; i < DebugCommandDefaults.ConsoleMaxOutputLines; i++)
            {
                console.Execute("ping");
            }

            Assert.AreEqual(DebugCommandDefaults.ConsoleMaxOutputLines, console.Output.Count);
        }

        [Test]
        public void ClearOutput_Empties()
        {
            var console = NewConsoleWithService();
            console.Execute("ping");

            console.ClearOutput();

            Assert.AreEqual(0, console.Output.Count);
        }

        [Test]
        public void Toggle_FlipsVisibility()
        {
            var console = NewConsole(null);

            Assert.IsFalse(console.IsVisible);

            console.Toggle();
            Assert.IsTrue(console.IsVisible);

            console.Toggle();
            Assert.IsFalse(console.IsVisible);
        }

        [Test]
        public void SetEnabled_False_HidesConsoleAndDisablesComponent()
        {
            var console = NewConsole(null);
            console.Toggle();
            Assert.IsTrue(console.IsVisible);

            console.SetEnabled(false);

            Assert.IsFalse(console.IsVisible);
            Assert.IsFalse(console.enabled);
        }
    }
}
