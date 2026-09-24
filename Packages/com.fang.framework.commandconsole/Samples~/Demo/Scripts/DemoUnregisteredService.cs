#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Fang.Framework.CommandConsole.Demo
{
    /// <summary>
    /// 刻意**不注册**的空服务：给 <c>demo_unregistered_service</c> 用。
    ///
    /// 它存在的唯一目的，是演示 <c>DebugCommandContext.TryGetService&lt;T&gt;()</c> 在整条 Scope 链上
    /// 都找不到服务时**返回 false / null，而不是抛异常** —— 核心包的 <c>Scope.GetService&lt;T&gt;()</c> 会抛，
    /// 而「还没进游戏就该返回 error」是指令的常见场景。
    /// </summary>
    public sealed class DemoUnregisteredService : Service
    {
    }
}
#endif
