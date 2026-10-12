#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Text.RegularExpressions;
using Fang.Framework.Async;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Fang.Framework.CommandConsole.Async.Tests
{
    public class DebugCommandContextTests
    {
        [TearDown]
        public void TearDown()
        {
            TestScope.DestroyAll();
        }

        [Test]
        public void Finds_a_service_on_the_origin_scope()
        {
            var root = TestScope.Create();
            var service = root.Add<TestService>();

            var context = CreateContext(() => root);

            Assert.AreSame(service, context.GetService<TestService>());
            Assert.IsTrue(context.TryGetService<TestService>(out var found));
            Assert.AreSame(service, found);
        }

        [Test]
        public void Falls_back_to_a_descendant_scope()
        {
            var root = TestScope.Create();
            var child = root.CreateChild<TestScope>();
            var grandChild = child.CreateChild<TestScope>();
            var service = grandChild.Add<TestService>();

            var context = CreateContext(() => root);

            Assert.AreSame(service, context.GetService<TestService>());
        }

        [Test]
        public void Prefers_the_origin_scope_over_a_descendant()
        {
            var root = TestScope.Create();
            var child = root.CreateChild<TestScope>();

            var onRoot = root.Add<TestService>();
            child.Add<TestService>();

            var context = CreateContext(() => root);

            Assert.AreSame(onRoot, context.GetService<TestService>());
        }

        [Test]
        public void Returns_null_when_the_service_is_absent()
        {
            var root = TestScope.Create();
            root.CreateChild<TestScope>().Add<TestOtherService>();

            var context = CreateContext(() => root);

            Assert.IsNull(context.GetService<TestService>());
            Assert.IsFalse(context.TryGetService<TestService>(out var service));
            Assert.IsNull(service);
        }

        [Test]
        public void Returns_false_when_no_scope_is_available()
        {
            var context = CreateContext(() => null);

            Assert.IsNull(context.CurrentScope);
            Assert.IsFalse(context.TryGetService<TestService>(out var service));
            Assert.IsNull(service);
            Assert.IsNull(context.GetService<TestService>());
        }

        [Test]
        public void Ambiguous_matches_take_the_first_breadth_first_hit_and_warn_exactly_once()
        {
            var root = TestScope.Create();
            var first = root.CreateChild<TestScope>();
            var second = root.CreateChild<TestScope>();

            var firstService = first.Add<TestService>();
            var secondService = second.Add<TestService>();

            // 只有一条期望：若出现第二条警告，测试会因为「未预期的日志」而失败。
            LogAssert.Expect(LogType.Warning, new Regex("多个实例"));

            var context = CreateContext(() => root);
            var found = context.GetService<TestService>();

            Assert.AreSame(firstService, found);
            Assert.AreNotSame(secondService, found);
        }

        [Test]
        public void Finds_a_service_through_an_interface()
        {
            var root = TestScope.Create();
            var service = root.Add<TestService>();

            var context = CreateContext(() => root);

            Assert.AreSame(service, context.GetService<ITestService>());
        }

        [Test]
        public void Interface_lookup_also_walks_up_to_the_parent()
        {
            var root = TestScope.Create();
            var child = root.CreateChild<TestScope>();
            var service = root.Add<TestService>();

            var context = CreateContext(() => child);

            Assert.AreSame(service, context.GetService<ITestService>());
        }

        private static DebugCommandContext CreateContext(Func<AsyncScope> scopeProvider)
        {
            return new DebugCommandContext(new DebugCommandRegistry(), scopeProvider);
        }
    }
}
#endif
