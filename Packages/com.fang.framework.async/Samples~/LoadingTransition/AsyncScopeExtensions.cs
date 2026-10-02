using Cysharp.Threading.Tasks;

namespace Fang.Framework.Async.LoadingTransition
{
    internal static class AsyncScopeExtensions
    {
        public static async UniTask AwaitServicesAsync(this AsyncScope scope)
        {
            var services = scope.Services;

            for (var i = 0; i < services.Count; i++)
            {
                await services[i].InitAsync();
            }
        }
    }
}
