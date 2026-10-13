#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Fang.Framework.CommandConsole.Async
{
    /// <summary>全部内置默认值（纯常量）。</summary>
    public static class DebugCommandDefaults
    {
        /// <summary>包内置指令（ping / list_commands / command_log）的分类。</summary>
        public const string BuiltInCategory = "builtin";
    }
}
#endif
