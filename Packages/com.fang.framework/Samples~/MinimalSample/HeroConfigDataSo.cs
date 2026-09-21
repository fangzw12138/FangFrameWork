using UnityEngine;

namespace Fang.Framework.MinimalSample
{
    public sealed class HeroConfigDataSo : ConfigDataSo
    {
        [SerializeField] private int _maxHealth;

        public int MaxHealth => _maxHealth;
    }
}
