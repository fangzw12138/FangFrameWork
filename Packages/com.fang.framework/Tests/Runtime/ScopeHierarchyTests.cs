using NUnit.Framework;

namespace Fang.Framework.Tests
{
    public class ScopeHierarchyTests
    {
        [Test]
        public void Child_resolves_parent_registration()
        {
            using (var parent = new Scope("parent"))
            using (var child = parent.CreateChild("child"))
            {
                parent.Register<IProbeService, ProbeService>();
                parent.Build();
                child.Build();

                Assert.IsInstanceOf<ProbeService>(child.Resolve<IProbeService>());
            }
        }

        [Test]
        public void Child_registration_shadows_parent_registration()
        {
            using (var parent = new Scope("parent"))
            using (var child = parent.CreateChild("child"))
            {
                parent.Register<IProbeService, ProbeService>();
                child.Register<IProbeService, ProbeServiceOverride>();
                parent.Build();
                child.Build();

                Assert.IsInstanceOf<ProbeServiceOverride>(child.Resolve<IProbeService>());
                Assert.IsInstanceOf<ProbeService>(parent.Resolve<IProbeService>());
            }
        }

        [Test]
        public void Child_shares_parent_singleton()
        {
            using (var parent = new Scope("parent"))
            using (var child = parent.CreateChild("child"))
            {
                parent.Register<IProbeService, ProbeService>();
                parent.Build();
                child.Build();

                Assert.AreSame(parent.Resolve<IProbeService>(), child.Resolve<IProbeService>());
            }
        }

        [Test]
        public void Parent_dispose_does_not_affect_child()
        {
            var parent = new Scope("parent");

            using (var child = parent.CreateChild("child"))
            {
                child.Register<IProbeService, ProbeService>();
                parent.Build();
                child.Build();

                var resolved = child.Resolve<IProbeService>();
                parent.Dispose();

                Assert.IsTrue(parent.IsDisposed);
                Assert.IsFalse(child.IsDisposed);
                Assert.AreSame(resolved, child.Resolve<IProbeService>());
            }
        }

        [Test]
        public void Parent_chain_miss_throws()
        {
            using (var parent = new Scope("parent"))
            using (var child = parent.CreateChild("child"))
            {
                parent.Build();
                child.Build();

                Assert.Throws<ScopeResolveException>(() => { child.Resolve<UnregisteredService>(); });
            }
        }
    }
}
