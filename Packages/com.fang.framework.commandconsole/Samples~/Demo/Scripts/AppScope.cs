#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace Fang.Framework.CommandConsole.Demo
{
    /// <summary>
    /// App 层（系统层）：装会话服务，以及<b>调试指令服务</b>。
    ///
    /// 指令服务刻意装在这一层（浅层）：<c>DebugCommandContext.GetService&lt;T&gt;()</c> 先沿
    /// <c>Scope.Parent</c> <b>向上</b>找，找不到再从当前 Scope 起<b>向下兜底</b> —— 所以系统层的指令
    /// 也能摸到更深的 Game / Scene 层的服务。真实项目同理：指令服务装在系统层（或任何一层），
    /// 只要你要的服务在它的子树里、或在它的祖先链上。
    /// </summary>
    public sealed class AppScope : Scope
    {
        [SerializeField] private DebugProjectSo _project;

        public override void OnInit()
        {
            base.OnInit();

            AddService<DemoSessionService>();

            var commands = AddService<DebugCommandService>();
            commands.Configure(_project);

            var game = CreateChildScope<GameScope>();
            game.OnInit();
        }
    }
}
#endif
