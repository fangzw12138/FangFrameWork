using System.Collections.Generic;
using NUnit.Framework;

namespace Fang.Framework.UI.Tests
{
    public class UIPanelStackTests
    {
        private sealed class FakePanel : IUIPanel
        {
            public FakePanel(string name)
            {
                Name = name;
                PanelId = name + "#" + ++Sequence;
            }

            private static int Sequence;

            public static readonly List<string> Log = new List<string>();

            public string Name { get; }

            public string PanelId { get; }

            public string ConfigId => Name;

            public bool IsOpen { get; private set; }

            public bool IsVisible { get; private set; }

            public List<string> Calls { get; } = new List<string>();

            public void Open()
            {
                IsOpen = true;
                IsVisible = true;
                Record("open");
            }

            public void Close()
            {
                IsOpen = false;
                IsVisible = false;
                Record("close");
            }

            public void Focus()
            {
                Record("focus");
            }

            public void Blur()
            {
                Record("blur");
            }

            public void SetVisible(bool visible)
            {
                IsVisible = visible;
                Record(visible ? "show" : "hide");
            }

            public void OnDispose()
            {
                Record("dispose");
            }

            private void Record(string action)
            {
                Calls.Add(action);
                Log.Add(Name + "." + action);
            }
        }

        [SetUp]
        public void SetUp()
        {
            FakePanel.Log.Clear();
        }

        [Test]
        public void Push_opens_and_focuses_the_new_panel_and_blurs_the_previous_top()
        {
            var stack = new UIPanelStack();
            var first = new FakePanel("first");
            var second = new FakePanel("second");

            stack.Push(first, false, false);
            stack.Push(second, false, false);

            CollectionAssert.AreEqual(new[] { "open", "focus", "blur" }, first.Calls);
            CollectionAssert.AreEqual(new[] { "open", "focus" }, second.Calls);
            Assert.AreSame(second, stack.Top);
            Assert.AreEqual(2, stack.Count);
        }

        [Test]
        public void Push_with_close_previous_on_open_hides_and_closes_the_previous_panel()
        {
            var stack = new UIPanelStack();
            var first = new FakePanel("first");
            var second = new FakePanel("second");

            stack.Push(first, false, false);
            stack.Push(second, true, false);

            Assert.IsFalse(first.IsOpen);
            Assert.IsFalse(first.IsVisible);
            CollectionAssert.AreEqual(new[] { "open", "focus", "blur", "hide", "close" }, first.Calls);
        }

        [Test]
        public void Pop_restores_the_panel_that_was_hidden_by_the_closed_one()
        {
            var stack = new UIPanelStack();
            var first = new FakePanel("first");
            var second = new FakePanel("second");

            stack.Push(first, false, false);
            stack.Push(second, true, false);

            Assert.IsTrue(stack.Pop(second));

            Assert.IsTrue(first.IsOpen);
            Assert.IsTrue(first.IsVisible);
            CollectionAssert.AreEqual(
                new[] { "open", "focus", "blur", "hide", "close", "show", "open", "focus" },
                first.Calls);
            Assert.AreEqual(1, stack.Count);
            Assert.AreSame(first, stack.Top);
        }

        [Test]
        public void Pop_of_a_middle_panel_does_not_restore_an_unrelated_panel()
        {
            var stack = new UIPanelStack();
            var first = new FakePanel("first");
            var second = new FakePanel("second");
            var third = new FakePanel("third");

            stack.Push(first, false, false);
            stack.Push(second, false, false);
            stack.Push(third, true, false);

            Assert.IsTrue(stack.Pop(second));

            Assert.AreSame(third, stack.Top);
            Assert.AreEqual(2, stack.Count);
            CollectionAssert.AreEqual(new[] { "open", "focus", "blur" }, first.Calls);
            Assert.IsTrue(third.IsOpen);
        }

        [Test]
        public void Pop_of_an_unknown_panel_returns_false()
        {
            var stack = new UIPanelStack();
            var panel = new FakePanel("panel");

            Assert.IsFalse(stack.Pop(panel));
            Assert.IsFalse(stack.Contains(panel));
        }

        [Test]
        public void PopTop_on_an_empty_stack_returns_false()
        {
            var stack = new UIPanelStack();

            Assert.IsFalse(stack.PopTop());
            Assert.IsNull(stack.Top);
            Assert.AreEqual(0, stack.Count);
        }

        [Test]
        public void PopAll_closes_panels_from_top_to_bottom()
        {
            var stack = new UIPanelStack();
            var first = new FakePanel("first");
            var second = new FakePanel("second");
            var third = new FakePanel("third");

            stack.Push(first, false, false);
            stack.Push(second, false, false);
            stack.Push(third, false, false);
            FakePanel.Log.Clear();

            stack.PopAll();

            CollectionAssert.AreEqual(
                new[] { "third.blur", "third.close", "second.blur", "second.close", "first.blur", "first.close" },
                FakePanel.Log);
            Assert.AreEqual(0, stack.Count);
            Assert.IsNull(stack.Top);
            Assert.IsFalse(first.IsOpen);
            Assert.IsFalse(second.IsOpen);
            Assert.IsFalse(third.IsOpen);
        }

        [Test]
        public void Push_of_the_same_panel_is_idempotent()
        {
            var stack = new UIPanelStack();
            var panel = new FakePanel("panel");

            stack.Push(panel, false, false);
            stack.Push(panel, false, false);

            Assert.AreEqual(1, stack.Count);
            CollectionAssert.AreEqual(new[] { "open", "focus" }, panel.Calls);
        }

        [Test]
        public void Push_of_null_is_ignored()
        {
            var stack = new UIPanelStack();

            stack.Push(null, false, false);

            Assert.AreEqual(0, stack.Count);
        }

        [Test]
        public void Input_blocking_events_fire_once_per_transition()
        {
            var stack = new UIPanelStack();
            var opened = 0;
            var closed = 0;
            stack.InputBlockingPanelOpened += () => opened++;
            stack.InputBlockingPanelClosed += () => closed++;

            var first = new FakePanel("first");
            var second = new FakePanel("second");
            var third = new FakePanel("third");

            stack.Push(first, false, false);
            Assert.IsFalse(stack.HasInputBlocking);

            stack.Push(second, false, true);
            Assert.AreEqual(1, opened);
            Assert.IsTrue(stack.HasInputBlocking);

            stack.Push(third, false, true);
            Assert.AreEqual(1, opened);

            stack.Pop(third);
            Assert.AreEqual(0, closed);

            stack.Pop(second);
            Assert.AreEqual(1, closed);
            Assert.IsFalse(stack.HasInputBlocking);
        }

        [Test]
        public void PopAll_releases_input_blocking_once()
        {
            var stack = new UIPanelStack();
            var closed = 0;
            stack.InputBlockingPanelClosed += () => closed++;

            stack.Push(new FakePanel("first"), false, true);
            stack.Push(new FakePanel("second"), false, true);

            stack.PopAll();

            Assert.AreEqual(1, closed);
            Assert.IsFalse(stack.HasInputBlocking);
        }
    }
}
