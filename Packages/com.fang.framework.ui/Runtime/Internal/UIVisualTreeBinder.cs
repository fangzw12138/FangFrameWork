using UnityEngine;
using UnityEngine.UIElements;

namespace Fang.Framework.UI
{
    internal static class UIVisualTreeBinder
    {
        public static VisualElement Build(Component host, UIVisualTreePanelConfigDataSo config, int sortOrder)
        {
            if (config.VisualTreeAsset == null)
            {
                Debug.LogError($"[UIService] visual tree asset is null for panel '{config.Id}'.");
                return null;
            }

            if (config.PanelSettings == null)
            {
                Debug.LogError($"[UIService] panel settings is null for panel '{config.Id}'.");
                return null;
            }

            var document = host.gameObject.AddComponent<UIDocument>();
            document.panelSettings = config.PanelSettings;
            document.sortingOrder = sortOrder;
            document.visualTreeAsset = config.VisualTreeAsset;

            var root = document.rootVisualElement;
            if (root == null)
            {
                Debug.LogError($"[UIService] root visual element is null for panel '{config.Id}'.");
                return null;
            }

            for (var i = 0; i < config.StyleSheets.Count; i++)
            {
                var styleSheet = config.StyleSheets[i];
                if (styleSheet == null || root.styleSheets.Contains(styleSheet))
                {
                    continue;
                }

                root.styleSheets.Add(styleSheet);
            }

            return root;
        }

        public static void ApplyScreenPosition(VisualElement root, Vector2 screenPosition)
        {
            if (root == null || root.panel == null)
            {
                return;
            }

            // ScreenToPanel 只做等比缩放、不做 y 轴翻转，翻转在这里补
            var panelPosition = RuntimePanelUtils.ScreenToPanel(root.panel, screenPosition);
            var panelHeight = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(0f, Screen.height)).y;

            root.style.left = panelPosition.x;
            root.style.top = panelHeight - panelPosition.y;
        }
    }
}
