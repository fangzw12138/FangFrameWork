using UnityEngine.UIElements;

namespace Fang.Framework.UI.Async
{
    public abstract class UIVisualTreePanelController<TConfig, TData> : UIPanelController<TConfig, TData>
        where TConfig : UIPanelConfigDataSo
        where TData : UIPanelData<TConfig>
    {
        protected VisualElement Root { get; private set; }

        internal void SetRoot(VisualElement root)
        {
            Root = root;
            SetVisibleCore(false);
        }

        protected override void SetVisibleCore(bool visible)
        {
            if (Root == null)
            {
                return;
            }

            Root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
