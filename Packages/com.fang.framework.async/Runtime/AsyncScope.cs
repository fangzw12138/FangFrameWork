using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Fang.Framework.Async
{
    public abstract class AsyncScope : AsyncLifecycleBehaviour
    {
        private const string ServicesNodeName = "Services";

        private readonly List<IAsyncLifecycle> _services = new List<IAsyncLifecycle>();
        private readonly List<AsyncScope> _children = new List<AsyncScope>();
        private readonly List<AsyncScope> _disposalBuffer = new List<AsyncScope>();
        private AsyncScope _parent;
        private Transform _servicesRoot;

        public string Name => GetType().Name;

        public AsyncScope Parent => _parent;

        public IReadOnlyList<IAsyncLifecycle> Services => _services;

        public IReadOnlyList<AsyncScope> Children => _children;

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

        public T GetService<T>() where T : IAsyncLifecycle
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

        protected T CreateChildScope<T>() where T : AsyncScope
        {
            var host = new GameObject(typeof(T).Name);
            host.transform.SetParent(transform, false);

            var scope = host.AddComponent<T>();
            scope._parent = this;
            _children.Add(scope);
            TrackMember(scope);
            RefreshInitProgress();
            return scope;
        }

        protected async UniTask RemoveChild<T>() where T : AsyncScope
        {
            for (var i = 0; i < _children.Count; i++)
            {
                if (!(_children[i] is T child))
                {
                    continue;
                }

                _children.RemoveAt(i);
                UntrackMember(child);
                RefreshInitProgress();

                if (child.HasStarted)
                {
                    await child.DisposeAsync();
                }
                else
                {
                    child.DestroySelf();
                }

                return;
            }
        }

        protected T AddService<T>() where T : MonoBehaviour, IAsyncLifecycle
        {
            if (typeof(AsyncScope).IsAssignableFrom(typeof(T)))
            {
                throw new ArgumentException(
                    $"'{typeof(T).FullName}' is an AsyncScope; a scope cannot be registered as a service.", nameof(T));
            }

            var host = new GameObject(typeof(T).Name);
            host.transform.SetParent(ServicesRoot, false);

            var service = host.AddComponent<T>();

            if (service is IAsyncInjectable injectable)
            {
                injectable.Inject(this);
            }

            _services.Add(service);
            TrackMember(service);
            RefreshInitProgress();
            service.InitAsync().Forget();
            return service;
        }

        protected UniTask RemoveService<T>() where T : MonoBehaviour, IAsyncLifecycle
        {
            for (var i = 0; i < _services.Count; i++)
            {
                if (_services[i] is T service)
                {
                    _services.RemoveAt(i);
                    UntrackMember(service);
                    RefreshInitProgress();
                    return service.DisposeAsync();
                }
            }

            return UniTask.CompletedTask;
        }

        // A scope's progress is the equal-weight average of what it holds — its services plus its
        // child scopes — so nobody outside has to fold the tree on its behalf. Members are tracked
        // the moment they are registered; disposing one clears its own events, which unsubscribes
        // it for free (AsyncLifecycleBehaviour.RunDisposalAsync).
        private void TrackMember(IAsyncLifecycle member)
        {
            if (member is AsyncLifecycleBehaviour behaviour)
            {
                behaviour.InitProgressChanged += OnMemberProgressChanged;
            }
        }

        private void UntrackMember(IAsyncLifecycle member)
        {
            if (member is AsyncLifecycleBehaviour behaviour)
            {
                behaviour.InitProgressChanged -= OnMemberProgressChanged;
            }
        }

        private void OnMemberProgressChanged(AsyncLifecycleBehaviour member, float value)
        {
            RefreshInitProgress();
        }

        private void RefreshInitProgress()
        {
            // An initialized scope sits at 1 and stays there: a member finishing late must not pull
            // a finished scope's progress back down.
            if (IsInitialized)
            {
                return;
            }

            var total = 0f;
            var sum = 0f;

            for (var i = 0; i < _services.Count; i++)
            {
                total += 1f;
                sum += _services[i].InitProgress;
            }

            for (var i = 0; i < _children.Count; i++)
            {
                total += 1f;
                sum += _children[i].InitProgress;
            }

            // Nothing to average (a scope holding nothing, or one whose members were all detached):
            // stay at 0 until this scope's own init completes.
            ReportInitProgress(total <= 0f ? 0f : sum / total);
        }

        private protected override async UniTask OnDisposeCoreAsync()
        {
            _disposalBuffer.Clear();
            _disposalBuffer.AddRange(_children);
            _children.Clear();

            Exception failure = null;

            for (var i = _disposalBuffer.Count - 1; i >= 0; i--)
            {
                try
                {
                    UntrackMember(_disposalBuffer[i]);
                    await _disposalBuffer[i].DisposeAsync();
                }
                catch (Exception exception)
                {
                    failure ??= exception;
                }
            }

            for (var i = _services.Count - 1; i >= 0; i--)
            {
                try
                {
                    UntrackMember(_services[i]);
                    await _services[i].DisposeAsync();
                }
                catch (Exception exception)
                {
                    failure ??= exception;
                }
            }

            _services.Clear();
            _parent = null;
            DestroySelf();

            if (failure != null)
            {
                throw failure;
            }
        }

        private Transform ServicesRoot
        {
            get
            {
                if (_servicesRoot == null)
                {
                    var host = new GameObject(ServicesNodeName);
                    host.transform.SetParent(transform, false);
                    _servicesRoot = host.transform;
                }

                return _servicesRoot;
            }
        }

        private void DestroySelf()
        {
            // Unity may already have torn this object down (the host scope was destroyed first,
            // or the editor stopped play mode) while the async disposal chain was still awaiting.
            // Touching gameObject then throws MissingReferenceException, which the fire-and-forget
            // disposal surfaces as an unobserved task exception.
            if (this == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }
    }
}
