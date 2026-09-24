using System;
using System.Collections.Generic;
using System.IO;

namespace Fang.Framework.CommandConsole.Editor
{
    /// <summary>一条校验结果。<see cref="IsHint"/> = 只是提醒，不是问题。</summary>
    internal sealed class DebugCommandIssue
    {
        public DebugCommandIssue(string message, bool isHint)
        {
            Message = message;
            IsHint = isHint;
        }

        public string Message { get; }

        public bool IsHint { get; }
    }

    /// <summary>
    /// 纯逻辑校验（internal）：把「扫描器会打的警告」+「配置项的越界」汇成一张清单。
    /// 复用 <see cref="DebugCommandScanner"/> 的警告回调 —— 校验里看到的就是运行时 Console 里会看到的。
    /// </summary>
    internal static class DebugCommandValidator
    {
        public static IReadOnlyList<DebugCommandEntry> ScanEntries(DebugSettings settings, Action<string> warn)
        {
            settings = settings ?? DebugSettings.From(null);

            var prefixes = settings.CommandAssemblyNamePrefixes;
            if (prefixes != null && prefixes.Count > 0)
            {
                return DebugCommandScanner.Scan(AppDomain.CurrentDomain.GetAssemblies(), prefixes, warn);
            }

            return DebugCommandScanner.ScanReferencingAssemblies(warn);
        }

        public static IReadOnlyList<DebugCommandIssue> Validate(DebugProjectSo project, DebugSettings settings)
        {
            settings = settings ?? DebugSettings.From(project);

            var issues = new List<DebugCommandIssue>();

            var warnings = new List<string>();
            var entries = ScanEntries(settings, warnings.Add);

            for (var i = 0; i < warnings.Count; i++)
            {
                issues.Add(new DebugCommandIssue(warnings[i], false));
            }

            if (entries.Count == 0)
            {
                issues.Add(new DebugCommandIssue("没有扫到任何指令（检查「程序集前缀」扫描范围）。", false));
            }

            AppendCategoryIssues(issues, entries, settings.CategoryOrder);
            AppendConfigIssues(issues, project);

            return issues;
        }

        private static void AppendCategoryIssues(
            List<DebugCommandIssue> issues,
            IReadOnlyList<DebugCommandEntry> entries,
            IReadOnlyList<string> configuredOrder)
        {
            var reported = new HashSet<string>(StringComparer.Ordinal);
            var configured = new HashSet<string>(StringComparer.Ordinal);

            if (configuredOrder != null)
            {
                for (var i = 0; i < configuredOrder.Count; i++)
                {
                    configured.Add(configuredOrder[i]);
                }
            }

            for (var i = 0; i < entries.Count; i++)
            {
                var category = entries[i].Category;

                if (configured.Contains(category) || !reported.Add(category))
                {
                    continue;
                }

                issues.Add(new DebugCommandIssue(
                    "分类 '" + category + "' 不在「分类顺序」里，导出时会排在已列出的分类后面。",
                    true));
            }
        }

        private static void AppendConfigIssues(List<DebugCommandIssue> issues, DebugProjectSo project)
        {
            if (project == null)
            {
                issues.Add(new DebugCommandIssue(
                    "还没有调试项目配置，当前整套路用内置默认值（端口 " + DebugCommandDefaults.Port + "）。",
                    true));
                return;
            }

            // 这里看的是 SO 的原始值 —— DebugSettings 已经把越界值悄悄回落了，只看生效值就发现不了填错。
            if (project.Port < 1 || project.Port > 65535)
            {
                issues.Add(new DebugCommandIssue(
                    "端口 " + project.Port + " 越界（1~65535），实际会用 " + DebugCommandDefaults.Port + "。",
                    false));
            }

            if (string.IsNullOrWhiteSpace(project.ListenAddress))
            {
                issues.Add(new DebugCommandIssue(
                    "监听地址留空，实际会用 " + DebugCommandDefaults.ListenAddress + "。",
                    true));
            }

            if (string.IsNullOrWhiteSpace(project.DocFolder))
            {
                issues.Add(new DebugCommandIssue(
                    "文档目录留空，实际会用 " + DebugCommandDefaults.DocFolder + "。",
                    true));
            }

            if (string.IsNullOrWhiteSpace(project.DocFileName))
            {
                issues.Add(new DebugCommandIssue(
                    "文档文件名留空，实际会用 " + DebugCommandDefaults.DocFileName + "。",
                    true));
            }

            if (Path.IsPathRooted(project.DocFolder))
            {
                issues.Add(new DebugCommandIssue(
                    "文档目录必须是工程相对路径（例如 Docs），不能是绝对路径：" + project.DocFolder,
                    false));
            }
        }
    }
}
