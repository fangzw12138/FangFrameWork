using UnityEngine;

namespace Fang.Framework.MinimalSample
{
    public sealed class HeroService : Service
    {
        private const string ConfigResourcePath = "MinimalSampleHeroConfig";

        private HeroController _controller;

        public override void OnInit()
        {
            var config = Resources.Load<HeroConfigDataSo>(ConfigResourcePath);

            var hero = new GameObject("Hero");
            hero.transform.SetParent(transform, false);

            _controller = hero.AddComponent<HeroController>();
            _controller.Initialize(new HeroData(config));
            _controller.Inject(Scope);
            _controller.OnInit();

            var worldObject = hero.AddComponent<HeroWorldObject>();
            worldObject.Initialize(_controller);
            worldObject.Inject(Scope);
        }

        public override void OnDispose()
        {
            _controller.OnDispose();
        }
    }
}
