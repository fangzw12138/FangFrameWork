using System;
using NUnit.Framework;

namespace Fang.Framework.Tests
{
    public class ScopeRegistrationTests
    {
        [SetUp]
        public void SetUp()
        {
            LifecycleLog.Reset();
        }

        [Test]
        public void AddService_injects_the_owning_scope_and_returns_the_instance()
        {
            var scope = new ProbeScope();

            var service = scope.Add<AudioService>();

            Assert.AreSame(scope, service.InjectedScope);
            Assert.AreSame(service, scope.GetService<AudioService>());
            Assert.AreEqual(1, scope.Services.Count);
        }

        [Test]
        public void AddService_initializes_the_service_right_away()
        {
            var scope = new ProbeScope();

            scope.Add<LifecycleProbeA>();

            CollectionAssert.AreEqual(new[] { "create:A", "init:A" }, LifecycleLog.Entries);
        }

        [Test]
        public void GetService_throws_for_an_unregistered_service()
        {
            var scope = new ProbeScope();

            Assert.Throws<InvalidOperationException>(() => { scope.GetService<AudioService>(); });
        }

        [Test]
        public void GetService_message_names_the_service_and_the_scope()
        {
            var scope = new ProbeScope();

            var exception = Assert.Throws<InvalidOperationException>(() => { scope.GetService<AudioService>(); });

            StringAssert.Contains(typeof(AudioService).FullName, exception.Message);
            StringAssert.Contains(nameof(ProbeScope), exception.Message);
        }

        [Test]
        public void RemoveService_disposes_and_unregisters_the_service()
        {
            var scope = new ProbeScope();
            scope.Add<LifecycleProbeA>();

            scope.Remove<LifecycleProbeA>();

            CollectionAssert.AreEqual(new[] { "create:A", "init:A", "dispose:A" }, LifecycleLog.Entries);
            Assert.AreEqual(0, scope.Services.Count);
            Assert.Throws<InvalidOperationException>(() => { scope.GetService<LifecycleProbeA>(); });
        }

        [Test]
        public void RemoveService_is_a_no_op_when_the_service_is_absent()
        {
            var scope = new ProbeScope();

            Assert.DoesNotThrow(() => { scope.Remove<AudioService>(); });
        }

        [Test]
        public void GetService_returns_the_first_match_in_add_order()
        {
            var scope = new ProbeScope();
            var first = scope.Add<AudioService>();
            scope.Add<AudioServiceOverride>();

            Assert.AreSame(first, scope.GetService<Service>());
        }
    }
}
