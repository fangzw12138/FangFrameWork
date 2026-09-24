using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Fang.Framework.CommandConsole.Editor.Tests
{
    /// <summary>
    /// <see cref="DebugCommandRegistry"/>：登记、执行、调用日志。
    /// 大多数用例手工 <c>Register</c>（确定、不产生扫描警告），另有一条走真实 <c>Scan</c>。
    /// </summary>
    public class DebugCommandRegistryTests
    {
        private static MethodInfo Probe(string methodName)
        {
            return typeof(DebugCommandProbes).GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
        }

        private static DebugCommandRegistry CreateRegistry()
        {
            var registry = new DebugCommandRegistry();

            registry.Register("probe_ok", "正常指令 <value>", "probe", Probe(nameof(DebugCommandProbes.Ok)));
            registry.Register("probe_null", "返回 null", "probe", Probe(nameof(DebugCommandProbes.Null)));
            registry.Register("probe_business_error", "业务失败", "probe", Probe(nameof(DebugCommandProbes.BusinessError)));
            registry.Register("probe_runtime_error", "运行时异常", "probe", Probe(nameof(DebugCommandProbes.RuntimeError)));
            registry.Register("probe_context", "回显注册表", "probe", Probe(nameof(DebugCommandProbes.ContextProbe)));

            return registry;
        }

        [Test]
        public void Register_ThenFind_ReturnsEntry()
        {
            var registry = CreateRegistry();

            var entry = registry.Find("probe_ok");

            Assert.IsNotNull(entry);
            Assert.AreEqual("probe_ok", entry.Name);
            Assert.AreEqual("probe", entry.Category);
            StringAssert.Contains("DebugCommandProbes", entry.Source);
            Assert.IsNull(registry.Find("no_such_cmd"));
            Assert.AreEqual(5, registry.Count);
        }

        [Test]
        public void Register_DuplicateName_OverridesPrevious()
        {
            var registry = new DebugCommandRegistry();

            registry.Register("dup", "第一条", "probe", Probe(nameof(DebugCommandProbes.Ok)));
            registry.Register("dup", "第二条", "probe", Probe(nameof(DebugCommandProbes.Null)));

            Assert.AreEqual(1, registry.Count);
            Assert.AreEqual("第二条", registry.Find("dup").Description);
        }

        [Test]
        public void Register_EmptyCategory_FallsBackToSystem()
        {
            var registry = new DebugCommandRegistry();

            registry.Register("x", "d", "   ", Probe(nameof(DebugCommandProbes.Ok)));

            Assert.AreEqual(DebugCommandDefaults.DefaultCategory, registry.Find("x").Category);
        }

        [Test]
        public void Commands_AreSortedByName()
        {
            var registry = CreateRegistry();

            var names = registry.Commands.Select(c => c.Name).ToList();
            var sorted = names.OrderBy(n => n, StringComparer.Ordinal).ToList();

            CollectionAssert.AreEqual(sorted, names);
        }

        [Test]
        public void Scan_RegistersProbeCommandsFromAssembly()
        {
            var registry = new DebugCommandRegistry();

            var count = registry.Scan(new[] { typeof(DebugCommandRegistryTests).Assembly });

            Assert.Greater(count, 0);
            Assert.IsNotNull(registry.Find("probe_ok"));
            // 签名不符 / 空名的探针必须没被登记。
            Assert.IsNull(registry.Find("probe_not_static"));
            Assert.IsNull(registry.Find("probe_wrong_params"));
        }

        [Test]
        public void ScanLoadedAssemblies_DefaultScope_FindsProbeCommands()
        {
            var registry = new DebugCommandRegistry();

            var count = registry.ScanLoadedAssemblies();

            Assert.Greater(count, 0);
            Assert.IsNotNull(registry.Find("probe_ok"));
        }

        [Test]
        public void ScanLoadedAssemblies_WithPrefix_FiltersAssemblies()
        {
            var registry = new DebugCommandRegistry();

            var count = registry.ScanLoadedAssemblies(new[] { "Fang.Framework.CommandConsole.Editor.Tests" });

            Assert.Greater(count, 0);
            Assert.IsNotNull(registry.Find("probe_ok"));
        }

        [Test]
        public void Execute_UnknownCommand_ReturnsErrorWithHint()
        {
            var registry = CreateRegistry();

            var json = registry.ExecuteJson("no_such_cmd", Array.Empty<string>());

            StringAssert.Contains("未知指令", json);
            StringAssert.Contains("list_commands", json);
            StringAssert.Contains("false", json);
        }

        [Test]
        public void Execute_OkCommand_ReturnsData()
        {
            var registry = CreateRegistry();

            var json = registry.ExecuteJson("probe_ok", new[] { "42" });

            StringAssert.Contains("\"ok\":true", json);
            StringAssert.Contains("\"value\":42", json);
        }

        [Test]
        public void Execute_NullResult_ReturnsNullData()
        {
            var registry = CreateRegistry();

            var json = registry.ExecuteJson("probe_null", Array.Empty<string>());

            StringAssert.Contains("\"ok\":true", json);
            StringAssert.Contains("\"data\":null", json);
        }

        [Test]
        public void Execute_BusinessError_ReturnsErrorMessage()
        {
            var registry = CreateRegistry();

            var json = registry.ExecuteJson("probe_business_error", Array.Empty<string>());

            StringAssert.Contains("\"ok\":false", json);
            StringAssert.Contains("预期失败", json);
        }

        [Test]
        public void Execute_BadArgument_ReturnsBusinessError()
        {
            var registry = CreateRegistry();

            // probe_ok 里 GetInt(0) 会抛 DebugCommandException —— 参数错走「业务错误」通道。
            var json = registry.ExecuteJson("probe_ok", new[] { "abc" });

            StringAssert.Contains("\"ok\":false", json);
            StringAssert.Contains("必须是整数", json);
        }

        [Test]
        public void Execute_RuntimeError_LogsAndReturnsError()
        {
            var registry = CreateRegistry();

            LogAssert.Expect(UnityEngine.LogType.Error, new System.Text.RegularExpressions.Regex("执行异常"));

            var json = registry.ExecuteJson("probe_runtime_error", Array.Empty<string>());

            StringAssert.Contains("\"ok\":false", json);
            StringAssert.Contains("炸了", json);
        }

        [Test]
        public void Execute_ContextCarriesRegistry()
        {
            var registry = CreateRegistry();

            var json = registry.ExecuteJson("probe_context", Array.Empty<string>());

            StringAssert.Contains("\"hasRegistry\":true", json);
        }

        [Test]
        public void Execute_UsesContextFactory()
        {
            var registry = CreateRegistry();
            var calls = 0;

            registry.ContextFactory = () =>
            {
                calls++;
                return new DebugCommandContext(registry, null);
            };

            registry.ExecuteJson("probe_ok", new[] { "1" });
            registry.ExecuteJson("probe_ok", new[] { "2" });

            Assert.AreEqual(2, calls);
        }

        [Test]
        public void CallLog_NewestFirst_AndCarriesArgs()
        {
            var registry = CreateRegistry();

            registry.ExecuteJson("probe_ok", new[] { "7" });
            registry.ExecuteJson("no_such_cmd", new[] { "x" });

            var log = registry.GetCallLog();

            Assert.AreEqual(2, log.Count);
            Assert.AreEqual("no_such_cmd", log[0].Cmd);
            Assert.IsFalse(log[0].Ok);
            StringAssert.Contains("未知指令", log[0].Summary);
            CollectionAssert.AreEqual(new[] { "x" }, log[0].Args);

            Assert.AreEqual("probe_ok", log[1].Cmd);
            Assert.IsTrue(log[1].Ok);
            CollectionAssert.AreEqual(new[] { "7" }, log[1].Args);
        }

        [Test]
        public void CallLog_Overflow_DropsOldest()
        {
            var registry = CreateRegistry();

            var overflow = DebugCommandDefaults.CallLogMaxSize + 5;
            for (var i = 0; i < overflow; i++)
            {
                registry.ExecuteJson("probe_ok", new[] { i.ToString() });
            }

            var log = registry.GetCallLog();

            Assert.AreEqual(DebugCommandDefaults.CallLogMaxSize, log.Count);
            // 最新在前：第 overflow-1 次调用的参数是 overflow-1。
            CollectionAssert.AreEqual(
                new[] { (overflow - 1).ToString() },
                log[0].Args);
        }

        [Test]
        public void CallLog_LongSummary_IsTruncated()
        {
            var registry = new DebugCommandRegistry();
            registry.Register(
                "long_summary",
                "长摘要",
                "probe",
                Probe(nameof(DebugCommandProbes.Ok)));

            // 用一个超长参数让 data 的 JSON 超过摘要上限。
            registry.ExecuteJson("long_summary", new[] { new string('9', DebugCommandDefaults.SummaryMaxLength * 2) });

            var log = registry.GetCallLog();

            Assert.AreEqual(1, log.Count);
            StringAssert.Contains("...(截断)", log[0].Summary);
            Assert.Less(log[0].Summary.Length, DebugCommandDefaults.SummaryMaxLength + 32);
        }

        [Test]
        public void ClearCallLog_EmptiesLog_ButKeepsCommands()
        {
            var registry = CreateRegistry();
            registry.ExecuteJson("probe_ok", Array.Empty<string>());

            registry.ClearCallLog();

            Assert.AreEqual(0, registry.GetCallLog().Count);
            Assert.AreEqual(5, registry.Count);
        }

        [Test]
        public void Clear_RemovesCommands_ButKeepsCallLog()
        {
            var registry = CreateRegistry();
            registry.ExecuteJson("probe_ok", Array.Empty<string>());

            registry.Clear();

            Assert.AreEqual(0, registry.Count);
            Assert.IsNull(registry.Find("probe_ok"));
            Assert.AreEqual(1, registry.GetCallLog().Count);
        }
    }
}
