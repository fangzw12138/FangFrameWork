using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Fang.Framework.UI.Async
{
    [DisallowMultipleComponent]
    public sealed class FocusTarget : Selectable, IPointerClickHandler, ISubmitHandler
    {
        internal static event Action<FocusTarget> AnySelected;

        private Navigation _navigationWhenDisabled;
        private bool _navigationEnabled = true;

        public event Action<FocusTarget> Selected;

        public event Action<FocusTarget> Deselected;

        public event Action<FocusTarget> Activated;

        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);
            Selected?.Invoke(this);
            AnySelected?.Invoke(this);
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            base.OnDeselect(eventData);
            Deselected?.Invoke(this);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            if (!IsInteractable())
            {
                return;
            }

            Activated?.Invoke(this);
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (!IsInteractable())
            {
                return;
            }

            Activated?.Invoke(this);
        }

        internal void SetNavigationEnabled(bool enabled)
        {
            if (_navigationEnabled == enabled)
            {
                return;
            }

            if (enabled)
            {
                navigation = _navigationWhenDisabled;
            }
            else
            {
                _navigationWhenDisabled = navigation;
                var value = navigation;
                value.mode = Navigation.Mode.None;
                navigation = value;
            }

            _navigationEnabled = enabled;
        }
    }
}
