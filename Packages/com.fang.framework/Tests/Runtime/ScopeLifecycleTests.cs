using NUnit.Framework;

namespace Fang.Framework.Tests
{
    public class ScopeLifecycleTests
    {
        [SetUp]
        public void SetUp()
        {
            LifecycleLog.Reset();
        }

        [Test]
        public void AddService_creates_then_initializes_in_add_order()
        {
            var scope = new ProbeScope();

            scope.Add<LifecycleProbeA>();
            scope.Add<LifecycleProbeB>();
            scope.Add<LifecycleProbeC>();

            CollectionAssert.AreEqual(
                new[] { "create:A", "init:A", "create:B", "init:B", "create:C", "init:C" },
                LifecycleLog.Entries);
        }

        [Test]
        public void Dispose_disposes_services_in_reverse_add_order_exactly_once()
        {
            var scope = new ProbeScope();
            scope.Add<LifecycleProbeA>();
            scope.Add<LifecycleProbeB>();

            scope.Dispose();
            scope.Dispose();

            CollectionAssert.AreEqual(
                new[] { "create:A", "init:A", "create:B", "init:B", "dispose:B", "dispose:A" },
                LifecycleLog.Entries);
        }

        [Test]
        public void Dispose_disposes_children_before_own_services()
        {
            var root = new ProbeScope();
            root.Add<LifecycleProbeA>();
            var child = root.AddChild<ProbeChildScope>();
            child.Add<LifecycleProbeB>();

            root.Dispose();

            CollectionAssert.AreEqual(
                new[] { "create:A", "init:A", "create:B", "init:B", "dispose:B", "dispose:A" },
                LifecycleLog.Entries);
        }

        [Test]
        public void Tick_drives_own_services_in_add_order_then_children()
        {
            var root = new ProbeScope();
            root.Add<TickProbeService>();
            root.Add<FixedOnlyProbeService>();
            root.Add<SecondaryTickProbeService>();
            var child = root.AddChild<ProbeChildScope>();
            child.Add<TickProbeService>();

            root.Tick(1f);

            CollectionAssert.AreEqual(new[] { "tick:first", "tick:second", "tick:first" }, LifecycleLog.Entries);
        }

        [Test]
        public void FixedTick_drives_own_services_in_add_order_then_children()
        {
            var root = new ProbeScope();
            root.Add<FixedOnlyProbeService>();
            root.Add<TickProbeService>();
            root.Add<SecondaryTickProbeService>();
            var child = root.AddChild<ProbeChildScope>();
            child.Add<FixedOnlyProbeService>();

            root.FixedTick(1f);

            CollectionAssert.AreEqual(
                new[] { "fixedTick:only", "fixedTick:first", "fixedTick:second", "fixedTick:only" },
                LifecycleLog.Entries);
        }

        [Test]
        public void Tick_forwards_the_delta_time()
        {
            var scope = new ProbeScope();
            var service = scope.Add<DeltaProbeService>();

            scope.Tick(0.25f);
            scope.FixedTick(0.5f);

            Assert.AreEqual(0.25f, service.LastTick);
            Assert.AreEqual(0.5f, service.LastFixedTick);
        }

        [Test]
        public void Tick_skips_services_without_the_capability()
        {
            var scope = new ProbeScope();
            scope.Add<LifecycleProbeA>();

            scope.Tick(1f);
            scope.FixedTick(1f);

            CollectionAssert.AreEqual(new[] { "create:A", "init:A" }, LifecycleLog.Entries);
        }

        [Test]
        public void Tick_does_not_touch_a_disposed_child()
        {
            var root = new ProbeScope();
            var child = root.AddChild<ProbeChildScope>();
            child.Add<TickProbeService>();
            child.Dispose();

            root.Tick(1f);

            Assert.AreEqual(0, LifecycleLog.Entries.Count);
        }
    }
}
