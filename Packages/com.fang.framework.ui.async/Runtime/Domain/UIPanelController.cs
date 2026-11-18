using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Fang.Framework.Async;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Fang.Framework.UI.Async
{
    public abstract class UIPanelController<TConfig, TData> : AsyncController<TConfig, TData>, IUIPanel
        where TConfig : UIPanelConfigDataSo
        where TData : UIPanelData<TConfig>
    {
        private FocusTarget _lastFocus;
        private bool _focusSubscribed;

        public string PanelId => Data.RuntimeId;

        public string ConfigId => Data.Config.Id;

        public bool IsOpen => Data.IsOpen;

        public bool IsVisible => Data.IsVisible;

        protected virtual bool CanFocus => true;

        public abstract TData CreateData(TConfig config);

        public async UniTask OpenAsync()
        {
            if (this == null)
            {
                return;
            }

            _lastFocus = null;
            SetVisible(true);
            Data.MarkOpen();
            await OnOpenAsync();
        }

        public async UniTask CloseAsync()
        {
            if (this == null)
            {
                Data.MarkClosed();
                return;
            }

            await OnCloseAsync();

            Data.MarkClosed();
            SetVisible(false);
        }

        public void Focus()
        {
            if (this == null || !CanFocus)
            {
                return;
            }

            SetNavigationEnabled(true);
            SubscribeFocus();
            OnFocus();
        }

        public void Blur()
        {
            UnsubscribeFocus();
            SetNavigationEnabled(false);
            OnBlur();
        }

        public void SetVisible(bool visible)
        {
            if (this == null)
            {
                return;
            }

            SetVisibleCore(visible);
            Data.SetVisible(visible);
        }

        protected virtual UniTask OnOpenAsync()
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask OnCloseAsync()
        {
            return UniTask.CompletedTask;
        }

        protected virtual void OnFocus()
        {
        }

        protected virtual void OnBlur()
        {
        }

        protected virtual void SetVisibleCore(bool visible)
        {
            if (this == null)
            {
                return;
            }

            gameObject.SetActive(visible);
        }

        protected void RestoreFocus(FocusTarget fallback = null)
        {
            if (this == null || !CanFocus)
            {
                return;
            }

            var eventSystem = GetEventSystem();

            if (eventSystem == null)
            {
                return;
            }

            var current = eventSystem.currentSelectedGameObject;

            if (IsValidTarget(GetFocusTarget(current)))
            {
                return;
            }

            var nearest = FindNearestTarget(current);

            if (IsValidTarget(nearest))
            {
                Select(eventSystem, nearest);
                return;
            }

            if (IsValidTarget(_lastFocus))
            {
                Select(eventSystem, _lastFocus);
                return;
            }

            if (IsValidTarget(fallback))
            {
                Select(eventSystem, fallback);
            }
        }

        private void OnEnable()
        {
            if (CanFocus)
            {
                SetNavigationEnabled(true);
            }
        }

        private void OnDisable()
        {
            UnsubscribeFocus();
            SetNavigationEnabled(false);
        }

        private void SubscribeFocus()
        {
            if (_focusSubscribed)
            {
                return;
            }

            _focusSubscribed = true;
            FocusTarget.AnySelected += OnAnyFocusSelected;
        }

        private void UnsubscribeFocus()
        {
            if (!_focusSubscribed)
            {
                return;
            }

            _focusSubscribed = false;
            FocusTarget.AnySelected -= OnAnyFocusSelected;
        }

        private void OnAnyFocusSelected(FocusTarget target)
        {
            if (target != null && IsUnderPanel(target.transform))
            {
                _lastFocus = target;
            }
        }

        private bool IsUnderPanel(Transform target)
        {
            if (target == null || this == null)
            {
                return false;
            }

            return target == transform || target.IsChildOf(transform);
        }

        private bool IsValidTarget(FocusTarget target)
        {
            return target != null &&
                   target.isActiveAndEnabled &&
                   target.IsInteractable() &&
                   IsUnderPanel(target.transform);
        }

        private static FocusTarget GetFocusTarget(GameObject gameObject)
        {
            return gameObject != null ? gameObject.GetComponent<FocusTarget>() : null;
        }

        private static EventSystem GetEventSystem()
        {
            var current = EventSystem.current;
            return current != null ? current : Object.FindAnyObjectByType<EventSystem>();
        }

        private FocusTarget FindNearestTarget(GameObject current)
        {
            if (current == null || !IsUnderPanel(current.transform))
            {
                return null;
            }

            var targets = GetComponentsInChildren<FocusTarget>(true);
            FocusTarget nearest = null;
            var nearestDistance = int.MaxValue;
            var currentTransform = current.transform;

            for (var i = 0; i < targets.Length; i++)
            {
                var distance = HierarchyDistance(currentTransform, targets[i].transform);

                if (distance >= nearestDistance)
                {
                    continue;
                }

                nearest = targets[i];
                nearestDistance = distance;
            }

            return nearest;
        }

        private static int HierarchyDistance(Transform a, Transform b)
        {
            if (a == null || b == null)
            {
                return int.MaxValue;
            }

            var ancestors = new Dictionary<Transform, int>();
            var depth = 0;

            for (var node = a; node != null; node = node.parent)
            {
                ancestors[node] = depth++;
            }

            var bDepth = 0;

            for (var node = b; node != null; node = node.parent, bDepth++)
            {
                if (!ancestors.TryGetValue(node, out var aDepth))
                {
                    continue;
                }

                return aDepth + bDepth;
            }

            return int.MaxValue;
        }

        private static void Select(EventSystem eventSystem, FocusTarget target)
        {
            if (eventSystem.currentSelectedGameObject != target.gameObject)
            {
                eventSystem.SetSelectedGameObject(target.gameObject);
            }
        }

        private void SetNavigationEnabled(bool enabled)
        {
            var targets = GetComponentsInChildren<FocusTarget>(true);

            for (var i = 0; i < targets.Length; i++)
            {
                targets[i].SetNavigationEnabled(enabled);
            }
        }
    }
}
