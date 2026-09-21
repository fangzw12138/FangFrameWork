using System;
using NUnit.Framework;

namespace Fang.Framework.Tests
{
    public class ScopeHierarchyTests
    {
        [SetUp]
        public void SetUp()
        {
            LifecycleLog.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            ProbeScope.DestroyAll();
        }

        [Test]
        public void CreateChildScope_records_both_directions()
        {
            var parent = ProbeScope.Create<ProbeScope>();

            var child = parent.AddChild<ProbeChildScope>();

            Assert.AreSame(parent, child.Parent);
            Assert.AreSame(child, parent.Children[0]);
            Assert.AreEqual(1, parent.Children.Count);
            Assert.AreEqual(0, child.Children.Count);
        }

        [Test]
        public void CreateChildScope_parents_the_host_under_the_scope_transform()
        {
            var parent = ProbeScope.Create<ProbeScope>();

            var child = parent.AddChild<ProbeChildScope>();

            Assert.AreSame(parent.transform, child.transform.parent);
        }

        [Test]
        public void Name_is_the_concrete_type_name()
        {
            Assert.AreEqual(nameof(ProbeScope), ProbeScope.Create<ProbeScope>().Name);
            Assert.AreEqual(nameof(ProbeChildScope), ProbeScope.Create<ProbeChildScope>().Name);
        }

        [Test]
        public void Child_scope_resolves_a_parent_service()
        {
            var parent = ProbeScope.Create<ProbeScope>();
            var service = parent.Add<AudioService>();
            var child = parent.AddChild<ProbeChildScope>();

            Assert.AreSame(service, child.GetService<AudioService>());
        }

        [Test]
        public void Child_layer_wins_over_the_parent_layer()
        {
            var parent = ProbeScope.Create<ProbeScope>();
            var parentService = parent.Add<AudioService>();
            var child = parent.AddChild<ProbeChildScope>();
            var childService = child.Add<AudioServiceOverride>();

            Assert.AreSame(childService, child.GetService<Service>());
            Assert.AreSame(childService, child.GetService<AudioServiceOverride>());
            Assert.AreSame(parentService, parent.GetService<Service>());
            Assert.AreSame(parentService, child.GetService<AudioService>());
        }

        [Test]
        public void Grandchild_resolves_the_topmost_ancestor_service()
        {
            var root = ProbeScope.Create<ProbeScope>();
            var service = root.Add<AudioService>();
            var child = root.AddChild<ProbeChildScope>();
            var grandchild = child.AddChild<ProbeGrandChildScope>();

            Assert.AreSame(service, grandchild.GetService<AudioService>());
        }

        [Test]
        public void GetService_throws_when_the_whole_chain_misses()
        {
            var root = ProbeScope.Create<ProbeScope>();
            var child = root.AddChild<ProbeChildScope>();
            var grandchild = child.AddChild<ProbeGrandChildScope>();

            Assert.Throws<InvalidOperationException>(() => { grandchild.GetService<AudioService>(); });
        }

        [Test]
        public void Parent_dispose_recursively_disposes_children()
        {
            var root = ProbeScope.Create<ProbeScope>();
            var child = root.AddChild<ProbeChildScope>();
            var grandchild = child.AddChild<ProbeGrandChildScope>();

            root.OnDispose();

            Assert.IsFalse(root.IsInitialized);
            Assert.IsFalse(child.IsInitialized);
            Assert.IsFalse(grandchild.IsInitialized);
            Assert.AreEqual(0, root.Children.Count);
            Assert.IsNull(child.Parent);
        }

        [Test]
        public void Dispose_is_idempotent_and_child_dispose_does_not_touch_the_parent()
        {
            var root = ProbeScope.Create<ProbeScope>();
            root.Add<LifecycleProbeA>();
            var child = root.AddChild<ProbeChildScope>();
            child.Add<LifecycleProbeB>();

            child.OnDispose();
            child.OnDispose();

            Assert.IsFalse(child.IsInitialized);
            Assert.IsTrue(root.IsInitialized);

            root.OnDispose();

            CollectionAssert.AreEqual(
                new[] { "init:A", "init:B", "dispose:B", "dispose:A" },
                LifecycleLog.Entries);
        }
    }
}
