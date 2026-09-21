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

        [TearDown]
        public void TearDown()
        {
            ProbeScope.DestroyAll();
        }

        [Test]
        public void AddService_injects_the_owning_scope_and_returns_the_instance()
        {
            var scope = ProbeScope.Create<ProbeScope>();

            var service = scope.Add<AudioService>();

            Assert.AreSame(scope, service.InjectedScope);
            Assert.AreSame(service, scope.GetService<AudioService>());
            Assert.AreEqual(1, scope.Services.Count);
        }

        [Test]
        public void AddService_initializes_the_service_right_away()
        {
            var scope = ProbeScope.Create<ProbeScope>();

            scope.Add<LifecycleProbeA>();

            CollectionAssert.AreEqual(new[] { "init:A" }, LifecycleLog.Entries);
        }

        [Test]
        public void AddService_places_the_service_under_the_services_node()
        {
            var scope = ProbeScope.Create<ProbeScope>();

            var service = scope.Add<AudioService>();

            Assert.AreEqual("Services", service.transform.parent.name);
            Assert.AreSame(scope.transform, service.transform.parent.parent);
        }

        [Test]
        public void OnInit_flips_IsInitialized()
        {
            var scope = ProbeScope.CreateUninitialized<ProbeScope>();

            Assert.IsFalse(scope.IsInitialized);

            scope.OnInit();

            Assert.IsTrue(scope.IsInitialized);
        }

        [Test]
        public void GetService_throws_for_an_unregistered_service()
        {
            var scope = ProbeScope.Create<ProbeScope>();

            Assert.Throws<InvalidOperationException>(() => { scope.GetService<AudioService>(); });
        }

        [Test]
        public void GetService_message_names_the_service_and_the_scope()
        {
            var scope = ProbeScope.Create<ProbeScope>();

            var exception = Assert.Throws<InvalidOperationException>(() => { scope.GetService<AudioService>(); });

            StringAssert.Contains(typeof(AudioService).FullName, exception.Message);
            StringAssert.Contains(nameof(ProbeScope), exception.Message);
        }

        [Test]
        public void RemoveService_disposes_and_unregisters_the_service()
        {
            var scope = ProbeScope.Create<ProbeScope>();
            scope.Add<LifecycleProbeA>();

            scope.Remove<LifecycleProbeA>();

            CollectionAssert.AreEqual(new[] { "init:A", "dispose:A" }, LifecycleLog.Entries);
            Assert.AreEqual(0, scope.Services.Count);
            Assert.Throws<InvalidOperationException>(() => { scope.GetService<LifecycleProbeA>(); });
        }

        [Test]
        public void RemoveService_keeps_the_game_object_alive()
        {
            var scope = ProbeScope.Create<ProbeScope>();
            var service = scope.Add<AudioService>();

            scope.Remove<AudioService>();

            Assert.IsTrue(service != null);
            Assert.Throws<InvalidOperationException>(() => { scope.GetService<AudioService>(); });
        }

        [Test]
        public void RemoveService_is_a_no_op_when_the_service_is_absent()
        {
            var scope = ProbeScope.Create<ProbeScope>();

            Assert.DoesNotThrow(() => { scope.Remove<AudioService>(); });
        }

        [Test]
        public void GetService_returns_the_first_match_in_add_order()
        {
            var scope = ProbeScope.Create<ProbeScope>();
            var first = scope.Add<AudioService>();
            scope.Add<AudioServiceOverride>();

            Assert.AreSame(first, scope.GetService<Service>());
        }
    }
}
