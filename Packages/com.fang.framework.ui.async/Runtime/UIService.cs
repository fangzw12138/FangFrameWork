using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Fang.Framework.Async;
using UnityEngine;
using UnityEngine.UI;

namespace Fang.Framework.UI.Async
{
    public sealed class UIService : AsyncService
    {
        private const string LogTag = "[Fang.Framework.UI.Async]";
        private const string RootName = "UI";
        private const string UguiRootName = "UGUI";
        private const string VisualTreeRootName = "UITK";

        private readonly Dictionary<string, UIPanelConfigDataSo> _registeredConfigs = new Dictionary<string, UIPanelConfigDataSo>();
        private readonly Dictionary<string, IUIPanel> _openedPanels = new Dictionary<string, IUIPanel>();
        private readonly UIPanelStack _stack = new UIPanelStack();

        private UIProjectConfigDataSo _projectConfig;
        private UICanvasPool _canvasPool;
        private Transform _uguiRoot;
        private Transform _visualTreeRoot;
        private Vector2 _canvasReferenceResolution = new Vector2(1920f, 1080f);
        private CanvasScaler.ScreenMatchMode _canvasScreenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        private float _canvasMatchWidthOrHeight = 1f;
        private UICanvasRenderMode _canvasRenderMode = UICanvasRenderMode.ScreenSpaceCamera;
        private string _canvasWorldCameraName;
        private float _canvasPlaneDistance = 1f;

        public event Action InputBlockingPanelOpened;

        public event Action InputBlockingPanelClosed;

        public bool HasInputBlockingPanel => _stack.HasInputBlocking;

        public void Configure(UIProjectConfigDataSo projectConfig)
        {
            if (projectConfig == null)
            {
                Debug.LogError($"{LogTag} UIService.Configure received a null project config; the service stays unconfigured.");

                _projectConfig = null;
                _registeredConfigs.Clear();
                return;
            }

            if (!TryValidateLayers(projectConfig) || !TryValidatePanels(projectConfig))
            {
                _projectConfig = null;
                _registeredConfigs.Clear();
                return;
            }

            _projectConfig = projectConfig;
            _registeredConfigs.Clear();

            _canvasReferenceResolution = projectConfig.ReferenceResolution;
            _canvasScreenMatchMode = projectConfig.ScreenMatchMode;
            _canvasMatchWidthOrHeight = projectConfig.MatchWidthOrHeight;
            _canvasRenderMode = projectConfig.CanvasRenderMode;
            _canvasWorldCameraName = projectConfig.CanvasWorldCameraName;
            _canvasPlaneDistance = projectConfig.CanvasPlaneDistance;

            var panels = projectConfig.Panels;

            for (var i = 0; i < panels.Count; i++)
            {
                var panelConfig = panels[i];
                _registeredConfigs[panelConfig.Id] = panelConfig;
            }
        }

        public async UniTask<TPanel> OpenAsync<TPanel, TConfig, TData>(string configId)
            where TPanel : UIPanelController<TConfig, TData>
            where TConfig : UIPanelConfigDataSo
            where TData : UIPanelData<TConfig>
        {
            var config = ResolveConfig<TConfig>(configId);

            if (config == null)
            {
                return null;
            }

            return await OpenAsync<TPanel, TConfig, TData>(config);
        }

