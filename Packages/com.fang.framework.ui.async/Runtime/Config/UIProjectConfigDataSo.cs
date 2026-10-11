using System.Collections.Generic;
using Fang.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Fang.Framework.UI.Async
{
    [CreateAssetMenu(fileName = "UIProjectConfigData", menuName = "Fang Framework/UI (Async)/工程配置")]
    public sealed class UIProjectConfigDataSo : ConfigDataSo
    {
        [SerializeField] private string _rootNamespace;
        [SerializeField] private string _layerConfigFolder;
        [SerializeField] private string _panelConfigFolder;
        [SerializeField] private string _panelPrefabFolder;
        [SerializeField] private string _panelScriptFolder;
        [SerializeField] private string _panelVisualTreeFolder;

        [Tooltip("所有 UGUI 画布的参考分辨率。")]
        [SerializeField] private Vector2 _referenceResolution = new Vector2(1920f, 1080f);

        [Tooltip("画布缩放匹配模式。Expand = 宽高都装得下（默认，保持既有行为）；MatchWidthOrHeight = 按下列权重插值，权重 1 即完全按高度适配屏幕。")]
        [SerializeField] private CanvasScaler.ScreenMatchMode _screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        [Tooltip("仅 MatchWidthOrHeight 生效：0 = 按宽度适配，1 = 按高度适配屏幕。")]
        [Range(0f, 1f)]
        [SerializeField] private float _matchWidthOrHeight = 1f;

        [Header("画布渲染模式")]
        [Tooltip("UGUI 画布由谁渲染。Overlay = 直接铺屏（不受相机与后处理影响）；ScreenSpaceCamera = 交给相机渲染（会进相机的颜色目标，因此会被该相机的后处理一起处理，也会被比画布平面更近的场景物体遮挡）。")]
        [SerializeField] private UICanvasRenderMode _canvasRenderMode = UICanvasRenderMode.ScreenSpaceCamera;

        [Tooltip("仅 ScreenSpaceCamera 生效：用哪台相机渲染（按名字找）。留空 = Camera.main。")]
        [SerializeField] private string _canvasWorldCameraName;

        [Tooltip("仅 ScreenSpaceCamera 生效：画布平面到相机的距离。必须小于“最近场景物体到相机的距离”，否则 UI 会被场景物体遮挡；也必须在相机裁剪面之内（越界会被自动夹到裁剪面内侧）。")]
        [SerializeField] private float _canvasPlaneDistance = 1f;

        [SerializeField] private List<UILayerConfigDataSo> _layers = new List<UILayerConfigDataSo>();
        [SerializeField] private List<UIPanelConfigDataSo> _panels = new List<UIPanelConfigDataSo>();

        public string RootNamespace => _rootNamespace;

        public string LayerConfigFolder => _layerConfigFolder;

        public string PanelConfigFolder => _panelConfigFolder;

        public string PanelPrefabFolder => _panelPrefabFolder;

        public string PanelScriptFolder => _panelScriptFolder;

        public string PanelVisualTreeFolder => _panelVisualTreeFolder;

        public Vector2 ReferenceResolution => _referenceResolution;

        public CanvasScaler.ScreenMatchMode ScreenMatchMode => _screenMatchMode;

        public float MatchWidthOrHeight => _matchWidthOrHeight;

        public UICanvasRenderMode CanvasRenderMode => _canvasRenderMode;

        public string CanvasWorldCameraName => _canvasWorldCameraName;

        public float CanvasPlaneDistance => _canvasPlaneDistance;

        public IReadOnlyList<UILayerConfigDataSo> Layers => _layers;

        public IReadOnlyList<UIPanelConfigDataSo> Panels => _panels;
    }
}
