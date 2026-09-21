using System.Collections.Generic;
using UnityEngine;

namespace Fang.Framework.MinimalSample
{
    public sealed class HeroManagerService : Service
    {
        private readonly List<HeroController> _heroes = new List<HeroController>();

        public IReadOnlyList<HeroController> Heroes => _heroes;

        public override void OnDispose()
        {
            for (var i = _heroes.Count - 1; i >= 0; i--)
            {
                RemoveHero(_heroes[i]);
            }
        }

        // CreateHero 本质是工厂职责；这里不拆出独立的 HeroFactoryService，保持简写。
        public HeroController CreateHero(HeroConfigDataSo config)
        {
            var root = new GameObject("Hero");
            root.transform.SetParent(transform, false);

            var controller = root.AddComponent<HeroController>();
            controller.Initialize(new HeroData(config));
            controller.Inject(Scope);
            controller.OnInit();

            _heroes.Add(controller);
            return controller;
        }

        public void RemoveHero(HeroController hero)
        {
            if (!_heroes.Remove(hero))
            {
                return;
            }

            hero.OnDispose();
        }
    }
}
