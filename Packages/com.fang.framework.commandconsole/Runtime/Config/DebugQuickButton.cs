using System;
using UnityEngine;

namespace Fang.Framework.CommandConsole
{
    /// <summary>
    /// 一个游戏内控制台快捷按钮：显示文字 + 点了要执行的一行指令。
    /// 刻意是普通可序列化类（不是 SO），这样项目配置 SO 里能直接内联编辑一串。
    /// </summary>
    [Serializable]
    public sealed class DebugQuickButton
    {
        [SerializeField] private string _label;

        [SerializeField] private string _commandLine;

        public DebugQuickButton()
        {
        }

        public DebugQuickButton(string label, string commandLine)
        {
            _label = label;
            _commandLine = commandLine;
        }

        /// <summary>按钮上显示的文字；留空就用指令行本身。</summary>
        public string Label => string.IsNullOrWhiteSpace(_label) ? _commandLine : _label;

        /// <summary>点了执行的指令行（指令名 + 空格分隔参数）。</summary>
        public string CommandLine => _commandLine ?? string.Empty;
    }
}
