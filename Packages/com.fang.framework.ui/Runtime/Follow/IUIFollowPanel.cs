using UnityEngine;

namespace Fang.Framework.UI
{
    public interface IUIFollowPanel
    {
        void SetTarget(Transform target, Vector3 worldOffset);

        void UpdateScreenPosition(Camera camera);
    }
}
