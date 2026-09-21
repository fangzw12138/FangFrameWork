namespace Fang.Framework.MinimalSample
{
    public sealed class HeroController : Controller<HeroConfigDataSo, HeroData>
    {
        private HeroVisual _visual;

        public void TakeDamage(int amount)
        {
            Data.ApplyDamage(amount);
        }

        public override void OnInit()
        {
            _visual = gameObject.AddComponent<HeroVisual>();
            _visual.Initialize(this);
        }

        public override void OnDispose()
        {
            _visual.OnDispose();
        }
    }
}
