using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Fang.Framework.Async.Tests
{
    internal static class LifecycleLog
    {
        public static readonly List<string> Entries = new List<string>();

        public static void Reset()
        {
            Entries.Clear();
        }

        public static void Add(string entry)
        {
            Entries.Add(entry);
        }
    }

    internal class ProbeAsyncScope : AsyncScope
    {
        private static readonly List<GameObject> Hosts = new List<GameObject>();

        public static TScope Create<TScope>() where TScope : AsyncScope
        {
            var scope = CreateUninitialized<TScope>();
            scope.InitAsync().GetAwaiter().GetResult();
            return scope;
        }

        public static TScope CreateUninitialized<TScope>() where TScope : AsyncScope
        {
            var host = new GameObject(typeof(TScope).Name);
            Hosts.Add(host);
            return host.AddComponent<TScope>();
        }

        public static void DestroyAll()
        {
            for (var i = Hosts.Count - 1; i >= 0; i--)
            {
                if (Hosts[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(Hosts[i]);
                }
            }

            Hosts.Clear();
        }

        public TService Add<TService>() where TService : MonoBehaviour, IAsyncLifecycle
        {
            return AddService<TService>();
        }

        public UniTask Remove<TService>() where TService : MonoBehaviour, IAsyncLifecycle
        {
            return RemoveService<TService>();
        }

        public TChild AddChild<TChild>() where TChild : AsyncScope
        {
            var child = CreateChildScope<TChild>();
            child.InitAsync().GetAwaiter().GetResult();
            return child;
        }

        public UniTask RemoveChildScope<TChild>() where TChild : AsyncScope
        {
            return RemoveChild<TChild>();
        }
    }

    internal sealed class ProbeChildAsyncScope : ProbeAsyncScope
    {
    }

    internal sealed class ProbeGrandChildAsyncScope : ProbeAsyncScope
    {
    }

    internal sealed class ThrowingDisposeAsyncScope : ProbeAsyncScope
    {
        protected override UniTask OnDisposeAsync()
        {
            throw new InvalidOperationException("child dispose failed");
        }
    }

    internal sealed class AudioService : AsyncService
    {
        public AsyncScope InjectedScope => Scope;
    }

    internal sealed class AudioServiceOverride : AsyncService
    {
    }

    internal sealed class AsyncLifecycleProbeA : AsyncService
    {
        protected override UniTask OnInitAsync()
        {
            LifecycleLog.Add("init:A");
            return UniTask.CompletedTask;
        }

        protected override UniTask OnDisposeAsync()
        {
            LifecycleLog.Add("dispose:A");
            return UniTask.CompletedTask;
        }
    }

    internal sealed class AsyncLifecycleProbeB : AsyncService
    {
        protected override UniTask OnInitAsync()
        {
            LifecycleLog.Add("init:B");
            return UniTask.CompletedTask;
        }

        protected override UniTask OnDisposeAsync()
        {
            LifecycleLog.Add("dispose:B");
            return UniTask.CompletedTask;
        }
    }

    internal sealed class TickProbeService : AsyncService, ITickable, IFixedTickable
    {
        public void OnTick(float deltaTime)
        {
            LifecycleLog.Add("tick:first");
        }

        public void OnFixedTick(float deltaTime)
        {
            LifecycleLog.Add("fixedTick:first");
        }
    }

    internal sealed class SecondaryTickProbeService : AsyncService, ITickable, IFixedTickable
    {
        public void OnTick(float deltaTime)
        {
            LifecycleLog.Add("tick:second");
        }

        public void OnFixedTick(float deltaTime)
        {
            LifecycleLog.Add("fixedTick:second");
        }
    }

    internal sealed class FixedOnlyProbeService : AsyncService, IFixedTickable
    {
        public void OnFixedTick(float deltaTime)
        {
            LifecycleLog.Add("fixedTick:only");
        }
    }

    internal sealed class DeltaProbeService : AsyncService, ITickable, IFixedTickable
    {
        public float LastTick { get; private set; }

        public float LastFixedTick { get; private set; }

        public void OnTick(float deltaTime)
        {
            LastTick = deltaTime;
        }

        public void OnFixedTick(float deltaTime)
        {
            LastFixedTick = deltaTime;
        }
    }
}
