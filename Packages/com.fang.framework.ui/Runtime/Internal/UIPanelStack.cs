using System;
using System.Collections.Generic;

namespace Fang.Framework.UI
{
    internal sealed class UIPanelStack
    {
        private sealed class Entry
        {
            public IUIPanel Panel;
            public bool BlocksInput;
            public bool HiddenByNext;
        }

        private readonly List<Entry> _entries = new List<Entry>();
        private int _inputBlockingCount;

        public event Action InputBlockingPanelOpened;

        public event Action InputBlockingPanelClosed;

        public int Count => _entries.Count;

        public bool HasInputBlocking => _inputBlockingCount > 0;

        public IUIPanel Top => _entries.Count == 0 ? null : _entries[_entries.Count - 1].Panel;

        public bool Contains(IUIPanel panel)
        {
            return IndexOf(panel) >= 0;
        }

        public void Push(IUIPanel panel, bool closePreviousOnOpen, bool blocksInput)
        {
            if (panel == null || Contains(panel))
            {
                return;
            }

            if (_entries.Count > 0)
            {
                var previous = _entries[_entries.Count - 1];
                previous.Panel.Blur();

                if (closePreviousOnOpen)
                {
                    previous.HiddenByNext = true;
                    previous.Panel.SetVisible(false);
                    previous.Panel.Close();
                }
            }

            _entries.Add(new Entry
            {
                Panel = panel,
                BlocksInput = blocksInput,
                HiddenByNext = false
            });

            panel.Open();
            panel.Focus();
            TrackInputBlocking(blocksInput);
        }

        public bool Pop(IUIPanel panel)
        {
            var index = IndexOf(panel);
            if (index < 0)
            {
                return false;
            }

            var entry = _entries[index];
            entry.Panel.Blur();
            entry.Panel.Close();

            _entries.RemoveAt(index);
            UntrackInputBlocking(entry.BlocksInput);
            RestorePrevious(index);
            return true;
        }

        public bool PopTop()
        {
            if (_entries.Count == 0)
            {
                return false;
            }

            return Pop(_entries[_entries.Count - 1].Panel);
        }

        public void PopAll()
        {
            while (_entries.Count > 0)
            {
                Pop(_entries[_entries.Count - 1].Panel);
            }
        }

        private void RestorePrevious(int removedIndex)
        {
            var previousIndex = removedIndex - 1;
            if (previousIndex < 0 || previousIndex >= _entries.Count)
            {
                return;
            }

            var previous = _entries[previousIndex];
            if (!previous.HiddenByNext)
            {
                return;
            }

            previous.HiddenByNext = false;
            previous.Panel.SetVisible(true);
            previous.Panel.Open();
            previous.Panel.Focus();
        }

        private void TrackInputBlocking(bool blocksInput)
        {
            if (!blocksInput)
            {
                return;
            }

            _inputBlockingCount++;
            if (_inputBlockingCount == 1)
            {
                InputBlockingPanelOpened?.Invoke();
            }
        }

        private void UntrackInputBlocking(bool blocksInput)
        {
            if (!blocksInput)
            {
                return;
            }

            _inputBlockingCount--;
            if (_inputBlockingCount > 0)
            {
                return;
            }

            _inputBlockingCount = 0;
            InputBlockingPanelClosed?.Invoke();
        }

        private int IndexOf(IUIPanel panel)
        {
            if (panel == null)
            {
                return -1;
            }

            for (var i = 0; i < _entries.Count; i++)
            {
                if (ReferenceEquals(_entries[i].Panel, panel))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
