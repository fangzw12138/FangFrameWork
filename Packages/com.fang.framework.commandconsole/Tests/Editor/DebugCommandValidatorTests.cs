using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Fang.Framework.CommandConsole.Editor.Tests
{
    /// <summary>校验器：扫描警告转问题 + 配置项越界（含「原始 SO 值」这条关键区别）。</summary>
    public class DebugCommandValidatorTests
    {
        private const string TestAssemblyPrefix = "Fang.Framework.CommandConsole.Editor.Tests";

        private DebugProjectSo _project;

        [TearDown]
        public void TearDown()
        {
            DebugProjectProbe.Destroy(_project);
            _project = null;
        }

        private static IReadOnlyList<string> Errors(IReadOnlyList<DebugCommandIssue> issues)
        {
            return issues.Where(i => !i.IsHint).Select(i => i.Message).ToList();
        }

        private static IReadOnlyList<string> Hints(IReadOnlyList<DebugCommandIssue> issues)
        {
            return issues.Where(i => i.IsHint).Select(i => i.Message).ToList();
        }

        [Test]
        public void Validate_WithoutProject_ReportsHintAboutDefaults()
        {
            var settings = new DebugSettings
            {
                CategoryOrder = DebugCommandDefaults.CategoryOrder,
                DocFolder = DebugCommandDefaults.DocFolder,
                DocFileName = DebugCommandDefaults.DocFileName,
                CommandAssemblyNamePrefixes = new[] { TestAssemblyPrefix },
            };

            var issues = DebugCommandValidator.Validate(null, settings);

            Assert.IsTrue(Hints(issues).Any(h => h.Contains("内置默认值")), string.Join("\n", Hints(issues)));
        }

        [Test]
        public void Validate_ScanWarnings_BecomeErrors()
        {
            // 白名单只留测试程序集：那里有签名不符 / 空名 / 重名 / 没描述四个探针。
            var settings = new DebugSettings
            {
                CategoryOrder = DebugCommandDefaults.CategoryOrder,
                DocFolder = DebugCommandDefaults.DocFolder,
                DocFileName = DebugCommandDefaults.DocFileName,
                CommandAssemblyNamePrefixes = new[] { TestAssemblyPrefix },
            };

            var errors = Errors(DebugCommandValidator.Validate(null, settings));

            Assert.IsTrue(errors.Any(e => e.Contains("probe_not_static")), string.Join("\n", errors));
            Assert.IsTrue(errors.Any(e => e.Contains("指令名是空的")), string.Join("\n", errors));
            Assert.IsTrue(errors.Any(e => e.Contains("重名")), string.Join("\n", errors));
            Assert.IsTrue(errors.Any(e => e.Contains("没有描述")), string.Join("\n", errors));
        }

        [Test]
        public void Validate_NoCommands_ReportsError()
        {
            var settings = new DebugSettings
            {
                CategoryOrder = DebugCommandDefaults.CategoryOrder,
                DocFolder = DebugCommandDefaults.DocFolder,
                DocFileName = DebugCommandDefaults.DocFileName,
                CommandAssemblyNamePrefixes = new[] { "ZZZ.No.Such.Assembly" },
            };

            var errors = Errors(DebugCommandValidator.Validate(null, settings));

            Assert.IsTrue(errors.Any(e => e.Contains("没有扫到任何指令")), string.Join("\n", errors));
        }

        [Test]
        public void Validate_OutOfRangePort_ReportsError()
        {
            _project = DebugProjectProbe.New();
            DebugProjectProbe.SetInt(_project, "_port", 70000);
            DebugProjectProbe.SetStringList(_project, "_commandAssemblyNamePrefixes", new[] { TestAssemblyPrefix });

            var errors = Errors(DebugCommandValidator.Validate(_project, DebugSettings.From(_project)));

            Assert.IsTrue(errors.Any(e => e.Contains("越界")), string.Join("\n", errors));
        }

        [Test]
        public void Validate_AbsoluteDocFolder_ReportsError()
        {
            _project = DebugProjectProbe.New();
            DebugProjectProbe.SetString(_project, "_docFolder", "C:/Temp/Docs");
            DebugProjectProbe.SetStringList(_project, "_commandAssemblyNamePrefixes", new[] { TestAssemblyPrefix });

            var errors = Errors(DebugCommandValidator.Validate(_project, DebugSettings.From(_project)));

            Assert.IsTrue(errors.Any(e => e.Contains("工程相对路径")), string.Join("\n", errors));
        }

        [Test]
        public void Validate_BlankConfigFields_ReportHints()
        {
            _project = DebugProjectProbe.New();
            DebugProjectProbe.SetString(_project, "_listenAddress", "  ");
            DebugProjectProbe.SetString(_project, "_docFolder", "");
            DebugProjectProbe.SetString(_project, "_docFileName", " ");
            DebugProjectProbe.SetStringList(_project, "_commandAssemblyNamePrefixes", new[] { TestAssemblyPrefix });

            var hints = Hints(DebugCommandValidator.Validate(_project, DebugSettings.From(_project)));

            Assert.IsTrue(hints.Any(h => h.Contains("监听地址留空")), string.Join("\n", hints));
            Assert.IsTrue(hints.Any(h => h.Contains("文档目录留空")), string.Join("\n", hints));
            Assert.IsTrue(hints.Any(h => h.Contains("文档文件名留空")), string.Join("\n", hints));
        }

        [Test]
        public void Validate_CategoryNotInOrder_ReportsHint()
        {
            _project = DebugProjectProbe.New();
            DebugProjectProbe.SetStringList(_project, "_commandAssemblyNamePrefixes", new[] { TestAssemblyPrefix });
            DebugProjectProbe.SetStringList(_project, "_categoryOrder", new[] { "system" });

            var hints = Hints(DebugCommandValidator.Validate(_project, DebugSettings.From(_project)));

            Assert.IsTrue(hints.Any(h => h.Contains("'probe'")), string.Join("\n", hints));
        }

        [Test]
        public void Validate_CategoryInOrder_ProducesNoSuchHint()
        {
            _project = DebugProjectProbe.New();
            DebugProjectProbe.SetStringList(_project, "_commandAssemblyNamePrefixes", new[] { TestAssemblyPrefix });
            DebugProjectProbe.SetStringList(_project, "_categoryOrder", new[] { "probe" });

            var hints = Hints(DebugCommandValidator.Validate(_project, DebugSettings.From(_project)));

            Assert.IsFalse(hints.Any(h => h.Contains("不在「分类顺序」里")), string.Join("\n", hints));
        }

        [Test]
        public void ScanEntries_DefaultScope_FindsPackageCommands()
        {
            var entries = DebugCommandValidator.ScanEntries(DebugSettings.From(null), null);

            Assert.IsTrue(entries.Any(e => e.Name == "ping"));
            Assert.IsTrue(entries.Any(e => e.Name == "list_commands"));
        }
    }
}
