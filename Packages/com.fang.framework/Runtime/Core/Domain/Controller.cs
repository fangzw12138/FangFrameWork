using System;
using UnityEngine;

namespace Fang.Framework
{
    public abstract class Controller<TConfig, TData> : MonoBehaviour, IInjectable, ILifecycle
        where TConfig : ConfigDataSo
        where TData : Data<TConfig>
    {
        protected Scope Scope { get; private set; }

        public TData Data { get; private set; }

        public void Inject(Scope scope)
        {
            Scope = scope;
        }

        public void Initialize(TData data)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public virtual void OnInit()
        {
        }

        public virtual void OnDispose()
        {
        }
    }
}