        public async UniTask<TPanel> OpenAsync<TPanel, TConfig, TData>(TConfig config)
            where TPanel : UIPanelController<TConfig, TData>
            where TConfig : UIPanelConfigDataSo
            where TData : UIPanelData<TConfig>
        {
            if (_projectConfig == null)
            {
                Debug.LogError($"{LogTag} UIService is not configured; call Configure(UIProjectConfigDataSo) before opening any panel.");
                return null;
            }

            if (_canvasPool == null || _visualTreeRoot == null)
            {
                Debug.LogError($"{LogTag} UIService is not initialized yet; open panels only after the service finished initializing.");
                return null;
            }

            if (config == null)
            {
                Debug.LogError($"{LogTag} UIService.OpenAsync received a null panel config.");
                return null;
            }

            if (string.IsNullOrEmpty(config.Id))
            {
                Debug.LogError($"{LogTag} Panel config of type '{config.GetType().FullName}' has no id.");
                return null;
            }

            if (!_registeredConfigs.ContainsKey(config.Id))
            {
                Debug.LogError($"{LogTag} Panel config '{config.Id}' is not registered in the project config; add it to UIProjectConfigDataSo.Panels first.");
                return null;
            }

            if (_openedPanels.TryGetValue(config.Id, out var opened))
            {
                if (!UIPanelStack.IsAlive(opened))
                {
                    _openedPanels.Remove(config.Id);
                }
                else if (!(opened is TPanel reused))
                {
                    Debug.LogError($"{LogTag} Panel config '{config.Id}' is already open as '{opened.GetType().FullName}' and cannot be reopened as '{typeof(TPanel).FullName}'.");
                    return null;
                }
                else
                {
                    await _stack.PushAsync(reused, config.ClosePreviousOnOpen, config.BlocksInput);
                    return reused;
                }
            }

            var sortOrder = UILayerResolver.Resolve(_projectConfig.Layers, config);
            var parent = config is UIPrefabPanelConfigDataSo ? _canvasPool.Get(sortOrder) : _visualTreeRoot;

            var panel = UIPanelFactory.Create<TPanel, TConfig, TData>(config, parent, sortOrder);

            if (panel == null)
            {
                return null;
            }

            var data = panel.CreateData(config);

            if (data == null)
            {
                Debug.LogError($"{LogTag} Panel '{config.Id}' returned a null data instance from CreateData.");
                return null;
            }

            data.LayerId = config.LayerId;
            data.SortOrder = sortOrder;

            await panel.InitAsync(data);

            _openedPanels[config.Id] = panel;

            await _stack.PushAsync(panel, config.ClosePreviousOnOpen, config.BlocksInput);
            return panel;
        }

        public async UniTask<bool> CloseAsync(string configId)
        {
            var panel = _stack.Find(configId);
            return panel != null && await _stack.PopAsync(panel);
        }

        public UniTask<bool> CloseTopAsync()
        {
            return _stack.PopTopAsync();
        }

        public UniTask CloseAllAsync()
        {
            return _stack.PopAllAsync();
        }

        public bool IsOpen(string configId)
        {
            return _stack.Contains(configId);
        }

        public IUIPanel GetOpened(string configId)
        {
            return _stack.Find(configId);
        }

        protected override UniTask OnInitAsync()
        {
            var root = new GameObject(RootName, typeof(RectTransform));
            root.transform.SetParent(transform, false);

            var ugui = new GameObject(UguiRootName, typeof(RectTransform));
            ugui.transform.SetParent(root.transform, false);
            _uguiRoot = ugui.transform;

            var visualTree = new GameObject(VisualTreeRootName, typeof(RectTransform));
            visualTree.transform.SetParent(root.transform, false);
            _visualTreeRoot = visualTree.transform;

            _canvasPool = new UICanvasPool(_uguiRoot, GetCanvasScalerSettings, GetCanvasRenderSettings);

            _stack.InputBlockingPanelOpened += OnInputBlockingPanelOpened;
            _stack.InputBlockingPanelClosed += OnInputBlockingPanelClosed;

            return UniTask.CompletedTask;
        }

        protected override async UniTask OnDisposeAsync()
        {
            _stack.InputBlockingPanelOpened -= OnInputBlockingPanelOpened;
            _stack.InputBlockingPanelClosed -= OnInputBlockingPanelClosed;

            await SafeAsync(_stack.PopAllAsync(), "close all opened panels");

            var panels = new List<IUIPanel>(_openedPanels.Values);

            for (var i = 0; i < panels.Count; i++)
            {
                var panel = panels[i];

                if (panel != null)
                {
                    await SafeAsync(panel.DisposeAsync(), $"dispose panel '{Describe(panel)}'");
                }
            }

            _openedPanels.Clear();
            _registeredConfigs.Clear();

            _projectConfig = null;
            _canvasPool = null;
            _uguiRoot = null;
            _visualTreeRoot = null;
        }

