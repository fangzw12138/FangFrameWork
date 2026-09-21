namespace Fang.Framework.MinimalSample
{
    public sealed class SceneScope : Scope
    {
        public override void OnInit()
        {
            base.OnInit();

            AddService<HeroService>();
        }
    }
}
