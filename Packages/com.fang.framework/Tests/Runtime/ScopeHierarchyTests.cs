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

        [Test]
        public void CreateChildScope_records_both_directions()
        {
            var parent = new ProbeScope();

            var child = parent.AddChild<ProbeChildScope>();

            Assert.AreSame(parent, child.Parent);
            Assert.AreSame(child, parent.Children[0]);
            Assert.AreEqual(1, parent.Children.Count);
            Assert.AreEqual(0, child.Children.Count);
        }

        [Test]
        public void Name_is_the_concrete_type_name()
        {
            Assert.AreEqual(nameof(ProbeScope), new ProbeScope().Name);
            Assert.AreEqual(nameof(ProbeChildScope), new ProbeChildScope().Name);
        }

        [Test]
        public void Child_scope_resolves_a_parent_service()
        {
            var parent = new ProbeScope();
            var service = parent.Add<AudioService>();
            var child = parent.AddChild<ProbeChildScope>();

            Assert.AreSame(service, child.GetService<AudioService>());
        }

        [Test]
        public void Child_layer_wins_over_the_parent_layer()
        {
            var parent = new ProbeScope();
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
            var root = new ProbeScope();
            var service = root.Add<AudioService>();
            var child = root.AddChild<ProbeChildScope>();
            var grandchild = child.AddChild<ProbeGrandChildScope>();

            Assert.AreSame(service, grandchild.GetService<AudioService>());
        }

        [Test]
        public void GetService_throws_when_the_whole_chain_misses()
        {
            var root = new ProbeScope();
            var child = root.AddChild<ProbeChildScope>();
            var grandchild = child.AddChild<ProbeGrandChildScope>();

            Assert.Throws<InvalidOperationException>(() => { grandchild.GetService<AudioService>(); });
        }

        [Test]
        public void Parent_dispose_recursively_disposes_children()
        {
            var root = new ProbeScope();
            var child = root.AddChild<ProbeChildScope>();
            var grandchild = child.AddChild<ProbeGrandChildScope>();

            root.Dispose();

            Assert.IsTrue(root.IsDisposed);
            Assert.IsTrue(child.IsDisposed);
            Assert.IsTrue(grandchild.IsDisposed);
            Assert.AreEqual(0, root.Children.Count);
            Assert.IsNull(child.Parent);
        }

        [Test]
        public void Dispose_is_idempotent_and_child_dispose_does_not_touch_the_parent()
        {
            var root = new ProbeScope();
            root.Add<LifecycleProbeA>();
            var child = root.AddChild<ProbeChildScope>();
            child.Add<LifecycleProbeB>();

            child.Dispose();
            child.Dispose();

            Assert.IsTrue(child.IsDisposed);
            Assert.IsFalse(root.IsDisposed);

            root.Dispose();

            CollectionAssert.AreEqual(
                new[] { "create:A", "init:A", "create:B", "init:B", "dispose:B", "dispose:A" },
                LifecycleLog.Entries);
        }
    }
}
