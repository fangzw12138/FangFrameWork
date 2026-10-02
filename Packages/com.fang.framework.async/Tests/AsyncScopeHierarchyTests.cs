using System;
using NUnit.Framework;

namespace Fang.Framework.Async.Tests
{
    public class AsyncScopeHierarchyTests
    {
        [SetUp]
        public void SetUp()
        {
            LifecycleLog.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            ProbeAsyncScope.DestroyAll();
        }

        [Test]
        public void CreateChildScope_records_both_directions()
        {
            var parent = ProbeAsyncScope.Create<ProbeAsyncScope>();

            var child = parent.AddChild<ProbeChildAsyncScope>();

            Assert.AreSame(parent, child.Parent);
            Assert.AreSame(child, parent.Children[0]);
            Assert.AreEqual(1, parent.Children.Count);
            Assert.AreEqual(0, child.Children.Count);
        }

        [Test]
        public void CreateChildScope_parents_the_host_under_the_scope_transform()
        {
            var parent = ProbeAsyncScope.Create<ProbeAsyncScope>();

            var child = parent.AddChild<ProbeChildAsyncScope>();

            Assert.AreSame(parent.transform, child.transform.parent);
        }

        [Test]
        public void Name_is_the_concrete_type_name()
        {
            Assert.AreEqual(nameof(ProbeAsyncScope), ProbeAsyncScope.Create<ProbeAsyncScope>().Name);
            Assert.AreEqual(nameof(ProbeChildAsyncScope), ProbeAsyncScope.Create<ProbeChildAsyncScope>().Name);
        }

        [Test]
        public void Child_scope_resolves_a_parent_service()
        {
            var parent = ProbeAsyncScope.Create<ProbeAsyncScope>();
            var service = parent.Add<AudioService>();
            var child = parent.AddChild<ProbeChildAsyncScope>();

            Assert.AreSame(service, child.GetService<AudioService>());
        }

        [Test]
        public void Child_layer_wins_over_the_parent_layer()
        {
            var parent = ProbeAsyncScope.Create<ProbeAsyncScope>();
            var parentService = parent.Add<AudioService>();
            var child = parent.AddChild<ProbeChildAsyncScope>();
            var childService = child.Add<AudioServiceOverride>();

            Assert.AreSame(childService, child.GetService<AudioServiceOverride>());
            Assert.AreSame(parentService, parent.GetService<AudioService>());
            Assert.AreSame(parentService, child.GetService<AudioService>());
        }

        [Test]
        public void Grandchild_resolves_the_topmost_ancestor_service()
        {
            var root = ProbeAsyncScope.Create<ProbeAsyncScope>();
            var service = root.Add<AudioService>();
            var child = root.AddChild<ProbeChildAsyncScope>();
            var grandchild = child.AddChild<ProbeGrandChildAsyncScope>();

            Assert.AreSame(service, grandchild.GetService<AudioService>());
        }

        [Test]
        public void GetService_throws_when_the_whole_chain_misses()
        {
            var root = ProbeAsyncScope.Create<ProbeAsyncScope>();
            var child = root.AddChild<ProbeChildAsyncScope>();
            var grandchild = child.AddChild<ProbeGrandChildAsyncScope>();

            Assert.Throws<InvalidOperationException>(() => { grandchild.GetService<AudioService>(); });
        }

        [Test]
        public void AddService_injects_the_scope_and_starts_the_service()
        {
            var scope = ProbeAsyncScope.Create<ProbeAsyncScope>();

            var service = scope.Add<AudioService>();

            Assert.AreSame(scope, service.InjectedScope);
            Assert.AreEqual(1, scope.Services.Count);
            Assert.AreSame(service, scope.Services[0]);
        }

        [Test]
        public void AddService_runs_the_service_init_hook()
        {
            var scope = ProbeAsyncScope.Create<ProbeAsyncScope>();

            scope.Add<AsyncLifecycleProbeA>();

            CollectionAssert.AreEqual(new[] { "init:A" }, LifecycleLog.Entries);
        }

        [Test]
        public void AddService_rejects_a_scope_type()
        {
            var scope = ProbeAsyncScope.Create<ProbeAsyncScope>();

            Assert.Throws<ArgumentException>(() => { scope.Add<ProbeChildAsyncScope>(); });
            Assert.AreEqual(0, scope.Services.Count);
        }

        [Test]
        public void RemoveService_deregisters_and_disposes_without_destroying_the_host()
        {
            var scope = ProbeAsyncScope.Create<ProbeAsyncScope>();
            var service = scope.Add<AsyncLifecycleProbeA>();

            scope.Remove<AsyncLifecycleProbeA>().GetAwaiter().GetResult();

            Assert.AreEqual(0, scope.Services.Count);
            Assert.IsTrue(service != null, "RemoveService must not destroy the service host.");
            CollectionAssert.AreEqual(new[] { "init:A", "dispose:A" }, LifecycleLog.Entries);
        }

