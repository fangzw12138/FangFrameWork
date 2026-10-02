using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Fang.Framework.Async
{
    public abstract class AsyncController<TConfig, TData> : AsyncLifecycleBehaviour, IAsyncInjectable
        where TConfig : ConfigDataSo
        where TData : Data<TConfig>
    {
        protected AsyncScope Scope { get; private set; }

        public TData Data { get; private set; }

        public void Inject(AsyncScope scope)
        {
            Scope = scope;
        }

        public UniTask InitAsync(TData data)
        {
            if (Data != null)
            {
                throw new InvalidOperationException("Data is already bound; an AsyncController binds its data only once.");
            }

            Data = data ?? throw new ArgumentNullException(nameof(data));

            return InitAsync();
        }

        public override UniTask InitAsync()
        {
            if (Data == null)
            {
                throw new InvalidOperationException("AsyncController needs InitAsync(data) before it can be initialized.");
            }

            return base.InitAsync();
        }
    }
}
