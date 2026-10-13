using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Fang.Framework.Async.Tests
{
    internal sealed class SilentGatedProbeService : AsyncService
    {
        public static UniTaskCompletionSource Gate;

        protected override async UniTask OnInitAsync()
        {
            await Gate.Task;
        }
    }

    internal sealed class HalfwayGatedProbeService : AsyncService
    {
        public static UniTaskCompletionSource Gate;

        protected override async UniTask OnInitAsync()
        {
            ReportInitProgress(0.5f);
            await Gate.Task;
        }
    }

    internal sealed class GatedProbeChildScope : ProbeAsyncScope
    {
        public static UniTaskCompletionSource Gate;

        protected override async UniTask OnInitAsync()
        {
            Add<HalfwayGatedProbeService>();
            await Gate.Task;
        }
    }

    internal sealed class LazyParentProbeScope : ProbeAsyncScope
    {
        public TChild Attach<TChild>() where TChild : AsyncScope
        {
            return CreateChildScope<TChild>();
        }
    }

    public class AsyncProgressTests
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
        public void Scope_progress_is_the_equal_weight_average_of_its_members()
        {
            var scope = ProbeAsyncScope.CreateUninitialized<ProbeAsyncScope>();

            Assert.AreEqual(0f, scope.InitProgress, "A scope holding nothing must stay at 0.");

            SilentGatedProbeService.Gate = new UniTaskCompletionSource();
            scope.Add<SilentGatedProbeService>();

            Assert.AreEqual(0f, scope.InitProgress, "A member that never reports must not lift the average.");

            scope.Add<AsyncLifecycleProbeA>();

            Assert.AreEqual(0.5f, scope.InitProgress, "Services and child scopes weigh the same.");
        }

        [UnityTest]
        public IEnumerator Scope_progress_follows_a_member_that_reports_then_finishes()
        {
            var scope = ProbeAsyncScope.CreateUninitialized<ProbeAsyncScope>();
            HalfwayGatedProbeService.Gate = new UniTaskCompletionSource();

            scope.Add<HalfwayGatedProbeService>();

            Assert.AreEqual(0.5f, scope.InitProgress, "The scope must mirror the member's own report.");

            HalfwayGatedProbeService.Gate.TrySetResult();
            yield return null;

            Assert.AreEqual(1f, scope.InitProgress, "Completion must reach the scope by event, not by polling.");
        }

        [UnityTest]
        public IEnumerator Child_scope_progress_reaches_the_parent()
        {
            var parent = ProbeAsyncScope.CreateUninitialized<LazyParentProbeScope>();
            HalfwayGatedProbeService.Gate = new UniTaskCompletionSource();
            GatedProbeChildScope.Gate = new UniTaskCompletionSource();

            var child = parent.Attach<GatedProbeChildScope>();

            Assert.AreEqual(0f, parent.InitProgress);

            child.InitAsync().Forget();

            Assert.AreEqual(0.5f, child.InitProgress);
            Assert.AreEqual(0.5f, parent.InitProgress, "A child scope counts as one member of its parent.");

            GatedProbeChildScope.Gate.TrySetResult();
            yield return null;

            Assert.AreEqual(1f, child.InitProgress);
            Assert.AreEqual(1f, parent.InitProgress);
        }

        [Test]
        public void An_initialized_scope_keeps_its_progress_at_one()
        {
            var scope = ProbeAsyncScope.Create<ProbeAsyncScope>();
            SilentGatedProbeService.Gate = new UniTaskCompletionSource();

            scope.Add<SilentGatedProbeService>();

            Assert.AreEqual(1f, scope.InitProgress, "A finished scope must not be dragged back down.");
        }

        [Test]
        public void Removing_a_member_recomputes_the_average()
        {
            var scope = ProbeAsyncScope.CreateUninitialized<ProbeAsyncScope>();
            SilentGatedProbeService.Gate = new UniTaskCompletionSource();

            scope.Add<SilentGatedProbeService>();
            scope.Add<AsyncLifecycleProbeA>();

            Assert.AreEqual(0.5f, scope.InitProgress);

            scope.Remove<AsyncLifecycleProbeA>().GetAwaiter().GetResult();

            Assert.AreEqual(0f, scope.InitProgress, "A detached member must stop counting.");
        }

        [Test]
        public void Completion_reports_progress_and_raises_the_event()
        {
            var host = new GameObject(nameof(Completion_reports_progress_and_raises_the_event));
            var changes = new List<float>();

            try
            {
                var service = host.AddComponent<AsyncLifecycleProbeA>();

                service.InitProgressChanged += (sender, value) => changes.Add(value);

                service.InitAsync().GetAwaiter().GetResult();

                CollectionAssert.AreEqual(new[] { 1f }, changes, "Completion must be reported like any other value.");
                Assert.AreEqual(1f, service.InitProgress);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }
    }
}
