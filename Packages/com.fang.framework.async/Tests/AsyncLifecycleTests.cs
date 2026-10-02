using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Fang.Framework.Async.Tests
{
    internal sealed class PlainProbe : AsyncLifecycleBehaviour
    {
        public int InitCount;
        public int DisposeCount;

        protected override UniTask OnInitAsync()
        {
            InitCount++;
            return UniTask.CompletedTask;
        }

        protected override UniTask OnDisposeAsync()
        {
            DisposeCount++;
            return UniTask.CompletedTask;
        }
    }

    internal sealed class ReentrantProbe : AsyncLifecycleBehaviour
    {
        public bool HandleWasAlreadyCompleted;

        protected override UniTask OnInitAsync()
        {
            HandleWasAlreadyCompleted = InitAsync().Status != UniTaskStatus.Pending;
            return UniTask.CompletedTask;
        }
    }

    internal sealed class FailingProbe : AsyncLifecycleBehaviour
    {
        protected override UniTask OnInitAsync()
        {
            throw new InvalidOperationException("hook failed");
        }
    }

    internal sealed class GatedProbe : AsyncLifecycleBehaviour
    {
        public readonly UniTaskCompletionSource Gate = new UniTaskCompletionSource();

        protected override async UniTask OnInitAsync()
        {
            await Gate.Task;
        }
    }

    internal sealed class ThrowingDisposeProbe : AsyncLifecycleBehaviour
    {
        protected override UniTask OnDisposeAsync()
        {
            throw new InvalidOperationException("dispose failed");
        }
    }

    public class AsyncLifecycleTests
    {
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

        [UnityTest]
        public IEnumerator InitAsync_runs_the_hook_once_and_reports_completion()
        {
            var probe = New<PlainProbe>();

            Assert.IsFalse(probe.IsInitialized);
            Assert.IsFalse(probe.IsInitializing);
            Assert.AreEqual(0f, probe.InitProgress);

            yield return probe.InitAsync().ToCoroutine(null);
            yield return probe.InitAsync().ToCoroutine(null);

            Assert.AreEqual(1, probe.InitCount);
            Assert.IsTrue(probe.IsInitialized);
            Assert.IsFalse(probe.IsInitializing);
            Assert.AreEqual(1f, probe.InitProgress);
        }

        [UnityTest]
        public IEnumerator InitAsync_returns_the_same_pending_handle_while_running()
        {
            var probe = New<GatedProbe>();

            var handle = probe.InitAsync();

            Assert.AreEqual(UniTaskStatus.Pending, handle.Status);
            Assert.IsTrue(probe.IsInitializing);
            Assert.IsFalse(probe.IsInitialized);

            var again = probe.InitAsync();

            Assert.AreEqual(UniTaskStatus.Pending, again.Status);

            probe.Gate.TrySetResult();

            yield return handle.ToCoroutine(null);

            Assert.IsFalse(probe.IsInitializing);
            Assert.IsTrue(probe.IsInitialized);
            Assert.AreEqual(1f, probe.InitProgress);
        }

        [UnityTest]
        public IEnumerator Reentrant_InitAsync_does_not_get_a_fake_completed_handle()
        {
            var probe = New<ReentrantProbe>();

            yield return probe.InitAsync().ToCoroutine(null);

            Assert.IsFalse(probe.HandleWasAlreadyCompleted);
            Assert.IsTrue(probe.IsInitialized);
        }

        [UnityTest]
        public IEnumerator Failing_hook_surfaces_the_original_exception()
        {
            var probe = New<FailingProbe>();
            Exception captured = null;

            yield return probe.InitAsync().ToCoroutine(exception => { captured = exception; });

            Assert.IsInstanceOf<InvalidOperationException>(captured);
            Assert.AreEqual("hook failed", captured.Message);
            Assert.IsFalse(probe.IsInitialized);
            Assert.AreEqual(0f, probe.InitProgress);
        }

        [Test]
        public void InitStarted_and_InitCompleted_fire_once_with_this_instance()
        {
            var probe = New<PlainProbe>();
            var started = 0;
            var completed = 0;
            AsyncLifecycleBehaviour startedSender = null;
            AsyncLifecycleBehaviour completedSender = null;

            probe.InitStarted += sender =>
            {
                started++;
                startedSender = sender;
            };

            probe.InitCompleted += sender =>
            {
                completed++;
                completedSender = sender;
            };

            probe.InitAsync().GetAwaiter().GetResult();
            probe.InitAsync().GetAwaiter().GetResult();

            Assert.AreEqual(1, started);
            Assert.AreEqual(1, completed);
            Assert.AreSame(probe, startedSender);
            Assert.AreSame(probe, completedSender);
        }

        [Test]
        public void ReportInitProgress_clamps_and_skips_unchanged_values()
        {
            var probe = New<PlainProbe>();
            var changes = new List<float>();

            probe.InitProgressChanged += (sender, value) => changes.Add(value);

            probe.ReportInitProgress(-1f);
            probe.ReportInitProgress(0f);

            Assert.AreEqual(0f, probe.InitProgress);
            Assert.AreEqual(0, changes.Count);

            probe.ReportInitProgress(0.5f);
            probe.ReportInitProgress(0.5f);
            probe.ReportInitProgress(2f);
            probe.ReportInitProgress(1f);

            CollectionAssert.AreEqual(new[] { 0.5f, 1f }, changes);
            Assert.AreEqual(1f, probe.InitProgress);
        }

        [UnityTest]
        public IEnumerator Dispose_runs_the_hook_once_and_clears_the_state()
        {
            var probe = New<PlainProbe>();

            yield return probe.InitAsync().ToCoroutine(null);
            yield return probe.DisposeAsync().ToCoroutine(null);
            yield return probe.DisposeAsync().ToCoroutine(null);

            Assert.AreEqual(1, probe.DisposeCount);
            Assert.IsFalse(probe.IsInitialized);
            Assert.IsFalse(probe.IsInitializing);
        }

        [UnityTest]
        public IEnumerator Dispose_before_InitAsync_is_a_no_op()
        {
            var probe = New<PlainProbe>();

            yield return probe.DisposeAsync().ToCoroutine(null);

            Assert.AreEqual(0, probe.DisposeCount);
            Assert.IsFalse(probe.IsInitialized);

            yield return probe.InitAsync().ToCoroutine(null);
            yield return probe.DisposeAsync().ToCoroutine(null);

            Assert.AreEqual(1, probe.DisposeCount);
        }

        [Test]
        public void Disposal_handle_is_completed_after_dispose()
        {
            var probe = New<PlainProbe>();

            Assert.AreEqual(UniTaskStatus.Succeeded, probe.Disposal.Status);

            probe.InitAsync().GetAwaiter().GetResult();

            var disposal = probe.DisposeAsync();

            Assert.AreEqual(UniTaskStatus.Succeeded, disposal.Status);
            Assert.AreEqual(UniTaskStatus.Succeeded, probe.DisposeAsync().Status);
        }

        [UnityTest]
        public IEnumerator Disposing_while_initializing_cancels_the_pending_handle()
        {
            var probe = New<GatedProbe>();
            var initialization = probe.InitAsync();

            Assert.AreEqual(UniTaskStatus.Pending, initialization.Status);

            yield return probe.DisposeAsync().ToCoroutine(null);

            Exception captured = null;
            yield return initialization.ToCoroutine(exception => { captured = exception; });

            Assert.IsInstanceOf<OperationCanceledException>(captured);

            probe.Gate.TrySetResult();

            Assert.IsFalse(probe.IsInitialized, "A late hook completion must not resurrect a disposed scope.");
            Assert.IsFalse(probe.IsInitializing);
        }

        [UnityTest]
        public IEnumerator Disposal_exception_reaches_the_awaiter()
        {
            var probe = New<ThrowingDisposeProbe>();

            yield return probe.InitAsync().ToCoroutine(null);

            Exception captured = null;
            yield return probe.DisposeAsync().ToCoroutine(exception => { captured = exception; });

            Assert.IsInstanceOf<InvalidOperationException>(captured);
            Assert.AreEqual("dispose failed", captured.Message);
            Assert.IsFalse(probe.IsInitialized);
        }

        [Test]
        public void Dispose_clears_the_events()
        {
            var probe = New<PlainProbe>();
            var changes = new List<float>();

            probe.InitProgressChanged += (sender, value) => changes.Add(value);

            probe.InitAsync().GetAwaiter().GetResult();
            probe.DisposeAsync().GetAwaiter().GetResult();

            probe.ReportInitProgress(0.25f);

            Assert.AreEqual(0, changes.Count);
        }

        private T New<T>() where T : AsyncLifecycleBehaviour
        {
            var host = new GameObject(typeof(T).Name);
            _hosts.Add(host);
            return host.AddComponent<T>();
        }
    }
}
