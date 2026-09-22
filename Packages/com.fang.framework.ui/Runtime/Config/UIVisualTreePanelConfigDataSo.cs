using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fang.Framework.UI
{
    [CreateAssetMenu(fileName = "UIVisualTreePanelConfigData", menuName = "Fang Framework/UI/UITK 面板配置")]
    public class UIVisualTreePanelConfigDataSo : UIPanelConfigDataSo
    {
        [SerializeField] private VisualTreeAsset _visualTreeAsset;
        [SerializeField] private PanelSettings _panelSettings;
        [SerializeField] private List<StyleSheet> _styleSheets = new List<StyleSheet>();

        public VisualTreeAsset VisualTreeAsset => _visualTreeAsset;
        public PanelSettings PanelSettings => _panelSettings;
        public IReadOnlyList<StyleSheet> StyleSheets => _styleSheets;
    }
}
