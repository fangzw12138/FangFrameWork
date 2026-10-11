using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Fang.Framework.UI.Async.Tests
{
    public class UIPanelStackTests
    {
        private readonly List<IUIPanel> _panels = new List<IUIPanel>();

        [TearDown]
        public void TearDown()
        {
            _panels.Clear();
        }

        [UnityTest]
        public IEnumerator Push_opens_then_focuses_the_panel()
        {
            var stack = new UIPanelStack();
            var panel = Track(new FakePanel("PanelA"));

            yield return stack.PushAsync(panel, false, false).ToCoroutine(null);

            Assert.AreEqual(1, stack.Count);
            Assert.AreSame(panel, stack.Top);
            CollectionAssert.AreEqual(new[] { "open", "focus" }, panel.Trace);
            Assert.IsTrue(panel.IsOpen);
            Assert.IsTrue(panel.IsVisible);
        }

        [UnityTest]
        public IEnumerator Push_with_close_previous_blurs_hides_then_closes_the_previous()
        {
            var stack = new UIPanelStack();
            var first = Track(new FakePanel("PanelA"));
            var second = Track(new FakePanel("PanelB"));

            yield return stack.PushAsync(first, false, false).ToCoroutine(null);
            yield return stack.PushAsync(second, true, false).ToCoroutine(null);

            CollectionAssert.AreEqual(
                new[] { "open", "focus", "blur", "visible:false", "close" },
                first.Trace);
            Assert.IsFalse(first.IsOpen);
            Assert.IsFalse(first.IsVisible);
            Assert.AreEqual(1, first.CloseCount);
            CollectionAssert.AreEqual(new[] { "open", "focus" }, second.Trace);
            Assert.AreSame(second, stack.Top);
            Assert.AreEqual(2, stack.Count);
        }

        [UnityTest]
        public IEnumerator Push_without_close_previous_keeps_the_previous_open()
        {
            var stack = new UIPanelStack();
            var first = Track(new FakePanel("PanelA"));
            var second = Track(new FakePanel("PanelB"));

            yield return stack.PushAsync(first, false, false).ToCoroutine(null);
            yield return stack.PushAsync(second, false, false).ToCoroutine(null);

            CollectionAssert.AreEqual(new[] { "open", "focus", "blur" }, first.Trace);
            Assert.IsTrue(first.IsOpen);
            Assert.IsTrue(first.IsVisible);
            Assert.AreEqual(2, stack.Count);
        }

        [UnityTest]
        public IEnumerator Push_ignores_null_and_already_stacked_panels()
        {
            var stack = new UIPanelStack();
            var panel = Track(new FakePanel("PanelA"));

            yield return stack.PushAsync(null, false, false).ToCoroutine(null);
            yield return stack.PushAsync(panel, false, false).ToCoroutine(null);
            yield return stack.PushAsync(panel, false, false).ToCoroutine(null);

            Assert.AreEqual(1, stack.Count);
            Assert.AreEqual(1, panel.OpenCount);
            Assert.AreEqual(1, panel.FocusCount);
        }

        [UnityTest]
        public IEnumerator Input_blocking_events_fire_only_on_transitions()
        {
            var stack = new UIPanelStack();
            var opened = 0;
            var closed = 0;
            stack.InputBlockingPanelOpened += () => opened++;
            stack.InputBlockingPanelClosed += () => closed++;

            yield return stack.PushAsync(Track(new FakePanel("PanelA")), false, false).ToCoroutine(null);

            Assert.AreEqual(0, opened);
            Assert.IsFalse(stack.HasInputBlocking);

            yield return stack.PushAsync(Track(new FakePanel("PanelB")), false, true).ToCoroutine(null);

            Assert.AreEqual(1, opened);
            Assert.AreEqual(0, closed);
            Assert.IsTrue(stack.HasInputBlocking);

            yield return stack.PushAsync(Track(new FakePanel("PanelC")), false, true).ToCoroutine(null);

            Assert.AreEqual(1, opened);

            yield return stack.PopTopAsync().ToCoroutine(null);

            Assert.AreEqual(0, closed);
            Assert.IsTrue(stack.HasInputBlocking);

            yield return stack.PopTopAsync().ToCoroutine(null);

            Assert.AreEqual(1, closed);
            Assert.IsFalse(stack.HasInputBlocking);
        }

        [UnityTest]
        public IEnumerator Pop_restores_the_hidden_previous_panel()
        {
            var stack = new UIPanelStack();
            var first = Track(new FakePanel("PanelA"));
            var second = Track(new FakePanel("PanelB"));

            yield return stack.PushAsync(first, false, false).ToCoroutine(null);
            yield return stack.PushAsync(second, true, false).ToCoroutine(null);

            var popped = false;
            yield return stack.PopAsync(second).ToCoroutine(result => popped = result);

            Assert.IsTrue(popped);
            Assert.AreEqual(1, stack.Count);
            Assert.AreSame(first, stack.Top);
            Assert.AreEqual(2, first.OpenCount);
            Assert.IsTrue(first.IsOpen);
            Assert.IsTrue(first.IsVisible);
            Assert.AreEqual(2, first.FocusCount);
        }

        [UnityTest]
        public IEnumerator Pop_returns_false_for_a_panel_that_is_not_stacked()
        {
            var stack = new UIPanelStack();

            var popped = true;
            yield return stack.PopAsync(new FakePanel("PanelA")).ToCoroutine(result => popped = result);

            Assert.IsFalse(popped);
        }

        [UnityTest]
        public IEnumerator PopTop_and_PopAll_are_no_ops_on_an_empty_stack()
        {
            var stack = new UIPanelStack();

            var popped = true;
            yield return stack.PopTopAsync().ToCoroutine(result => popped = result);
            Assert.IsFalse(popped);

            yield return stack.PopAllAsync().ToCoroutine(null);

            Assert.AreEqual(0, stack.Count);
            Assert.IsNull(stack.Top);
        }

        [UnityTest]
        public IEnumerator PopAll_closes_each_panel_once_from_the_top_down()
        {
            var stack = new UIPanelStack();
            var first = Track(new FakePanel("PanelA"));
            var second = Track(new FakePanel("PanelB"));
            var third = Track(new FakePanel("PanelC"));

            yield return stack.PushAsync(first, false, false).ToCoroutine(null);
            yield return stack.PushAsync(second, true, false).ToCoroutine(null);
            yield return stack.PushAsync(third, true, false).ToCoroutine(null);

            yield return stack.PopAllAsync().ToCoroutine(null);

            Assert.AreEqual(0, stack.Count);
            Assert.IsNull(stack.Top);
            Assert.AreEqual(1, third.CloseCount);
            Assert.AreEqual(1, second.CloseCount);
            Assert.AreEqual(1, first.CloseCount);
            CollectionAssert.AreEqual(new[] { "open", "focus", "blur", "close" }, third.Trace);
            CollectionAssert.AreEqual(
                new[] { "open", "focus", "blur", "visible:false", "close", "blur" },
                second.Trace);
            CollectionAssert.AreEqual(
                new[] { "open", "focus", "blur", "visible:false", "close", "blur" },
                first.Trace);
        }

        [UnityTest]
        public IEnumerator Find_and_Contains_match_the_config_id()
        {
            var stack = new UIPanelStack();
            var panel = Track(new FakePanel("PanelA"));

            yield return stack.PushAsync(panel, false, false).ToCoroutine(null);

            Assert.IsTrue(stack.Contains(panel));
            Assert.IsTrue(stack.Contains("PanelA"));
            Assert.IsFalse(stack.Contains("PanelB"));
            Assert.IsFalse(stack.Contains(string.Empty));
            Assert.AreSame(panel, stack.Find("PanelA"));
            Assert.IsNull(stack.Find("PanelB"));
        }

        private FakePanel Track(FakePanel panel)
        {
            _panels.Add(panel);
            return panel;
        }
    }
}
