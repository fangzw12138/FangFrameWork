using UnityEngine;

namespace Fang.Framework.MinimalSample
{
    public sealed class HeroData : Data<HeroConfigDataSo>
    {
        private int _health;

        public HeroData(HeroConfigDataSo config) : base(config)
        {
            _health = config.MaxHealth;
        }

        public int Health => _health;

        public void ApplyDamage(int amount)
        {
            _health = Mathf.Max(0, _health - amount);
        }
    }
}
