using System;
using System.Collections.Generic;

namespace Fang.Framework.Tests
{
    internal interface IProbeService
    {
    }

    internal sealed class ProbeService : IProbeService
    {
    }

    internal sealed class ProbeServiceOverride : IProbeService
    {
    }

    internal sealed class ProbeFactoryResult : IProbeService
    {
    }

    internal sealed class UnregisteredService
    {
    }

    internal sealed class DependentService
    {
        public DependentService(IProbeService probe)
        {
            Probe = probe;
        }

        public IProbeService Probe { get; }
    }

    internal sealed class CircularServiceA
    {
        public CircularServiceA(CircularServiceB other)
        {
            Other = other;
        }

        public CircularServiceB Other { get; }
    }

    internal sealed class CircularServiceB
    {
        public CircularServiceB(CircularServiceA other)
        {
            Other = other;
        }

        public CircularServiceA Other { get; }
    }

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

    internal sealed class LifecycleProbeA : IInjectable, IInitializable, IDisposable
    {
        public LifecycleProbeA()
        {
            LifecycleLog.Add("create:A");
        }

        public void Inject(Scope scope)
        {
            LifecycleLog.Add("inject:A");
        }

        public void Initialize()
        {
            LifecycleLog.Add("initialize:A");
        }

        public void Dispose()
        {
            LifecycleLog.Add("dispose:A");
        }
    }

    internal sealed class LifecycleProbeB : IInjectable, IInitializable, IDisposable
    {
        public LifecycleProbeB()
        {
            LifecycleLog.Add("create:B");
        }

        public void Inject(Scope scope)
        {
            LifecycleLog.Add("inject:B");
        }

        public void Initialize()
        {
            LifecycleLog.Add("initialize:B");
        }

        public void Dispose()
        {
            LifecycleLog.Add("dispose:B");
        }
    }

    internal sealed class LifecycleProbeC : IInjectable, IInitializable, IDisposable
    {
        public LifecycleProbeC()
        {
            LifecycleLog.Add("create:C");
        }

        public void Inject(Scope scope)
        {
            LifecycleLog.Add("inject:C");
        }

        public void Initialize()
        {
            LifecycleLog.Add("initialize:C");
        }

        public void Dispose()
        {
            LifecycleLog.Add("dispose:C");
        }
    }
}
