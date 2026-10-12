#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;

namespace Fang.Framework.CommandConsole.Async
{
    /// <summary>
    /// 把一个静态方法登记为调试指令。
    /// 方法签名必须精确是 <c>static object M(DebugCommandContext, DebugCommandArgs)</c>，否则扫描时会被跳过并给出警告。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class DebugCommandAttribute : Attribute
    {
        /// <summary>未显式指定分类时使用的分类名。</summary>
        public const string DefaultCategory = "system";

        public DebugCommandAttribute(string name)
            : this(name, null, DefaultCategory)
        {
        }

        public DebugCommandAttribute(string name, string description)
            : this(name, description, DefaultCategory)
        {
        }

        public DebugCommandAttribute(string name, string description, string category)
        {
            Name = name;
            Description = description;
            Category = string.IsNullOrWhiteSpace(category) ? DefaultCategory : category;
        }

        /// <summary>指令名（调用时使用的标识符）。</summary>
        public string Name { get; }

        /// <summary>人类可读的说明，用于 list_commands 与控制台窗口。</summary>
        public string Description { get; }

        /// <summary>分类，用于分组与过滤。</summary>
        public string Category { get; }
    }
}
#endif