        private static async UniTask SafeAsync(UniTask task, string what)
        {
            try
            {
                await task;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"{LogTag} Shutdown step failed: {what} ({e.GetType().Name}: {e.Message}); continuing shutdown.");
            }
        }

        private static string Describe(IUIPanel panel)
        {
            if (!UIPanelStack.IsAlive(panel))
            {
                return "(destroyed)";
            }

            var id = panel.ConfigId;
            return string.IsNullOrEmpty(id) ? panel.GetType().Name : id;
        }

        private CanvasScalerSettings GetCanvasScalerSettings()
        {
            return new CanvasScalerSettings(
                _canvasReferenceResolution,
                _canvasScreenMatchMode,
                _canvasMatchWidthOrHeight);
        }

        private CanvasRenderSettings GetCanvasRenderSettings()
        {
            return new CanvasRenderSettings(_canvasRenderMode, _canvasWorldCameraName, _canvasPlaneDistance);
        }

        private bool TryValidateLayers(UIProjectConfigDataSo projectConfig)
        {
            var layers = projectConfig.Layers;

            if (layers == null)
            {
                Debug.LogError($"{LogTag} Project config '{projectConfig.name}' has a null layer list.");
                return false;
            }

            var seen = new HashSet<string>();

            for (var i = 0; i < layers.Count; i++)
            {
                var layer = layers[i];

                if (layer == null)
                {
                    Debug.LogError($"{LogTag} Project config '{projectConfig.name}' has a null layer entry at index {i}.");
                    return false;
                }

                if (string.IsNullOrEmpty(layer.Id))
                {
                    Debug.LogError($"{LogTag} Layer entry at index {i} has no id.");
                    return false;
                }

                if (!seen.Add(layer.Id))
                {
                    Debug.LogError($"{LogTag} Project config '{projectConfig.name}' has a duplicate layer id '{layer.Id}'.");
                    return false;
                }
            }

            return true;
        }

        private bool TryValidatePanels(UIProjectConfigDataSo projectConfig)
        {
            var panels = projectConfig.Panels;

            if (panels == null)
            {
                Debug.LogError($"{LogTag} Project config '{projectConfig.name}' has a null panel list.");
                return false;
            }

            var seen = new HashSet<string>();

            for (var i = 0; i < panels.Count; i++)
            {
                var panel = panels[i];

                if (panel == null)
                {
                    Debug.LogError($"{LogTag} Project config '{projectConfig.name}' has a null panel entry at index {i}.");
                    return false;
                }

                if (string.IsNullOrEmpty(panel.Id))
                {
                    Debug.LogError($"{LogTag} Panel entry at index {i} has no id.");
                    return false;
                }

                if (!seen.Add(panel.Id))
                {
                    Debug.LogError($"{LogTag} Project config '{projectConfig.name}' has a duplicate panel id '{panel.Id}'.");
                    return false;
                }
            }

            return true;
        }

        private TConfig ResolveConfig<TConfig>(string configId) where TConfig : UIPanelConfigDataSo
        {
            if (_projectConfig == null)
            {
                Debug.LogError($"{LogTag} UIService is not configured; call Configure(UIProjectConfigDataSo) before opening any panel.");
                return null;
            }

            if (string.IsNullOrEmpty(configId))
            {
                Debug.LogError($"{LogTag} UIService.OpenAsync received an empty panel config id.");
                return null;
            }

            if (_registeredConfigs.TryGetValue(configId, out var config) && config is TConfig typed)
            {
                return typed;
            }

            Debug.LogError($"{LogTag} Panel config '{configId}' is not registered as '{typeof(TConfig).FullName}' in project config '{_projectConfig.name}'.");
            return null;
        }

        private void OnInputBlockingPanelOpened()
        {
            InputBlockingPanelOpened?.Invoke();
        }

        private void OnInputBlockingPanelClosed()
        {
            InputBlockingPanelClosed?.Invoke();
        }
    }
}
