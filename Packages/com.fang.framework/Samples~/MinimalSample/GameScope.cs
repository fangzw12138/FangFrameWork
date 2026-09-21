namespace Fang.Framework.MinimalSample
{
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
