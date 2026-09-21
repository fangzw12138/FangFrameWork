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
        public void Build_creates_in_registration_order_then_injects_then_initializes()
        {
            using (var scope = new Scope("lifecycle"))
            {
                scope.Register<LifecycleProbeA, LifecycleProbeA>();
                scope.Register<LifecycleProbeB, LifecycleProbeB>();
                scope.Register<LifecycleProbeC, LifecycleProbeC>();
                scope.Build();

                CollectionAssert.AreEqual(
                    new[]
                    {
                        "create:A", "create:B", "create:C",
                        "inject:A", "inject:B", "inject:C",
                        "initialize:A", "initialize:B", "initialize:C"
                    },
                    LifecycleLog.Entries);
            }
        }

        [Test]
        public void Dispose_releases_in_reverse_creation_order_exactly_once()
        {
            var scope = new Scope("lifecycle");
            scope.Register<LifecycleProbeA, LifecycleProbeA>();
            scope.Register<LifecycleProbeB, LifecycleProbeB>();
            scope.Build();

            scope.Dispose();
            scope.Dispose();

            CollectionAssert.AreEqual(
                new[]
                {
                    "create:A", "create:B",
                    "inject:A", "inject:B",
                    "initialize:A", "initialize:B",
                    "dispose:B", "dispose:A"
                },
                LifecycleLog.Entries);
        }
    }
}
