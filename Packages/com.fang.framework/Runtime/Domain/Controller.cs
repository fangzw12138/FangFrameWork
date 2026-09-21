using System;
using UnityEngine;

namespace Fang.Framework
{
    public abstract class Controller<TConfig, TData> : MonoBehaviour, IInjectable
        where TConfig : ConfigDataSo
        where TData : Data<TConfig>
    {
        public TData Data { get; private set; }

        public void Initialize(TData data)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public virtual void Inject(Scope scope)
        {
        }
    }
}
