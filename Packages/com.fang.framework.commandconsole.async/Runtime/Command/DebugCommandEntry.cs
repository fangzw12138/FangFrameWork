#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Reflection;

namespace Fang.Framework.CommandConsole.Async
{
    /// <summary>
    /// 一条已注册指令的元数据。构造前请先由 <see cref="DebugCommandScanner"/> 校验签名。
    /// </summary>
    public sealed class DebugCommandEntry
    {
        public DebugCommandEntry(string name, string description, string category, MethodInfo method)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("指令名不能为空。", nameof(name));
            }

            Name = name;
            Description = description ?? string.Empty;
            Category = string.IsNullOrWhiteSpace(category) ? DebugCommandAttribute.DefaultCategory : category;
            Method = method ?? throw new ArgumentNullException(nameof(method));
            Source = method.DeclaringType == null ? method.Name : method.DeclaringType.Name + "." + method.Name;
        }

        /// <summary>指令名（调用标识符）。</summary>
        public string Name { get; }

        /// <summary>说明文本。</summary>
        public string Description { get; }

        /// <summary>分类。</summary>
        public string Category { get; }

        /// <summary>被登记的方法（签名已核定）。</summary>
        public MethodInfo Method { get; }

        /// <summary>来源标识：<c>类型名.方法名</c>，用于文档与排错。</summary>
        public string Source { get; }

        internal object Invoke(DebugCommandContext context, DebugCommandArgs args)
        {
            return Method.Invoke(null, new object[] { context, args });
        }

        public override string ToString()
        {
            return $"{Name} [{Category}] ({Source})";
        }
    }
}
#endif
