using UnityEngine;
using UnityEngine.UIElements;

namespace Fang.Framework.UI.Async
{
    internal static class UIVisualTreeBinder
    {
        private const string LogTag = "[Fang.Framework.UI.Async]";

        public static VisualElement Bind<TPanel, TConfig, TData>(
            TPanel panel,
            UIVisualTreePanelConfigDataSo config,
            int sortOrder)
            where TPanel : UIVisualTreePanelController<TConfig, TData>
            where TConfig : UIPanelConfigDataSo
            where TData : UIPanelData<TConfig>
        {
            if (config.VisualTreeAsset == null)
            {
                Debug.LogError($"{LogTag} UI Toolkit panel '{config.Id}' has no visual tree asset assigned.");
                return null;
            }

            if (config.PanelSettings == null)
            {
                Debug.LogError($"{LogTag} UI Toolkit panel '{config.Id}' has no panel settings assigned.");
                return null;
            }

            var document = panel.gameObject.AddComponent<UIDocument>();
            document.panelSettings = config.PanelSettings;
            document.sortingOrder = sortOrder;
            document.visualTreeAsset = config.VisualTreeAsset;

            var root = document.rootVisualElement;

            if (root == null)
            {
                Debug.LogError($"{LogTag} UI Toolkit panel '{config.Id}' has no root visual element; the panel host must stay active for UIDocument to build.");
                return null;
            }

            var styleSheets = config.StyleSheets;

            for (var i = 0; i < styleSheets.Count; i++)
            {
                var styleSheet = styleSheets[i];

                if (styleSheet != null)
                {
                    root.styleSheets.Add(styleSheet);
                }
            }

            return root;
        }
    }
}
