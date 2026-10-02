using Cysharp.Threading.Tasks;

namespace Fang.Framework.Async.LoadingTransition
{
    internal sealed class TitleScope : AsyncScope
    {
        protected override async UniTask OnInitAsync()
        {
            AddService<TitleUiLoadingStep>();
            AddService<TitleDataLoadingStep>();
            AddService<InitDriver>();

            await this.AwaitServicesAsync();
        }
    }
}
