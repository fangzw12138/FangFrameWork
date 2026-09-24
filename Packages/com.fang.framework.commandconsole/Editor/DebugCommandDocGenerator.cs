using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Fang.Framework.CommandConsole.Editor
{
    /// <summary>
    /// 指令清单导出（薄 IO 层）：解析配置 → 用与运行时同一个扫描器取条目 →
    /// <see cref="DebugCommandDocBuilder"/> 生成 → 落盘。
    ///
    /// 存在意义：AI 制定测试计划时游戏还没跑，不知道有哪些指令 —— 文档静态可读，与代码同步。
    /// </summary>
    public static class DebugCommandDocGenerator
    {
        [MenuItem(DebugCommandDocBuilder.ExportMenuPath)]
        public static void Export()
        {
            var project = DebugProjectLocator.Resolve();
            var settings = DebugSettings.From(project);

            var warnings = new List<string>();
            var entries = DebugCommandValidator.ScanEntries(settings, warnings.Add);

            if (entries.Count == 0)
            {
                Debug.LogWarning("[DebugCommand] 没扫到任何指令，文档未生成。");
                return;
            }

            var documents = DebugCommandDocBuilder.Build(entries, settings);
            var root = ProjectRoot();

            if (string.IsNullOrEmpty(root))
            {
                Debug.LogError("[DebugCommand] 定位不到工程根目录，文档未生成。");
                return;
            }

            var written = 0;
            var skipped = 0;

            foreach (var document in documents)
            {
                var absolute = Path.GetFullPath(Path.Combine(root, document.Key));

                if (!IsInside(root, absolute))
                {
                    Debug.LogError("[DebugCommand] 跳过越界路径（文档必须写在工程目录内）：" + document.Key);
                    skipped++;
                    continue;
                }

                var folder = Path.GetDirectoryName(absolute);
                if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                File.WriteAllText(absolute, document.Value, new UTF8Encoding(false));
                written++;
            }

            AssetDatabase.Refresh();

            var source = project != null ? "（配置：" + project.name + "）" : "（内置默认配置）";
            Debug.Log(
                "[DebugCommand] 指令清单已导出：" + written + " 个文件，" + entries.Count + " 条指令" + source
                + (skipped > 0 ? "，跳过 " + skipped + " 个越界路径" : string.Empty));

            if (written > 0)
            {
                EditorUtility.RevealInFinder(Path.Combine(root, DebugCommandDocBuilder.IndexPath(settings)));
            }
        }

        private static string ProjectRoot()
        {
            var parent = Directory.GetParent(Application.dataPath);
            return parent?.FullName;
        }

        /// <summary>目标路径必须落在工程根目录内（挡住绝对路径与 <c>..</c> 逃逸）。</summary>
        private static bool IsInside(string root, string absolute)
        {
            var normalizedRoot = Path.GetFullPath(root);
            if (!normalizedRoot.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                normalizedRoot += Path.DirectorySeparatorChar;
            }

            return absolute.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
        }
    }
}
