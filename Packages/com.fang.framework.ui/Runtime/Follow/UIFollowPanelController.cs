using UnityEngine;

namespace Fang.Framework.UI
{
    public abstract class UIFollowPanelController<TConfig, TData> : UIPanelController<TConfig, TData>, IUIFollowPanel
        where TConfig : UIPanelConfigDataSo
        where TData : UIPanelData<TConfig>
    {
        private Transform _target;
        private Vector3 _worldOffset;

        public void SetTarget(Transform target, Vector3 worldOffset)
        {
            _target = target;
            _worldOffset = worldOffset;
        }

        void IUIFollowPanel.UpdateScreenPosition(Camera camera)
        {
            if (_target == null || camera == null)
            {
                return;
            }

            var worldPosition = _target.position + _worldOffset;
            var screenPosition = camera.WorldToScreenPoint(worldPosition);
            if (screenPosition.z < 0f)
            {
                return;
            }

            ApplyScreenPosition(screenPosition);
        }

        protected abstract void ApplyScreenPosition(Vector2 screenPosition);
    }
}
