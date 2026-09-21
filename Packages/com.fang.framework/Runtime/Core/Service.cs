namespace Fang.Framework
{
    public abstract class Service : IInjectable, ILifecycle
    {
        protected Scope Scope { get; private set; }

        public void Inject(Scope scope)
        {
            Scope = scope;
        }

        public virtual void OnInit()
        {
        }

        public virtual void OnDispose()
        {
        }
    }
}
