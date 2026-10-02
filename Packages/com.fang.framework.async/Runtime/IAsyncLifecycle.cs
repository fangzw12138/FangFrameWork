using Cysharp.Threading.Tasks;

namespace Fang.Framework.Async
{
    public interface IAsyncLifecycle
    {
        bool IsInitialized { get; }

        bool IsInitializing { get; }

        float InitProgress { get; }

        UniTask InitAsync();

        UniTask DisposeAsync();
    }

    public interface IAsyncInjectable
    {
        void Inject(AsyncScope scope);
    }
}
