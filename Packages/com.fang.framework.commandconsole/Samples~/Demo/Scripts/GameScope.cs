#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Fang.Framework.CommandConsole.Demo
{
    /// <summary>
    /// Game 层：真实项目里放战斗 / 关卡这类服务，示例只往下建 Scene 层。
    /// </summary>
    public sealed class GameScope : Scope
    {
        public override void OnInit()
        {
            base.OnInit();

            var scene = CreateChildScope<SceneScope>();
            scene.OnInit();
        }
    }
}
#endif
