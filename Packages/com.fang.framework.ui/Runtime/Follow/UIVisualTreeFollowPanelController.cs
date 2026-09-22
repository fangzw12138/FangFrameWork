using UnityEngine;
using UnityEngine.UIElements;

namespace Fang.Framework.UI
{
    public abstract class UIVisualTreeFollowPanelController<TConfig, TData> : UIFollowPanelController<TConfig, TData>
        where TConfig : UIVisualTreePanelConfigDataSo
        where TData : UIPanelData<TConfig>
    {
        protected VisualElement Root { get; private set; }

        public override void OnInit()
        {
            Root = UIVisualTreeBinder.Build(this, Data.Config, Data.SortOrder);
            if (Root != null)
            {
                Root.style.position = Position.Absolute;
                Root.style.display = DisplayStyle.None;
            }
        }

        public override void OnDispose()
        {
            Root = null;
        }

        protected override void SetVisible(bool visible)
        {
            if (Root == null)
            {
                return;
            }

            Root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        protected override void ApplyScreenPosition(Vector2 screenPosition)
        {
            UIVisualTreeBinder.ApplyScreenPosition(Root, screenPosition);
        }
    }
}
