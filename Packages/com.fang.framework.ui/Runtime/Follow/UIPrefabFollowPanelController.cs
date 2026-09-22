using UnityEngine;

namespace Fang.Framework.UI
{
    public abstract class UIPrefabFollowPanelController<TConfig, TData> : UIFollowPanelController<TConfig, TData>
        where TConfig : UIPrefabPanelConfigDataSo
        where TData : UIPanelData<TConfig>
    {
        protected override void ApplyScreenPosition(Vector2 screenPosition)
        {
            // UGUI 面板挂在 ScreenSpaceOverlay 画布下，屏幕像素坐标即世界坐标
            transform.position = new Vector3(screenPosition.x, screenPosition.y, transform.position.z);
        }
    }
}
