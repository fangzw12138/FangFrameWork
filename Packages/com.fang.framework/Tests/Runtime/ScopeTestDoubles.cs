using System.Collections.Generic;

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
        public TService Add<TService>() where TService : Service, new()
        {
            return AddService<TService>();
        }

        public void Remove<TService>() where TService : Service
        {
            RemoveService<TService>();
        }

        public TChild AddChild<TChild>() where TChild : Scope, new()
        {
            return CreateChildScope<TChild>();
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
        public LifecycleProbeA()
        {
            LifecycleLog.Add("create:A");
        }

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
        public LifecycleProbeB()
        {
            LifecycleLog.Add("create:B");
        }

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
        public LifecycleProbeC()
        {
            LifecycleLog.Add("create:C");
        }

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
