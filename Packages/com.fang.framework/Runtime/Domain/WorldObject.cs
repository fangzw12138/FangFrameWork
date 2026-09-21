using System;
using UnityEngine;

namespace Fang.Framework
{
    public abstract class WorldObject<TController, TData, TConfig> : MonoBehaviour, IInjectable
        where TConfig : ConfigDataSo
        where TData : Data<TConfig>
        where TController : Controller<TConfig, TData>
    {
        public TController Controller { get; private set; }

        public TData Data => Controller != null ? Controller.Data : null;

        public TConfig Config => Data != null ? Data.Config : null;

        public void Initialize(TController controller)
        {
            Controller = controller ?? throw new ArgumentNullException(nameof(controller));
        }

        public virtual void Inject(Scope scope)
        {
        }
    }
}
