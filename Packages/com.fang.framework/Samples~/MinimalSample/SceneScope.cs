using UnityEngine;

namespace Fang.Framework.MinimalSample
{
    public sealed class SceneScope : Scope
    {
        // 非标准写法：示例直接用 Resources.Load 拿配置，只为把最小示例压到最短。
        // 正式项目的配置解析归扩展包的解析层，不在场景层做。
        private const string HeroConfigResourcePath = "MinimalSampleHeroConfig";

        public override void OnInit()
        {
            base.OnInit();

            var heroes = AddService<HeroManagerService>();
            heroes.CreateHero(Resources.Load<HeroConfigDataSo>(HeroConfigResourcePath));
        }
    }
}
