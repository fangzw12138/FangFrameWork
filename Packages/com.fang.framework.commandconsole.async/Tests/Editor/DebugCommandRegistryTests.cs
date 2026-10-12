#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Fang.Framework.CommandConsole.Async.Tests
{
    /// <summary>注册表用的固定装置。</summary>
    internal static class RegistryFixtures
    {
        [DebugCommand("reg_ok", "成功返回", "registry")]
        public static object Ok(DebugCommandContext context, DebugCommandArgs args)
        {
            return new { value = 42, echo = args.GetOrDefault(0, "echo", null) };
        }

        [DebugCommand("reg_business", "抛业务错误", "registry")]
        public static object Business(DebugCommandContext context, DebugCommandArgs args)
        {
            throw new DebugCommandException("参数不够用");
        }

        [DebugCommand("reg_crash", "抛意外异常", "registry")]
        public static object Crash(DebugCommandContext context, DebugCommandArgs args)
        {
            throw new InvalidOperationException("真的炸了");
        }

        [DebugCommand("reg_args", "需要参数", "registry")]
        public static object Args(DebugCommandContext context, DebugCommandArgs args)
        {
            return new { count = args.Count, first = args.Get(0, "first") };
        }
    }

    public class DebugCommandRegistryTests
    {
        [Test]
        public void Execute_returns_ok_and_data()
        {
            var registry = CreateRegistry();

            var result = registry.Execute("reg_ok", new[] { "hello" });

            Assert.IsTrue(result.Value<bool>("ok"));
            Assert.AreEqual(42, result["data"].Value<int>("value"));
            Assert.AreEqual("hello", result["data"].Value<string>("echo"));
        }

        [Test]
        public void Execute_maps_a_business_exception_to_error_without_logging_an_error()
        {
            var registry = CreateRegistry();

            var result = registry.Execute("reg_business", Array.Empty<string>());

            Assert.IsFalse(result.Value<bool>("ok"));
            StringAssert.Contains("参数不够用", result.Value<string>("error"));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Execute_maps_an_unexpected_exception_to_error_and_logs_an_error()
        {
            var registry = CreateRegistry();
            LogAssert.Expect(LogType.Error, new Regex("reg_crash"));

            var result = registry.Execute("reg_crash", Array.Empty<string>());

            Assert.IsFalse(result.Value<bool>("ok"));
            StringAssert.Contains("真的炸了", result.Value<string>("error"));
        }

        [Test]
        public void Execute_reports_an_unknown_command()
        {
            var registry = CreateRegistry();

            var result = registry.Execute("no_such_command", Array.Empty<string>());

            Assert.IsFalse(result.Value<bool>("ok"));
            StringAssert.Contains("未知指令", result.Value<string>("error"));
        }

        [Test]
        public void Execute_surfaces_a_missing_argument_as_a_business_error()
        {
            var registry = CreateRegistry();

            var result = registry.Execute("reg_args", Array.Empty<string>());

            Assert.IsFalse(result.Value<bool>("ok"));
            StringAssert.Contains("first", result.Value<string>("error"));
            StringAssert.Contains("位置 0", result.Value<string>("error"));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Execute_without_a_context_factory_reports_an_error()
        {
            var registry = new DebugCommandRegistry();
            registry.Register(FindFixtureEntry("reg_ok"));

            var result = registry.Execute("reg_ok", Array.Empty<string>());

            Assert.IsFalse(result.Value<bool>("ok"));
            StringAssert.Contains("上下文不可用", result.Value<string>("error"));
        }

        [Test]
        public void Call_log_is_newest_first_and_capped_at_200()
        {
            var registry = CreateRegistry();
            var total = DebugCommandRegistry.CallLogCapacity + 25;

            for (var i = 0; i < total; i++)
            {
                registry.Execute("reg_ok", new[] { i.ToString() });
            }

            var log = registry.GetCallLog();

            Assert.AreEqual(DebugCommandRegistry.CallLogCapacity, log.Count);
            Assert.AreEqual(total, log[0].Sequence);
            Assert.AreEqual(total - DebugCommandRegistry.CallLogCapacity + 1, log[log.Count - 1].Sequence);
            Assert.AreEqual((total - 1).ToString(), log[0].ArgsText);
            Assert.IsTrue(log[0].Succeeded);
        }

        [Test]
        public void Call_log_records_the_failure_reason()
        {
            var registry = CreateRegistry();

            registry.Execute("reg_business", Array.Empty<string>());

            var log = registry.GetCallLog();

            Assert.AreEqual(1, log.Count);
            Assert.IsFalse(log[0].Succeeded);
            StringAssert.Contains("参数不够用", log[0].Error);
        }

        [Test]
        public void ClearCallLog_empties_the_ring_buffer()
        {
            var registry = CreateRegistry();

            registry.Execute("reg_ok", Array.Empty<string>());
            registry.ClearCallLog();

            Assert.AreEqual(0, registry.GetCallLog().Count);
        }

        [Test]
        public void Commands_are_sorted_by_name()
        {
            var registry = CreateRegistry();
            var names = new List<string>();

            foreach (var entry in registry.Commands)
            {
                names.Add(entry.Name);
            }

            var sorted = new List<string>(names);
            sorted.Sort(StringComparer.Ordinal);

            CollectionAssert.AreEqual(sorted, names);
        }

        [Test]
        public void ExecuteText_accepts_a_plain_text_line()
        {
            var registry = CreateRegistry();

            var parsed = JObject.Parse(registry.ExecuteText("reg_ok world"));

            Assert.IsTrue(parsed.Value<bool>("ok"));
            Assert.AreEqual("world", parsed["data"].Value<string>("echo"));
        }

        [Test]
        public void ExecuteText_reports_an_empty_line()
        {
            var registry = CreateRegistry();

            var parsed = JObject.Parse(registry.ExecuteText("   "));

            Assert.IsFalse(parsed.Value<bool>("ok"));
        }

        [Test]
        public void ExecuteRequest_accepts_ndjson()
        {
            var registry = CreateRegistry();

            var parsed = JObject.Parse(registry.ExecuteRequest("{\"cmd\":\"reg_ok\",\"args\":[\"json\"]}"));

            Assert.IsTrue(parsed.Value<bool>("ok"));
            Assert.AreEqual("json", parsed["data"].Value<string>("echo"));
        }

        [Test]
        public void ExecuteRequest_reports_broken_json()
        {
            var registry = CreateRegistry();

            var parsed = JObject.Parse(registry.ExecuteRequest("{ not json"));

            Assert.IsFalse(parsed.Value<bool>("ok"));
            StringAssert.Contains("JSON", parsed.Value<string>("error"));
        }

        [Test]
        public void ExecuteRequest_accepts_a_json_string_argument_list()
        {
            var registry = CreateRegistry();

            var parsed = JObject.Parse(registry.ExecuteRequest("{\"cmd\":\"reg_ok\",\"args\":\"a b\"}"));

            Assert.IsTrue(parsed.Value<bool>("ok"));
            Assert.AreEqual("a", parsed["data"].Value<string>("echo"));
        }

        [Test]
        public void ScanLoadedAssemblies_finds_the_built_in_commands_and_skips_test_assemblies()
        {
            var registry = new DebugCommandRegistry();

            var count = registry.ScanLoadedAssemblies();

            Assert.Greater(count, 0);
            Assert.IsNotNull(registry.Find("ping"));
            Assert.IsNotNull(registry.Find("list_commands"));
            Assert.IsNotNull(registry.Find("command_log"));

            // 本测试程序集引用了 nunit.framework，属于测试程序集，默认扫描会跳过它。
            Assert.IsNull(registry.Find("reg_ok"));
            Assert.IsNull(registry.Find("fixture_ok"));
        }

        private static DebugCommandRegistry CreateRegistry()
        {
            var registry = new DebugCommandRegistry();

            // 没有活 Scope：查找起点为 null，GetService<T>() 只会返回 null。
            registry.ContextFactory = () => new DebugCommandContext(registry, () => null);

            var warnings = new List<string>();

            foreach (var entry in DebugCommandScanner.Scan(new[] { typeof(RegistryFixtures).Assembly }, null, warnings.Add))
            {
                registry.Register(entry);
            }

            return registry;
        }

        private static DebugCommandEntry FindFixtureEntry(string name)
        {
            var entries = DebugCommandScanner.Scan(new[] { typeof(RegistryFixtures).Assembly }, null, _ => { });

            return entries.Find(entry => entry.Name == name);
        }
    }
}
#endif
