using System;
using System.Collections.Generic;

namespace Fang.Framework
{
    public interface IInjectable
    {
        void Inject(Scope scope);
    }

    public abstract class Scope
    {
        private readonly List<Service> _services = new List<Service>();
        private readonly List<Scope> _children = new List<Scope>();
        private Scope _parent;
        private bool _disposed;

        public string Name => GetType().Name;

        public Scope Parent => _parent;

        public IReadOnlyList<Service> Services => _services;

        public IReadOnlyList<Scope> Children => _children;

        public bool IsDisposed => _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            for (var i = _children.Count - 1; i >= 0; i--)
            {
                _children[i].Dispose();
            }

            for (var i = _services.Count - 1; i >= 0; i--)
            {
                _services[i].OnDispose();
            }

            _children.Clear();
            _services.Clear();
            _parent = null;
        }

        public void Tick(float deltaTime)
        {
            for (var i = 0; i < _services.Count; i++)
            {
                if (_services[i] is ITickable tickable)
                {
                    tickable.OnTick(deltaTime);
                }
            }

            for (var i = 0; i < _children.Count; i++)
            {
                _children[i].Tick(deltaTime);
            }
        }

        public void FixedTick(float deltaTime)
        {
            for (var i = 0; i < _services.Count; i++)
            {
                if (_services[i] is IFixedTickable tickable)
                {
                    tickable.OnFixedTick(deltaTime);
                }
            }

            for (var i = 0; i < _children.Count; i++)
            {
                _children[i].FixedTick(deltaTime);
            }
        }

        protected T CreateChildScope<T>() where T : Scope, new()
        {
            var scope = new T();
            scope._parent = this;
            _children.Add(scope);
            return scope;
        }

        protected T AddService<T>() where T : Service, new()
        {
            var service = new T();
            service.Inject(this);
            _services.Add(service);
            service.OnInit();
            return service;
        }

        public T GetService<T>() where T : Service
        {
            var scope = this;

            while (scope != null)
            {
                for (var i = 0; i < scope._services.Count; i++)
                {
                    if (scope._services[i] is T service)
                    {
                        return service;
                    }
                }

                scope = scope._parent;
            }

            throw new InvalidOperationException(
                $"Service '{typeof(T).FullName}' is not registered in scope '{Name}' or any of its parents.");
        }

        protected void RemoveService<T>() where T : Service
        {
            for (var i = 0; i < _services.Count; i++)
            {
                if (_services[i] is T service)
                {
                    service.OnDispose();
                    _services.RemoveAt(i);
                    return;
                }
            }
        }
    }
}
