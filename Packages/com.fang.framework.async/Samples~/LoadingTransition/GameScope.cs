using Cysharp.Threading.Tasks;

namespace Fang.Framework.Async.LoadingTransition
{
    internal sealed class GameScope : AsyncScope
    {
        protected override async UniTask OnInitAsync()
        {
            AddService<GameDataLoadingStep>();
            AddService<GameInputLoadingStep>();
            AddService<InitDriver>();

            var scene = CreateChildScope<SceneScope>();

            await UniTask.WhenAll(this.AwaitServicesAsync(), scene.InitAsync());
        }
    }
}
