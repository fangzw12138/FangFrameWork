#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Fang.Framework.CommandConsole.Demo
{
    /// <summary>
    /// Scene 层：玩家与场景物体两个服务。
    ///
    /// <b>调试指令服务不在这里</b>：它装在 App 层（系统层），靠 <c>DebugCommandContext</c> 的
    /// <b>向下兜底</b>摸到本层的服务 —— 这正是「指令服务装浅一点、它要用的服务在深处」的形态。
    /// </summary>
    public sealed class SceneScope : Scope
    {
        public override void OnInit()
        {
            base.OnInit();

            AddService<DemoPlayerService>();
            AddService<DemoSpawnService>();
        }
    }
}
#endif
