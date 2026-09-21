using System.Collections.Generic;
using UnityEngine;

namespace Fang.Framework.Tests
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

    internal class ProbeScope : Scope
    {
        private static readonly List<GameObject> Hosts = new List<GameObject>();

        public static TScope Create<TScope>() where TScope : Scope
        {
            var scope = CreateUninitialized<TScope>();
            scope.OnInit();
            return scope;
        }

        public static TScope CreateUninitialized<TScope>() where TScope : Scope
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

        public TService Add<TService>() where TService : Service
        {
            return AddService<TService>();
        }

        public void Remove<TService>() where TService : Service
        {
            RemoveService<TService>();
        }

        public TChild AddChild<TChild>() where TChild : Scope
        {
            var child = CreateChildScope<TChild>();
            child.OnInit();
            return child;
        }
    }

    internal sealed class ProbeChildScope : ProbeScope
    {
    }

    internal sealed class ProbeGrandChildScope : ProbeScope
    {
    }

    internal sealed class AudioService : Service
    {
        public Scope InjectedScope => Scope;
    }

    internal sealed class AudioServiceOverride : Service
    {
    }

    internal sealed class LifecycleProbeA : Service
    {
        public override void OnInit()
        {
            LifecycleLog.Add("init:A");
        }

        public override void OnDispose()
        {
            LifecycleLog.Add("dispose:A");
        }
    }

    internal sealed class LifecycleProbeB : Service
    {
        public override void OnInit()
        {
            LifecycleLog.Add("init:B");
        }

        public override void OnDispose()
        {
            LifecycleLog.Add("dispose:B");
        }
    }

    internal sealed class LifecycleProbeC : Service
    {
        public override void OnInit()
        {
            LifecycleLog.Add("init:C");
        }

        public override void OnDispose()
        {
            LifecycleLog.Add("dispose:C");
        }
    }

    internal sealed class TickProbeService : Service, ITickable, IFixedTickable
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

    internal sealed class SecondaryTickProbeService : Service, ITickable, IFixedTickable
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

    internal sealed class FixedOnlyProbeService : Service, IFixedTickable
    {
        public void OnFixedTick(float deltaTime)
        {
            LifecycleLog.Add("fixedTick:only");
        }
    }

    internal sealed class DeltaProbeService : Service, ITickable, IFixedTickable
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
