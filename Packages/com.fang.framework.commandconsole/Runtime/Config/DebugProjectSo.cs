using System.Collections.Generic;
using Fang.Framework;
using UnityEngine;

namespace Fang.Framework.CommandConsole
{
    /// <summary>
    /// 调试项目配置：<b>运行参数</b>（自动启动 / 监听地址 / 端口 / 控制台开关与快捷键 / 快捷按钮 / 扫描范围）、
    /// <b>分类顺序</b>、<b>文档输出</b>。
    ///
    /// 只管这三件事，不做「资产清单」。字段都种上了内置默认值，肉眼可见、随手可改；
    /// 把某一项清空（字符串留白、端口填 0、列表清空）就回落到 <see cref="DebugCommandDefaults"/>，
    /// 回落规则集中在 <see cref="DebugSettings.From"/> 一处。
    ///
    /// 分类名与指令名一律是<b>自由字符串</b>：没有枚举、没有常量表。
    /// </summary>
    [CreateAssetMenu(fileName = "DebugProject", menuName = "Fang Framework/指令/项目配置")]
    public sealed class DebugProjectSo : ConfigDataSo
    {
        [Header("运行参数")]
        [Tooltip("进入游戏就自动开始监听。关掉后要自己调 StartTransport()。")]
        [SerializeField] private bool _autoStart = DebugCommandDefaults.AutoStart;

        [Tooltip("监听地址，必须是 IP（例如 127.0.0.1）。")]
        [SerializeField] private string _listenAddress = DebugCommandDefaults.ListenAddress;

        [Tooltip("监听端口（1~65535）。")]
        [SerializeField] private int _port = DebugCommandDefaults.Port;

        [Tooltip("是否创建游戏内控制台（IMGUI）。")]
        [SerializeField] private bool _consoleEnabled = DebugCommandDefaults.ConsoleEnabled;

        [Tooltip("游戏内控制台的开关快捷键。")]
        [SerializeField] private KeyCode _consoleToggleKey = DebugCommandDefaults.ConsoleToggleKey;

        [Tooltip("控制台上的快捷按钮。内置的 ping / list_commands / command_log 始终在最前面，这里加的是额外的。")]
        [SerializeField] private List<DebugQuickButton> _quickButtons = new List<DebugQuickButton>();

        [Header("扫描范围")]
        [Tooltip("程序集名前缀白名单。留空 = 只扫引用了本包的程序集（快得多，且扫到的指令一致）。")]
        [SerializeField] private List<string> _commandAssemblyNamePrefixes = new List<string>();

        [Header("文档")]
        [Tooltip("分类顺序。留空 = 内置指令分类在最前、其余按名称排序；未知分类排在已列出的后面。")]
        [SerializeField] private List<string> _categoryOrder = new List<string>();

        [Tooltip("文档输出目录（工程相对路径，例如 Docs）。")]
        [SerializeField] private string _docFolder = DebugCommandDefaults.DocFolder;

        [Tooltip("文档文件名（例如 DebugCommands.md）。")]
        [SerializeField] private string _docFileName = DebugCommandDefaults.DocFileName;

        [Tooltip("按分类拆成多个文件（总索引仍然生成）。")]
        [SerializeField] private bool _docSplitByCategory = DebugCommandDefaults.DocSplitByCategory;

        public bool AutoStart => _autoStart;

        public string ListenAddress => _listenAddress;

        public int Port => _port;

        public bool ConsoleEnabled => _consoleEnabled;

        public KeyCode ConsoleToggleKey => _consoleToggleKey;

        public IReadOnlyList<DebugQuickButton> QuickButtons => _quickButtons;

        public IReadOnlyList<string> CommandAssemblyNamePrefixes => _commandAssemblyNamePrefixes;

        public IReadOnlyList<string> CategoryOrder => _categoryOrder;

        public string DocFolder => _docFolder;

        public string DocFileName => _docFileName;

        public bool DocSplitByCategory => _docSplitByCategory;

        /// <summary>按回落规则解析出的生效配置（本资产为 null 时也能拿到一套可用的默认值）。</summary>
        public DebugSettings Settings => DebugSettings.From(this);
    }
}
