#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;

namespace Fang.Framework.CommandConsole
{
    /// <summary>
    /// 指令标记：贴在 <c>static</c> 方法上即注册为调试指令，启动时扫描收集。
    /// 签名约定：<c>public static object Method(DebugCommandContext ctx, DebugCommandArgs args)</c>。
    /// 描述里可用 <c>&lt;参数名&gt;</c> 标记参数（文档生成器原样展示，AI 靠它拼调用）。
    /// 分类是<b>自由字符串</b>（没有枚举、没有常量表），只用来分组与排序；忘标归入 <c>system</c>。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class DebugCommandAttribute : Attribute
    {
        public DebugCommandAttribute(string name, string description = "", string category = DebugCommandDefaults.DefaultCategory)
        {
            Name = name;
            Description = description;
            Category = category;
        }

        public string Name { get; }

        public string Description { get; }

        public string Category { get; }
    }
}
#endif
