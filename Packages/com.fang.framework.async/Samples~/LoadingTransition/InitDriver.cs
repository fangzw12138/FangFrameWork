using Cysharp.Threading.Tasks;

namespace Fang.Framework.Async.LoadingTransition
{
    internal sealed class InitDriver : AsyncService, ITickable
    {
        public void OnTick(float deltaTime)
        {
            var scope = Scope;

            if (scope == null || scope.IsInitialized)
            {
                return;
            }

            scope.ReportInitProgress(Fold(scope));
        }

        protected override UniTask OnInitAsync()
        {
            return UniTask.CompletedTask;
        }

        private float Fold(AsyncScope scope)
        {
            var services = scope.Services;
            var children = scope.Children;
            var total = 0f;
            var sum = 0f;

            for (var i = 0; i < services.Count; i++)
            {
                if (ReferenceEquals(services[i], this))
                {
                    continue;
                }

                total += 1f;
                sum += services[i].InitProgress;
            }

            for (var i = 0; i < children.Count; i++)
            {
                total += 1f;
                sum += children[i].InitProgress;
            }

            return total <= 0f ? 0f : sum / total;
        }
    }
}
