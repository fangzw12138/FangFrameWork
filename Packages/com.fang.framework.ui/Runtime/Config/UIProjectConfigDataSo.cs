using System.Collections.Generic;
using UnityEngine;

namespace Fang.Framework.UI
{
    [CreateAssetMenu(fileName = "UIProjectConfigData", menuName = "Fang Framework/UI/项目配置")]
    public sealed class UIProjectConfigDataSo : ConfigDataSo
    {
        [SerializeField] private string _rootNamespace;
        [SerializeField] private string _panelConfigFolder;
        [SerializeField] private string _panelPrefabFolder;
        [SerializeField] private string _panelScriptFolder;
        [SerializeField] private string _panelVisualTreeFolder;
        [SerializeField] private string _layerConfigFolder;
        [SerializeField] private List<UILayerConfigDataSo> _layers = new List<UILayerConfigDataSo>();
        [SerializeField] private List<UIPanelConfigDataSo> _panels = new List<UIPanelConfigDataSo>();

        public string RootNamespace => _rootNamespace;

        public string PanelConfigFolder => _panelConfigFolder;

        public string PanelPrefabFolder => _panelPrefabFolder;

        public string PanelScriptFolder => _panelScriptFolder;

        public string PanelVisualTreeFolder => _panelVisualTreeFolder;

        public string LayerConfigFolder => _layerConfigFolder;

        public IReadOnlyList<UILayerConfigDataSo> Layers => _layers;

        public IReadOnlyList<UIPanelConfigDataSo> Panels => _panels;
    }
}
