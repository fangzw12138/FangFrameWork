using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Fang.Framework.CommandConsole.Editor.Tests
{
    /// <summary>
    /// <see cref="DebugCommandScanner"/>：签名校验、空名、重名、程序集范围、排序。
    /// 警告走回调收集，不写 Console（测试输出干净）。
    /// </summary>
    public class DebugCommandScannerTests
    {
        private static List<string> ScanTestAssembly(
            IEnumerable<string> prefixes,
            out IReadOnlyList<DebugCommandEntry> entries)
        {
            var warnings = new List<string>();
            entries = DebugCommandScanner.Scan(
                new[] { typeof(DebugCommandScannerTests).Assembly },
                prefixes,
                warnings.Add);
            return warnings;
        }

        private static List<string> ScanTestAssembly(out IReadOnlyList<DebugCommandEntry> entries)
        {
            return ScanTestAssembly(null, out entries);
        }

        [Test]
        public void Scan_FindsProbeCommands()
        {
            ScanTestAssembly(out var entries);

            var names = entries.Select(e => e.Name).ToList();

            CollectionAssert.Contains(names, "probe_ok");
            CollectionAssert.Contains(names, "probe_null");
            CollectionAssert.Contains(names, "probe_business_error");
            CollectionAssert.Contains(names, "probe_runtime_error");
        }

        [Test]
        public void Scan_SkipsNonStaticAndWrongParameters_WithWarning()
        {
            var warnings = ScanTestAssembly(out var entries);

            var names = entries.Select(e => e.Name).ToList();

            CollectionAssert.DoesNotContain(names, "probe_not_static");
            CollectionAssert.DoesNotContain(names, "probe_wrong_params");

            Assert.IsTrue(
                warnings.Any(w => w.Contains("probe_not_static") && w.Contains("签名")),
                "应警告非 static 的探针：\n" + string.Join("\n", warnings));
            Assert.IsTrue(
                warnings.Any(w => w.Contains("probe_wrong_params") && w.Contains("签名")),
                "应警告参数不符的探针：\n" + string.Join("\n", warnings));
        }

        [Test]
        public void Scan_SkipsEmptyName_WithWarning()
        {
            var warnings = ScanTestAssembly(out var entries);

            Assert.IsFalse(entries.Any(e => string.IsNullOrWhiteSpace(e.Name)));
            Assert.IsTrue(warnings.Any(w => w.Contains("指令名是空的")), string.Join("\n", warnings));
        }

        [Test]
        public void Scan_KeepsEntryButWarns_WhenDescriptionEmpty()
        {
            var warnings = ScanTestAssembly(out var entries);

            var entry = entries.FirstOrDefault(e => e.Name == "probe_no_description");

            Assert.IsNotNull(entry, "描述为空不该被跳过");
            Assert.AreEqual(string.Empty, entry.Description);
            Assert.IsTrue(warnings.Any(w => w.Contains("没有描述")), string.Join("\n", warnings));
        }

        [Test]
        public void Scan_DuplicateName_KeepsOneEntry_WithWarning()
        {
            var warnings = ScanTestAssembly(out var entries);

            Assert.AreEqual(1, entries.Count(e => e.Name == "probe_duplicate"));
            Assert.IsTrue(warnings.Any(w => w.Contains("重名")), string.Join("\n", warnings));
        }

        [Test]
        public void Scan_CategoryEmpty_FallsBackToSystem()
        {
            var warnings = ScanTestAssembly(out var entries);

            // 探针都标了 probe 域，这里只验证「分类不会是空」这条不变量。
            Assert.IsFalse(entries.Any(e => string.IsNullOrWhiteSpace(e.Category)), string.Join("\n", warnings));
            Assert.AreEqual("probe", entries.First(e => e.Name == "probe_ok").Category);
        }

        [Test]
        public void Scan_AssemblyPrefixFilter_ExcludesEverything_WhenNoMatch()
        {
            var warnings = ScanTestAssembly(new[] { "ZZZ.NoSuch.Assembly" }, out var entries);

            Assert.AreEqual(0, entries.Count);
            Assert.AreEqual(0, warnings.Count);
        }

        [Test]
        public void Scan_AssemblyPrefixFilter_MatchesOwnAssembly()
        {
            ScanTestAssembly(new[] { "Fang.Framework.CommandConsole" }, out var entries);

            Assert.IsTrue(entries.Any(e => e.Name == "probe_ok"));
        }

        [Test]
        public void Scan_ForeignAssembly_YieldsNothingAndDoesNotThrow()
        {
            var warnings = new List<string>();

            // 顺手验证「扫一个没有指令的大程序集」不会抛（ReflectionTypeLoadException 路径）。
            var entries = DebugCommandScanner.Scan(
                new[] { typeof(object).Assembly },
                null,
                warnings.Add);

            Assert.AreEqual(0, entries.Count);
        }

        [Test]
        public void Scan_NullAssemblies_ReturnsEmpty()
        {
            Assert.AreEqual(0, DebugCommandScanner.Scan(null).Count);
        }

        [Test]
        public void Scan_ReturnsEntriesSortedByName()
        {
            ScanTestAssembly(out var entries);

            var names = entries.Select(e => e.Name).ToList();
            var sorted = names.OrderBy(n => n, StringComparer.Ordinal).ToList();

            CollectionAssert.AreEqual(sorted, names);
        }

        [Test]
        public void Scan_EntryCarriesSource()
        {
            ScanTestAssembly(out var entries);

            var entry = entries.First(e => e.Name == "probe_ok");

            StringAssert.Contains("DebugCommandProbes", entry.Source);
            StringAssert.Contains("Ok", entry.Source);
        }

        [Test]
        public void HasCommandSignature_AcceptsConvention()
        {
            var method = typeof(DebugCommandProbes).GetMethod(nameof(DebugCommandProbes.Ok));

            Assert.IsTrue(DebugCommandScanner.HasCommandSignature(method));
        }

        [Test]
        public void HasCommandSignature_RejectsDeviations()
        {
            var notStatic = typeof(DebugCommandProbeHost).GetMethod(nameof(DebugCommandProbeHost.NotStatic));
            var wrongParams = typeof(DebugCommandProbeHost).GetMethod(nameof(DebugCommandProbeHost.WrongParams));

            Assert.IsFalse(DebugCommandScanner.HasCommandSignature(notStatic));
            Assert.IsFalse(DebugCommandScanner.HasCommandSignature(wrongParams));
            Assert.IsFalse(DebugCommandScanner.HasCommandSignature(null));
        }

        [Test]
        public void ScanReferencingAssemblies_FindsProbeCommands()
        {
            var warnings = new List<string>();

            // 默认范围：只扫引用了本包的程序集。测试程序集引用了本包，所以探针必须被扫到。
            var entries = DebugCommandScanner.ScanReferencingAssemblies(warnings.Add);

            Assert.IsTrue(entries.Any(e => e.Name == "probe_ok"), string.Join("\n", warnings));
        }

        [Test]
        public void ReferencesPackage_TrueForOwnAndReferencingAssemblies()
        {
            Assert.IsTrue(DebugCommandScanner.ReferencesPackage(typeof(DebugCommandScanner).Assembly));
            Assert.IsTrue(DebugCommandScanner.ReferencesPackage(typeof(DebugCommandScannerTests).Assembly));
        }

        [Test]
        public void ReferencesPackage_FalseForForeignAssembly()
        {
            Assert.IsFalse(DebugCommandScanner.ReferencesPackage(typeof(object).Assembly));
            Assert.IsFalse(DebugCommandScanner.ReferencesPackage(null));
        }

        [Test]
        public void DescribeSignature_RendersReadableText()
        {
            var method = typeof(DebugCommandProbeHost).GetMethod(nameof(DebugCommandProbeHost.NotStatic));

            var text = DebugCommandScanner.DescribeSignature(method);

            StringAssert.Contains("NotStatic", text);
            StringAssert.Contains("DebugCommandContext", text);
            Assert.IsFalse(text.StartsWith("static", StringComparison.Ordinal), "非 static 不该被描述成 static：" + text);
        }
    }
}
