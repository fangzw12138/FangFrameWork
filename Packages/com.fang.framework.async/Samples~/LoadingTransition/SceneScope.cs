using Cysharp.Threading.Tasks;

namespace Fang.Framework.Async.LoadingTransition
{
    internal sealed class SceneScope : AsyncScope
    {
        protected override async UniTask OnInitAsync()
        {
            AddService<TerrainLoadingStep>();
            AddService<ActorsLoadingStep>();
            AddService<NavMeshLoadingStep>();
            AddService<InitDriver>();

            await this.AwaitServicesAsync();
        }
    }
}
