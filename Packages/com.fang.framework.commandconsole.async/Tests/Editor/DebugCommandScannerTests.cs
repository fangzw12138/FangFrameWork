#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using NUnit.Framework;

namespace Fang.Framework.CommandConsole.Async.Tests
{
    /// <summary>扫描器用的固定装置：合规与各种「不合规」的方法各一份。</summary>
    internal class ScannerFixtures
    {
        [DebugCommand("fixture_ok", "合规指令", "test")]
        public static object Ok(DebugCommandContext context, DebugCommandArgs args)
        {
            return new { value = "ok" };
        }

        [DebugCommand("fixture_dup", "重名·先出现", "test")]
        public static object DuplicateFirst(DebugCommandContext context, DebugCommandArgs args)
        {
            return new { value = "first" };
        }

        [DebugCommand("fixture_dup", "重名·后出现（应覆盖）", "test")]
        public static object DuplicateSecond(DebugCommandContext context, DebugCommandArgs args)
        {
            return new { value = "second" };
        }

        [DebugCommand("fixture_not_static", "非 static，应被拒绝", "test")]
        public object NotStatic(DebugCommandContext context, DebugCommandArgs args)
        {
            return null;
        }

        [DebugCommand("fixture_bad_return", "返回类型不是 object，应被拒绝", "test")]
        public static string BadReturn(DebugCommandContext context, DebugCommandArgs args)
        {
            return null;
        }

        [DebugCommand("fixture_bad_params", "形参不对，应被拒绝", "test")]
        public static object BadParameters(DebugCommandContext context)
        {
            return null;
        }

        [DebugCommand("  ", "名称为空，应被拒绝", "test")]
        public static object EmptyName(DebugCommandContext context, DebugCommandArgs args)
        {
            return null;
        }
    }

    public class DebugCommandScannerTests
    {
        private List<string> _warnings;

        [SetUp]
        public void SetUp()
        {
            _warnings = new List<string>();
        }

        [Test]
        public void Scan_registers_only_well_formed_commands()
        {
            var entries = ScanFixtureAssembly();
            var names = entries.ConvertAll(entry => entry.Name);

            CollectionAssert.Contains(names, "fixture_ok");
            CollectionAssert.Contains(names, "fixture_dup");
            CollectionAssert.DoesNotContain(names, "fixture_not_static");
            CollectionAssert.DoesNotContain(names, "fixture_bad_return");
            CollectionAssert.DoesNotContain(names, "fixture_bad_params");
            CollectionAssert.DoesNotContain(names, string.Empty);
        }

        [Test]
        public void Scan_warns_once_for_each_rejected_candidate()
        {
            ScanFixtureAssembly();

            Assert.IsTrue(_warnings.Exists(warning => warning.Contains("NotStatic") && warning.Contains("static")));
            Assert.IsTrue(_warnings.Exists(warning => warning.Contains("BadReturn")));
            Assert.IsTrue(_warnings.Exists(warning => warning.Contains("BadParameters")));
            Assert.IsTrue(_warnings.Exists(warning => warning.Contains("EmptyName")));
        }

        [Test]
        public void Scan_keeps_the_last_registration_for_a_duplicate_name()
        {
            var entries = ScanFixtureAssembly();
            var duplicates = entries.FindAll(entry => entry.Name == "fixture_dup");

            Assert.AreEqual(1, duplicates.Count);
            Assert.AreEqual("DuplicateSecond", duplicates[0].Method.Name);
            Assert.IsTrue(_warnings.Exists(warning => warning.Contains("fixture_dup") && warning.Contains("重复")));
        }

        [Test]
        public void Scan_describes_each_entry_with_type_dot_method()
        {
            var entries = ScanFixtureAssembly();
            var entry = entries.Find(item => item.Name == "fixture_ok");

            Assert.IsNotNull(entry);
            Assert.AreEqual("ScannerFixtures.Ok", entry.Source);
            Assert.AreEqual("test", entry.Category);
            Assert.AreEqual("合规指令", entry.Description);
        }

        [Test]
        public void Scan_with_a_non_matching_prefix_returns_nothing()
        {
            var entries = DebugCommandScanner.Scan(
                new[] { typeof(ScannerFixtures).Assembly },
                new[] { "Fang.NoSuchPrefix" },
                _warnings.Add);

            Assert.AreEqual(0, entries.Count);
        }

        [Test]
        public void HasAnyPrefix_ignores_blank_prefixes()
        {
            Assert.IsFalse(DebugCommandScanner.HasAnyPrefix(null));
            Assert.IsFalse(DebugCommandScanner.HasAnyPrefix(new[] { string.Empty, "   " }));
            Assert.IsTrue(DebugCommandScanner.HasAnyPrefix(new[] { "Fang" }));
        }

        [Test]
        public void ScanReferencingAssemblies_finds_commands_in_this_test_assembly()
        {
            var entries = DebugCommandScanner.ScanReferencingAssemblies(
                System.AppDomain.CurrentDomain.GetAssemblies(),
                _warnings.Add);

            Assert.IsTrue(entries.Exists(entry => entry.Name == "fixture_ok"));
        }

        [Test]
        public void IsTestAssembly_recognizes_this_test_assembly()
        {
            Assert.IsTrue(DebugCommandScanner.IsTestAssembly(typeof(DebugCommandScannerTests).Assembly));
            Assert.IsFalse(DebugCommandScanner.IsTestAssembly(typeof(DebugCommandScanner).Assembly));
            Assert.IsFalse(DebugCommandScanner.IsTestAssembly(null));
        }

        private List<DebugCommandEntry> ScanFixtureAssembly()
        {
            return DebugCommandScanner.Scan(new[] { typeof(ScannerFixtures).Assembly }, null, _warnings.Add);
        }
    }
}
#endif
