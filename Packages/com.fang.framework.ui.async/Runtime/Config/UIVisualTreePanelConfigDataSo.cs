using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fang.Framework.UI.Async
{
    [CreateAssetMenu(fileName = "UIVisualTreePanelConfigData", menuName = "Fang Framework/UI (Async)/UITK 面板配置")]
    public sealed class UIVisualTreePanelConfigDataSo : UIPanelConfigDataSo
    {
        [SerializeField] private VisualTreeAsset _visualTreeAsset;
        [SerializeField] private PanelSettings _panelSettings;
        [SerializeField] private List<StyleSheet> _styleSheets = new List<StyleSheet>();

        public VisualTreeAsset VisualTreeAsset => _visualTreeAsset;

        public PanelSettings PanelSettings => _panelSettings;

        public IReadOnlyList<StyleSheet> StyleSheets => _styleSheets;
    }
}
