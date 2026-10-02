using UnityEngine;

namespace Fang.Framework.Async
{
    public abstract class AsyncService : AsyncLifecycleBehaviour, IAsyncInjectable
    {
        protected AsyncScope Scope { get; private set; }

        public void Inject(AsyncScope scope)
        {
            Scope = scope;
        }
    }
}
