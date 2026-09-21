namespace Fang.Framework.MinimalSample
{
    public sealed class HeroController : Controller<HeroConfigDataSo, HeroData>
    {
        public void TakeDamage(int amount)
        {
            Data.ApplyDamage(amount);
        }
    }
}