        [Test]
        public void RemoveChild_deregisters_and_disposes_only_that_child()
        {
            var root = ProbeAsyncScope.Create<ProbeAsyncScope>();
            var first = root.AddChild<ProbeChildAsyncScope>();
            var second = root.AddChild<ProbeGrandChildAsyncScope>();

            root.RemoveChildScope<ProbeChildAsyncScope>().GetAwaiter().GetResult();

            Assert.AreEqual(1, root.Children.Count);
            Assert.AreSame(second, root.Children[0]);
            Assert.IsTrue(first == null, "RemoveChild must dispose the child and its host.");
            Assert.IsTrue(second != null);
        }

        [Test]
        public void Dispose_recursively_disposes_children()
        {
            var root = ProbeAsyncScope.Create<ProbeAsyncScope>();
            var child = root.AddChild<ProbeChildAsyncScope>();
            var grandchild = child.AddChild<ProbeGrandChildAsyncScope>();

            root.DisposeAsync().GetAwaiter().GetResult();

            Assert.IsFalse(root.IsInitialized);
            Assert.IsFalse(child.IsInitialized);
            Assert.IsFalse(grandchild.IsInitialized);
            Assert.IsTrue(root == null, "The scope must destroy its own host.");
            Assert.IsTrue(child == null);
            Assert.IsTrue(grandchild == null);
        }

        [Test]
        public void Dispose_cascades_the_service_hooks_children_first()
        {
            var root = ProbeAsyncScope.Create<ProbeAsyncScope>();
            root.Add<AsyncLifecycleProbeA>();
            var child = root.AddChild<ProbeChildAsyncScope>();
            child.Add<AsyncLifecycleProbeB>();

            root.DisposeAsync().GetAwaiter().GetResult();

            CollectionAssert.AreEqual(
                new[] { "init:A", "init:B", "dispose:B", "dispose:A" },
                LifecycleLog.Entries);
        }

        [Test]
        public void Dispose_is_idempotent_and_a_child_dispose_does_not_touch_the_parent()
        {
            var root = ProbeAsyncScope.Create<ProbeAsyncScope>();
            var child = root.AddChild<ProbeChildAsyncScope>();

            child.DisposeAsync().GetAwaiter().GetResult();
            child.DisposeAsync().GetAwaiter().GetResult();

            Assert.IsTrue(root.IsInitialized);
            Assert.AreEqual(1, root.Children.Count);
        }

        [Test]
        public void Dispose_before_InitAsync_is_a_no_op()
        {
            var scope = ProbeAsyncScope.CreateUninitialized<ProbeAsyncScope>();

            scope.DisposeAsync().GetAwaiter().GetResult();

            Assert.IsTrue(scope != null, "A scope that was never initialized must not destroy its host.");
            Assert.IsFalse(scope.IsInitialized);
        }

        [Test]
        public void Tick_visits_local_services_before_children()
        {
            var root = ProbeAsyncScope.Create<ProbeAsyncScope>();
            root.Add<TickProbeService>();
            var child = root.AddChild<ProbeChildAsyncScope>();
            child.Add<SecondaryTickProbeService>();

            root.Tick(0.5f);

            CollectionAssert.AreEqual(new[] { "tick:first", "tick:second" }, LifecycleLog.Entries);
        }

        [Test]
        public void Tick_skips_services_without_the_capability()
        {
            var scope = ProbeAsyncScope.Create<ProbeAsyncScope>();
            scope.Add<FixedOnlyProbeService>();

            scope.Tick(0.5f);

            Assert.AreEqual(0, LifecycleLog.Entries.Count);

            scope.FixedTick(0.5f);

            CollectionAssert.AreEqual(new[] { "fixedTick:only" }, LifecycleLog.Entries);
        }

        [Test]
        public void Dispose_still_destroys_the_host_when_a_child_throws()
        {
            var root = ProbeAsyncScope.Create<ProbeAsyncScope>();
            var child = root.AddChild<ThrowingDisposeAsyncScope>();
            Exception captured = null;

            try
            {
                root.DisposeAsync().GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                captured = exception;
            }

            Assert.IsInstanceOf<InvalidOperationException>(captured);
            Assert.AreEqual("child dispose failed", captured.Message);
            Assert.IsTrue(root == null, "The host must be destroyed even when a child throws.");
            Assert.IsTrue(child == null, "The throwing child must be torn down too.");
        }

        [Test]
        public void Tick_and_FixedTick_pass_the_delta()
        {
            var scope = ProbeAsyncScope.Create<ProbeAsyncScope>();
            var service = scope.Add<DeltaProbeService>();

            scope.Tick(0.25f);
            scope.FixedTick(0.75f);

            Assert.AreEqual(0.25f, service.LastTick);
            Assert.AreEqual(0.75f, service.LastFixedTick);
        }
    }
}
