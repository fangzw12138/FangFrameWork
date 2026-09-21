using NUnit.Framework;

namespace Fang.Framework.Tests
{
    public class ScopeRegistrationTests
    {
        [Test]
        public void Resolve_throws_for_unregistered_type()
        {
            using (var scope = new Scope("registration"))
            {
                scope.Register<IProbeService, ProbeService>();
                scope.Build();

                Assert.Throws<ScopeResolveException>(() => { scope.Resolve<UnregisteredService>(); });
            }
        }

        [Test]
        public void Register_after_build_throws()
        {
            using (var scope = new Scope("registration"))
            {
                scope.Register<IProbeService, ProbeService>();
                scope.Build();

                Assert.Throws<ScopeStateException>(() => { scope.Register<UnregisteredService, UnregisteredService>(); });
                Assert.Throws<ScopeStateException>(() => { scope.RegisterInstance(new UnregisteredService()); });
                Assert.Throws<ScopeStateException>(() => { scope.Register<IProbeService>(_ => new ProbeFactoryResult()); });
            }
        }

        [Test]
        public void Build_twice_throws()
        {
            using (var scope = new Scope("registration"))
            {
                scope.Register<IProbeService, ProbeService>();
                scope.Build();

                Assert.Throws<ScopeStateException>(() => { scope.Build(); });
            }
        }

        [Test]
        public void Resolve_returns_the_same_instance_for_every_call()
        {
            using (var scope = new Scope("registration"))
            {
                scope.Register<IProbeService, ProbeService>();
                scope.Build();

                var first = scope.Resolve<IProbeService>();
                var second = scope.Resolve<IProbeService>();

                Assert.IsInstanceOf<ProbeService>(first);
                Assert.AreSame(first, second);
            }
        }

        [Test]
        public void Factory_and_instance_registrations_resolve()
        {
            var provided = new UnregisteredService();

            using (var scope = new Scope("registration"))
            {
                scope.Register<IProbeService>(_ => new ProbeFactoryResult());
                scope.RegisterInstance(provided);
                scope.Build();

                Assert.IsInstanceOf<ProbeFactoryResult>(scope.Resolve<IProbeService>());
                Assert.AreSame(provided, scope.Resolve<UnregisteredService>());
            }
        }

        [Test]
        public void Constructor_dependencies_are_injected()
        {
            using (var scope = new Scope("registration"))
            {
                scope.Register<IProbeService, ProbeService>();
                scope.Register<DependentService, DependentService>();
                scope.Build();

                var dependent = scope.Resolve<DependentService>();

                Assert.IsInstanceOf<ProbeService>(dependent.Probe);
                Assert.AreSame(scope.Resolve<IProbeService>(), dependent.Probe);
            }
        }

        [Test]
        public void TryResolve_reports_missing_registrations_without_throwing()
        {
            using (var scope = new Scope("registration"))
            {
                scope.Register<IProbeService, ProbeService>();
                scope.Build();

                Assert.IsTrue(scope.TryResolve<IProbeService>(out var probe));
                Assert.IsInstanceOf<ProbeService>(probe);

                Assert.IsFalse(scope.TryResolve<UnregisteredService>(out var missing));
                Assert.IsNull(missing);
            }
        }

        [Test]
        public void Circular_dependency_throws_with_the_full_chain()
        {
            using (var scope = new Scope("registration"))
            {
                scope.Register<CircularServiceA, CircularServiceA>();
                scope.Register<CircularServiceB, CircularServiceB>();

                var exception = Assert.Throws<ScopeCircularDependencyException>(() => { scope.Build(); });

                StringAssert.Contains(typeof(CircularServiceA).FullName, exception.Message);
                StringAssert.Contains(typeof(CircularServiceB).FullName, exception.Message);
            }
        }
    }
}
