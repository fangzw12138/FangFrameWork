using UnityEngine;

namespace Fang.Framework.CommandConsole
{
    /// <summary>
    /// 全部内置默认值集中在这里（纯常量，可单测）。
    /// 刻意不加 <c>#if UNITY_EDITOR || DEVELOPMENT_BUILD</c> 守卫：<see cref="DebugProjectSo"/> /
    /// <see cref="DebugQuickButton"/> 是数据资产、Release 包里也存在，它们的「留空即回落默认」属性
    /// 要在任何平台都能编译，所以这张表必须一直在。
    /// </summary>
    public static class DebugCommandDefaults
    {
        // ---------- 运行参数 ----------

        public const bool AutoStart = true;

        public const string ListenAddress = "127.0.0.1";

        public const int Port = 7777;

        public const bool ConsoleEnabled = true;

        public static readonly KeyCode ConsoleToggleKey = KeyCode.BackQuote;

        // ---------- 分类 ----------

        /// <summary>忘标分类时的归属。分类名是自由字符串，这里只是默认值，不是枚举。</summary>
        public const string DefaultCategory = "system";

        /// <summary>包内置指令（ping / list_commands / command_log）的分类。</summary>
        public const string BuiltInCategory = "builtin";

        /// <summary>内置默认分类顺序：只保证内置指令分类在最前，其余按名称排序追加。</summary>
        public static readonly string[] CategoryOrder = { BuiltInCategory };

        // ---------- 上限 ----------

        public const int CallLogMaxSize = 200;

        public const int SummaryMaxLength = 300;

        public const int ConsoleMaxOutputLines = 50;

        // ---------- 文档输出 ----------

        public const string DocFolder = "Docs";

        public const string DocFileName = "DebugCommands.md";

        public const bool DocSplitByCategory = true;
    }
}
