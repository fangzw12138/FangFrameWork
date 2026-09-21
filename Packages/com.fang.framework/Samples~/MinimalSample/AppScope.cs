namespace Fang.Framework.MinimalSample
{
    public sealed class AppScope : Scope
    {
        public override void OnInit()
        {
            base.OnInit();

            AddService<AudioService>();

            var game = CreateChildScope<GameScope>();
            game.OnInit();
        }
    }
}
