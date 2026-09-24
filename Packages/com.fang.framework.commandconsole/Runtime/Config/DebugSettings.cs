using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fang.Framework.CommandConsole
{
    /// <summary>
    /// 一份「生效配置」快照：<see cref="From"/> 把项目配置 SO（或 null）解析成实际要用的值，
    /// <b>回落规则只在这一处</b> —— 服务、文档生成器、FangHub 页都读它，不会各写一套默认值。
    ///
    /// 回落规则：字符串留白 / 端口越界 / 列表清空 → 取 <see cref="DebugCommandDefaults"/>；
    /// 列表会去空白、去重（保持原顺序），但<b>不排序</b> —— 顺序是配置的表达力。
    /// </summary>
    public sealed class DebugSettings
    {
        private static readonly DebugQuickButton[] NoButtons = Array.Empty<DebugQuickButton>();

        private static readonly string[] NoStrings = Array.Empty<string>();

        /// <summary>自动开始监听。</summary>
        public bool AutoStart { get; internal set; }

        /// <summary>监听地址（IP）。</summary>
        public string ListenAddress { get; internal set; }

        /// <summary>监听端口（1~65535）。</summary>
        public int Port { get; internal set; }

        /// <summary>是否创建游戏内控制台。</summary>
        public bool ConsoleEnabled { get; internal set; }

        /// <summary>游戏内控制台开关快捷键。</summary>
        public KeyCode ConsoleToggleKey { get; internal set; }

        /// <summary>额外的快捷按钮（内置三个不在这里）。</summary>
        public IReadOnlyList<DebugQuickButton> QuickButtons { get; internal set; }

        /// <summary>扫描用的程序集名前缀白名单；空 = 只扫引用了本包的程序集。</summary>
        public IReadOnlyList<string> CommandAssemblyNamePrefixes { get; internal set; }

        /// <summary>文档分类顺序；永远非空。</summary>
        public IReadOnlyList<string> CategoryOrder { get; internal set; }

        /// <summary>文档输出目录（工程相对路径）。</summary>
        public string DocFolder { get; internal set; }

        /// <summary>文档文件名。</summary>
        public string DocFileName { get; internal set; }

        /// <summary>是否按分类拆文件。</summary>
        public bool DocSplitByCategory { get; internal set; }

        /// <summary>把项目配置解析成生效配置；<paramref name="project"/> 为 null 时整套走默认值。</summary>
        public static DebugSettings From(DebugProjectSo project)
        {
            if (project == null)
            {
                return new DebugSettings
                {
                    AutoStart = DebugCommandDefaults.AutoStart,
                    ListenAddress = DebugCommandDefaults.ListenAddress,
                    Port = DebugCommandDefaults.Port,
                    ConsoleEnabled = DebugCommandDefaults.ConsoleEnabled,
                    ConsoleToggleKey = DebugCommandDefaults.ConsoleToggleKey,
                    QuickButtons = NoButtons,
                    CommandAssemblyNamePrefixes = NoStrings,
                    CategoryOrder = DebugCommandDefaults.CategoryOrder,
                    DocFolder = DebugCommandDefaults.DocFolder,
                    DocFileName = DebugCommandDefaults.DocFileName,
                    DocSplitByCategory = DebugCommandDefaults.DocSplitByCategory,
                };
            }

            return new DebugSettings
            {
                AutoStart = project.AutoStart,
                ListenAddress = NormalizeText(project.ListenAddress, DebugCommandDefaults.ListenAddress),
                Port = NormalizePort(project.Port),
                ConsoleEnabled = project.ConsoleEnabled,
                ConsoleToggleKey = project.ConsoleToggleKey,
                QuickButtons = Buttons(project.QuickButtons),
                CommandAssemblyNamePrefixes = NormalizeList(project.CommandAssemblyNamePrefixes, NoStrings),
                CategoryOrder = NormalizeList(project.CategoryOrder, DebugCommandDefaults.CategoryOrder),
                DocFolder = NormalizeText(project.DocFolder, DebugCommandDefaults.DocFolder),
                DocFileName = NormalizeText(project.DocFileName, DebugCommandDefaults.DocFileName),
                DocSplitByCategory = project.DocSplitByCategory,
            };
        }

        /// <summary>留白 → 回落；否则去掉首尾空白。</summary>
        public static string NormalizeText(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        /// <summary>端口越界（含 0）→ 回落。SO 里的端口不接受「0 = 系统分配」这种隐式含义。</summary>
        public static int NormalizePort(int value)
        {
            return value >= 1 && value <= 65535 ? value : DebugCommandDefaults.Port;
        }

        /// <summary>去空白、去重（序数）、保持顺序；结果为空则回落。</summary>
        public static IReadOnlyList<string> NormalizeList(IReadOnlyList<string> values, IReadOnlyList<string> fallback)
        {
            if (values == null)
            {
                return fallback;
            }

            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            for (var i = 0; i < values.Count; i++)
            {
                var value = values[i];
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                var trimmed = value.Trim();
                if (seen.Add(trimmed))
                {
                    result.Add(trimmed);
                }
            }

            return result.Count == 0 ? fallback : result;
        }

        private static IReadOnlyList<DebugQuickButton> Buttons(IReadOnlyList<DebugQuickButton> buttons)
        {
            if (buttons == null)
            {
                return NoButtons;
            }

            var result = new List<DebugQuickButton>();
            for (var i = 0; i < buttons.Count; i++)
            {
                var button = buttons[i];
                if (button != null && !string.IsNullOrWhiteSpace(button.CommandLine))
                {
                    result.Add(button);
                }
            }

            return result.Count == 0 ? NoButtons : result;
        }
    }
}
