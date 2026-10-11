using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Fang.Framework.UI.Async
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

        public int Count
        {
            get
            {
                PurgeDestroyed();
                return _entries.Count;
            }
        }

        public IUIPanel Top
        {
            get
            {
                PurgeDestroyed();
                return _entries.Count == 0 ? null : _entries[_entries.Count - 1].Panel;
            }
        }

        public bool HasInputBlocking => _inputBlockingCount > 0;

        internal static bool IsAlive(IUIPanel panel)
        {
            return panel != null && (!(panel is UnityEngine.Object unityObject) || unityObject != null);
        }

        private void PurgeDestroyed()
        {
            for (var i = _entries.Count - 1; i >= 0; i--)
            {
                var entry = _entries[i];

                if (IsAlive(entry.Panel))
                {
                    continue;
                }

                _entries.RemoveAt(i);
                UntrackInputBlocking(entry);
            }
        }

        public bool Contains(IUIPanel panel)
        {
            return IndexOf(panel) >= 0;
        }

        public bool Contains(string configId)
        {
            return IndexOf(configId) >= 0;
        }

        public IUIPanel Find(string configId)
        {
            var index = IndexOf(configId);
            return index < 0 ? null : _entries[index].Panel;
        }

        public async UniTask PushAsync(IUIPanel panel, bool closePreviousOnOpen, bool blocksInput)
        {
            if (!IsAlive(panel) || Contains(panel))
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
                    await CloseIfOpenAsync(previous.Panel);
                }
            }

            _entries.Add(new Entry { Panel = panel, BlocksInput = blocksInput });

            await panel.OpenAsync();
            panel.Focus();

            TrackInputBlocking(blocksInput);
        }

        public async UniTask<bool> PopAsync(IUIPanel panel)
        {
            var index = IndexOf(panel);

            if (index < 0)
            {
                return false;
            }

            var entry = _entries[index];

            entry.Panel.Blur();
            await CloseIfOpenAsync(entry.Panel);

            var current = IndexOf(panel);

            if (current < 0)
            {
                return false;
            }

            var wasTopNow = current == _entries.Count - 1;

            _entries.RemoveAt(current);
            UntrackInputBlocking(entry);

            if (wasTopNow)
            {
                await RestoreTopAsync();
            }

            return true;
        }

        public UniTask<bool> PopTopAsync()
        {
            var top = Top;
            return top == null ? UniTask.FromResult(false) : PopAsync(top);
        }

        public async UniTask PopAllAsync()
        {
            while (true)
            {
                PurgeDestroyed();

                if (_entries.Count == 0)
                {
                    return;
                }

                var index = _entries.Count - 1;
                var entry = _entries[index];

                _entries.RemoveAt(index);
                entry.Panel.Blur();
                await CloseIfOpenAsync(entry.Panel);
                UntrackInputBlocking(entry);
            }
        }

        private static UniTask CloseIfOpenAsync(IUIPanel panel)
        {
            return IsAlive(panel) && panel.IsOpen ? panel.CloseAsync() : UniTask.CompletedTask;
        }

        private async UniTask RestoreTopAsync()
        {
            if (_entries.Count == 0)
            {
                return;
            }

            var entry = _entries[_entries.Count - 1];

            if (!IsAlive(entry.Panel))
            {
                return;
            }

            if (entry.HiddenByNext)
            {
                entry.HiddenByNext = false;
                await entry.Panel.OpenAsync();
            }

            entry.Panel.Focus();
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

        private void UntrackInputBlocking(Entry entry)
        {
            if (!entry.BlocksInput)
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
            PurgeDestroyed();

            if (!IsAlive(panel))
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

        private int IndexOf(string configId)
        {
            PurgeDestroyed();

            if (string.IsNullOrEmpty(configId))
            {
                return -1;
            }

            for (var i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Panel.ConfigId == configId)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
