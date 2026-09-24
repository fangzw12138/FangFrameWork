#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;

namespace Fang.Framework.CommandConsole
{
    /// <summary>
    /// 指令业务错误：指令内抛出，注册表捕获后序列化成 <c>{ok:false, error:message}</c>。
    /// 用于参数非法 / 目标不存在 / 状态不允许这类「预期内失败」——错误文案直接暴露给调用方（AI / 测试脚本），
    /// 所以写清楚「错在哪 + 怎么办」。
    /// </summary>
    public sealed class DebugCommandException : Exception
    {
        public DebugCommandException(string message) : base(message)
        {
        }
    }
}
#endif
