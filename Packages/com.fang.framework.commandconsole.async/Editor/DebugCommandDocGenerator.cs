#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Fang.Framework.CommandConsole.Async.Editor
{
    /// <summary>
    /// 把当前能看到的指令导出成 <c>Docs/DebugCommands.md</c>。
    /// 优先用活实例的注册表；没有活实例时退回编辑器侧扫描。
    /// </summary>
    public static class DebugCommandDocGenerator
    {
        /// <summary>输出路径（相对工程根）。</summary>
        public const string OutputRelativePath = "Docs/DebugCommands.md";

        [MenuItem("Fang/调试/导出指令文档", false, 11)]
        public static void GenerateFromMenu()
        {
            var path = Generate();
            Debug.Log($"{DebugCommandRegistry.LogTag} 指令文档已生成：{path}");
        }

        /// <summary>生成文档并返回绝对路径。</summary>
        public static string Generate()
        {
            var entries = Collect();
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            var targetPath = Path.Combine(projectRoot, OutputRelativePath.Replace('/', Path.DirectorySeparatorChar));
            var directory = Path.GetDirectoryName(targetPath);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(targetPath, BuildMarkdown(entries), new UTF8Encoding(false));

            return targetPath;
        }

        private static List<DebugCommandEntry> Collect()
        {
            var services = DebugCommandService.ActiveServices;

            for (var i = 0; i < services.Count; i++)
            {
                var registry = services[i] != null ? services[i].Registry : null;

                if (registry != null && registry.Count > 0)
                {
                    return new List<DebugCommandEntry>(registry.Commands);
                }
            }

            var fallback = new DebugCommandRegistry();
            fallback.ScanLoadedAssemblies();

            return new List<DebugCommandEntry>(fallback.Commands);
        }

        private static string BuildMarkdown(List<DebugCommandEntry> entries)
        {
            entries.Sort(CompareEntries);

            var builder = new StringBuilder();

            builder.AppendLine("# 调试指令清单");
            builder.AppendLine();
            builder.AppendLine($"> 生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}　指令总数：{entries.Count}");
            builder.AppendLine();
            builder.AppendLine("调用方式：TCP NDJSON（默认 `127.0.0.1:7777`），或编辑器菜单 `Fang/调试/指令控制台`。");
            builder.AppendLine();

            if (entries.Count == 0)
            {
                builder.AppendLine("（当前没有扫描到任何指令。）");

                return builder.ToString();
            }

            string currentCategory = null;

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];

                if (!string.Equals(currentCategory, entry.Category, StringComparison.Ordinal))
                {
                    currentCategory = entry.Category;

                    builder.AppendLine($"## {currentCategory}");
                    builder.AppendLine();
                    builder.AppendLine("| 指令 | 说明 | 来源 |");
                    builder.AppendLine("| --- | --- | --- |");
                }

                builder.AppendLine($"| `{entry.Name}` | {Escape(entry.Description)} | `{entry.Source}` |");
            }

            builder.AppendLine();

            return builder.ToString();
        }

        private static int CompareEntries(DebugCommandEntry left, DebugCommandEntry right)
        {
            var byCategory = string.Compare(left.Category, right.Category, StringComparison.Ordinal);

            return byCategory != 0 ? byCategory : string.CompareOrdinal(left.Name, right.Name);
        }

        private static string Escape(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return "（无说明）";
            }

            return text.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
        }
    }
}
#endif
