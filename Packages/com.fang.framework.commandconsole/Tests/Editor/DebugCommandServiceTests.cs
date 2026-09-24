using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Fang.Framework.CommandConsole.Editor.Tests
{
    /// <summary>
    /// <see cref="DebugCommandService"/>：默认启动、Configure 幂等与生效、tick 驱动、内置指令、控制台。
    /// </summary>
    public class DebugCommandServiceTests
    {
        private const int TestPort = 47811;
        private const int OtherPort = 47812;

        private ProbeScope _scope;
        private DebugCommandService _service;
        private DebugProjectSo _project;

        [TearDown]
        public void TearDown()
        {
            if (_scope != null)
            {
                // 框架不销毁 GameObject，也不写 Unity 消息方法 —— 释放必须显式调。
                _scope.OnDispose();
                Object.DestroyImmediate(_scope.gameObject);
            }

            _scope = null;
            _service = null;

            DebugProjectProbe.Destroy(_project);
            _project = null;
        }

        private DebugCommandService CreateService()
        {
            _scope = ProbeScope.Create("DebugServiceHost");
            _service = _scope.AddProbeService<DebugCommandService>();
            return _service;
        }

        private DebugProjectSo NewProject(int port, bool autoStart = true, bool consoleEnabled = true)
        {
            _project = DebugProjectProbe.New();
            DebugProjectProbe.SetInt(_project, "_port", port);
            DebugProjectProbe.SetBool(_project, "_autoStart", autoStart);
            DebugProjectProbe.SetBool(_project, "_consoleEnabled", consoleEnabled);
            return _project;
        }

        // ---------- 默认启动 ----------

        [Test]
        public void OnInit_UsesBuiltInDefaultsAndStartsListening()
        {
            var service = CreateService();

            Assert.AreEqual(DebugCommandDefaults.Port, service.Settings.Port);
            Assert.AreEqual(DebugCommandDefaults.ListenAddress, service.Settings.ListenAddress);
            Assert.IsTrue(service.Settings.AutoStart);
            Assert.IsTrue(service.IsListening, "默认 AutoStart=true，OnInit 就该在监听");
            Assert.AreEqual(DebugCommandDefaults.Port, service.Port);
            Assert.IsNotNull(service.Console, "默认 ConsoleEnabled=true，就该建出控制台");
            Assert.IsNull(service.Project, "还没 Configure 过");
        }

        [Test]
        public void OnInit_RegistersBuiltInCommands()
        {
            var service = CreateService();

            Assert.IsNotNull(service.Registry.Find("ping"));
            Assert.IsNotNull(service.Registry.Find("list_commands"));
            Assert.IsNotNull(service.Registry.Find("command_log"));
            Assert.AreEqual("builtin", service.Registry.Find("ping").Category);
        }

        [Test]
        public void OnInit_AddsItselfToActiveServices()
        {
            var service = CreateService();

            CollectionAssert.Contains(DebugCommandService.ActiveServices, service);
        }

        [Test]
        public void OnDispose_StopsListeningAndLeavesActiveServices()
        {
            var service = CreateService();

            service.OnDispose();

            Assert.IsFalse(service.IsListening);
            CollectionAssert.DoesNotContain(DebugCommandService.ActiveServices, service);
        }

        // ---------- Configure ----------

        [Test]
        public void Configure_AppliesPortAndKeepsListening()
        {
            var service = CreateService();

            service.Configure(NewProject(TestPort));

            Assert.IsTrue(service.IsListening);
            Assert.AreEqual(TestPort, service.Port);
            Assert.AreSame(_project, service.Project);
        }

        [Test]
        public void Configure_IsIdempotent()
        {
            var service = CreateService();
            var project = NewProject(TestPort);

            service.Configure(project);
            var firstPort = service.Port;

            // 同样的配置再套一次：不该重开端口（否则日志里会多一条「已启动」）。
            service.Configure(project);

            Assert.IsTrue(service.IsListening);
            Assert.AreEqual(firstPort, service.Port);
        }

        [Test]
        public void Configure_ChangingPort_MovesTheListener()
        {
            var service = CreateService();

            service.Configure(NewProject(TestPort));
            Assert.AreEqual(TestPort, service.Port);

            service.Configure(NewProject(OtherPort));
            Assert.AreEqual(OtherPort, service.Port);
            Assert.IsTrue(service.IsListening);
        }

        [Test]
        public void Configure_AutoStartFalse_StopsListening()
        {
            var service = CreateService();
            Assert.IsTrue(service.IsListening);

            service.Configure(NewProject(TestPort, autoStart: false));

            Assert.IsFalse(service.IsListening);
        }

        [Test]
        public void Configure_ConsoleDisabled_TurnsConsoleOff()
        {
            var service = CreateService();
            var console = service.Console;
            Assert.IsNotNull(console);

            service.Configure(NewProject(TestPort, consoleEnabled: false));

            Assert.IsFalse(console.enabled, "关掉控制台后 OnGUI 不该再被调用");
        }

        [Test]
        public void Configure_AppliesToggleKeyAndQuickButtons()
        {
            var service = CreateService();
            var project = NewProject(TestPort);
            DebugProjectProbe.SetQuickButtons(project, new[] { new DebugQuickButton("战斗", "probe_ok 1") });

            service.Configure(project);

            // 内置三个在前，配置的在后面。
            Assert.AreEqual(4, service.Console.QuickButtons.Count);
            Assert.AreEqual("ping", service.Console.QuickButtons[0].CommandLine);
            Assert.AreEqual("probe_ok 1", service.Console.QuickButtons[3].CommandLine);
        }

        [Test]
        public void Configure_NarrowerScanPrefix_RescansAndDropsBuiltIns()
        {
            var service = CreateService();
            Assert.IsNotNull(service.Registry.Find("ping"));

            var project = NewProject(TestPort);
            DebugProjectProbe.SetStringList(
                project,
                "_commandAssemblyNamePrefixes",
                new[] { "Fang.Framework.CommandConsole.Editor.Tests" });

            service.Configure(project);

            // 白名单只留测试程序集 → 探针在、内置指令（在 Runtime 程序集里）不在。
            Assert.IsNotNull(service.Registry.Find("probe_ok"));
            Assert.IsNull(service.Registry.Find("ping"));
        }

        [Test]
        public void Rescan_ReRegistersAfterClear()
        {
            var service = CreateService();

            service.Registry.Clear();
            Assert.IsNull(service.Registry.Find("ping"));

            service.Rescan();

            Assert.IsNotNull(service.Registry.Find("ping"));
        }

        // ---------- tick ----------

        [Test]
        public void OnTick_CountsAndMarksTicking()
        {
            var service = CreateService();

            Assert.AreEqual(0, service.TickCount);
            Assert.IsFalse(service.IsTicking, "没 tick 过就不算在 tick");

            service.OnTick(0.016f);
            service.OnTick(0.016f);

            Assert.AreEqual(2, service.TickCount);
            Assert.IsTrue(service.IsTicking);
        }

        [Test]
        public void OnTick_WithoutTransport_DoesNotThrow()
        {
            var service = CreateService();
            service.StopTransport();

            Assert.DoesNotThrow(() => service.OnTick(0.016f));
        }

        [Test]
        public void StartTransport_AfterStop_Resumes()
        {
            var service = CreateService();

            service.StopTransport();
            Assert.IsFalse(service.IsListening);

            Assert.IsTrue(service.StartTransport());
            Assert.IsTrue(service.IsListening);
        }

        // ---------- 内置指令 ----------

        [Test]
        public void Ping_ReportsListeningAndTicking()
        {
            var service = CreateService();
            service.OnTick(0.016f);

            var json = service.ExecuteText("ping");

            StringAssert.Contains("\"pong\":true", json);
            StringAssert.Contains("\"listening\":true", json);
            StringAssert.Contains("\"ticking\":true", json);
            StringAssert.Contains("\"commands\":", json);
        }

        [Test]
        public void Ping_WhenNotTicked_ReportsNotTicking()
        {
            var service = CreateService();

            var json = service.ExecuteText("ping");

            StringAssert.Contains("\"ticking\":false", json);
        }

        [Test]
        public void ListCommands_ContainsBuiltIns()
        {
            var service = CreateService();

            var json = service.ExecuteText("list_commands");

            StringAssert.Contains("ping", json);
            StringAssert.Contains("list_commands", json);
            StringAssert.Contains("command_log", json);
        }

        [Test]
        public void ListCommands_FilterNarrowsResult()
        {
            var service = CreateService();

            var json = service.ExecuteText("list_commands probe_ok");

            StringAssert.Contains("probe_ok", json);
            StringAssert.Contains("\"shown\":1", json);
        }

        [Test]
        public void CommandLog_InvalidCount_ReturnsBusinessError()
        {
            var service = CreateService();

            var json = service.ExecuteText("command_log 999");

            StringAssert.Contains("\"ok\":false", json);
            StringAssert.Contains("1~200", json);
        }

        [Test]
        public void ExecuteText_SplitsArguments()
        {
            var service = CreateService();

            var json = service.ExecuteText("probe_ok 5");

            StringAssert.Contains("\"ok\":true", json);
            StringAssert.Contains("\"value\":5", json);
        }

        [Test]
        public void ExecuteText_EmptyLine_ReturnsError()
        {
            var service = CreateService();

            StringAssert.Contains("\"ok\":false", service.ExecuteText("   "));
        }

        [Test]
        public void UnknownCommand_ReturnsError()
        {
            var service = CreateService();

            StringAssert.Contains("未知指令", service.ExecuteText("no_such_cmd"));
        }
    }
}
