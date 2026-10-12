#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;

namespace Fang.Framework.CommandConsole.Async
{
    /// <summary>
    /// 指令的「业务错误」。抛出它表示调用方传错了参数或当前状态不满足，
    /// 注册表会把它转成 <c>{ok:false,error}</c>，并且**不**打 Error 日志。
    /// 其它异常会被视为缺陷，除了返回 error 外还会 <c>Debug.LogError</c>。
    /// </summary>
    public sealed class DebugCommandException : Exception
    {
        public DebugCommandException(string message)
            : base(message)
        {
        }

        public DebugCommandException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
#endif
