using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Fang.Framework.EventBus.Tests
{
    public class EventBusTests
    {
        private sealed class PayloadA
        {
            public int Value;
        }

        private sealed class PayloadB
        {
            public int Value;
        }

        private readonly List<GameObject> _hosts = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (var i = _hosts.Count - 1; i >= 0; i--)
            {
                if (_hosts[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(_hosts[i]);
                }
            }

            _hosts.Clear();
        }

        [Test]
        public void Publish_invokes_the_subscriber_once_with_the_payload()
        {
            var bus = CreateBus();
            var received = new List<PayloadA>();
            Action<PayloadA> handler = payload => received.Add(payload);
            bus.Subscribe(handler);

            var payload = new PayloadA { Value = 7 };
            bus.Publish(payload);

            Assert.AreEqual(1, received.Count);
            Assert.AreSame(payload, received[0]);
        }

        [Test]
        public void Publish_invokes_same_type_subscribers_in_subscribe_order()
        {
            var bus = CreateBus();
            var order = new List<string>();
            bus.Subscribe<PayloadA>(_ => order.Add("first"));
            bus.Subscribe<PayloadA>(_ => order.Add("second"));

            bus.Publish(new PayloadA());

            CollectionAssert.AreEqual(new[] { "first", "second" }, order);
        }

        [Test]
        public void Subscribe_is_idempotent_for_the_same_delegate()
        {
            var bus = CreateBus();
            var count = 0;
            Action<PayloadA> handler = _ => count++;
            bus.Subscribe(handler);
            bus.Subscribe(handler);

            bus.Publish(new PayloadA());

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Unsubscribe_stops_delivery()
        {
            var bus = CreateBus();
            var count = 0;
            Action<PayloadA> handler = _ => count++;
            bus.Subscribe(handler);
            bus.Unsubscribe(handler);

            bus.Publish(new PayloadA());

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Publish_without_subscribers_does_not_throw()
        {
            var bus = CreateBus();

            Assert.DoesNotThrow(() => bus.Publish(new PayloadA()));
        }

        [Test]
        public void Publish_does_not_reach_other_payload_types()
        {
            var bus = CreateBus();
            var count = 0;
            bus.Subscribe<PayloadA>(_ => count++);

            bus.Publish(new PayloadB());

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Publish_after_OnDispose_delivers_nothing()
        {
            var bus = CreateBus();
            var count = 0;
            bus.Subscribe<PayloadA>(_ => count++);

            bus.OnDispose();

            Assert.DoesNotThrow(() => bus.Publish(new PayloadA()));
            Assert.AreEqual(0, count);
        }

        [Test]
        public void Subscribe_rejects_a_null_handler()
        {
            var bus = CreateBus();

            Assert.Throws<ArgumentNullException>(() => bus.Subscribe<PayloadA>(null));
        }

        private EventBus CreateBus()
        {
            var host = new GameObject("EventBus");
            _hosts.Add(host);
            return host.AddComponent<EventBus>();
        }
    }
}
