#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Reflection;

namespace Fang.Framework.CommandConsole
{
    /// <summary>
    /// 一条已登记的指令：名字 / 描述 / 分类 + 实际要调用的方法。
    /// 分类与描述在这里统一兜底（分类空 → <see cref="DebugCommandDefaults.DefaultCategory"/>），
    /// 所以扫描来的和手工注册的条目行为一致。
    /// </summary>
    public sealed class DebugCommandEntry
    {
        public DebugCommandEntry(string name, string description, string category, MethodInfo method)
        {
            Name = name;
            Description = description ?? string.Empty;
            Category = string.IsNullOrWhiteSpace(category)
                ? DebugCommandDefaults.DefaultCategory
                : category.Trim();
            Method = method;
        }

        public string Name { get; }

        public string Description { get; }

        /// <summary>文档分域（自由字符串）。</summary>
        public string Category { get; }

        public MethodInfo Method { get; }

        public Type DeclaringType => Method?.DeclaringType;

        /// <summary>实现位置（全名 + 方法名），警告与文档里都用它定位。</summary>
        public string Source => DeclaringType == null
            ? (Method == null ? "(未知)" : Method.Name)
            : DeclaringType.FullName + "." + Method.Name;
    }
}
#endif
