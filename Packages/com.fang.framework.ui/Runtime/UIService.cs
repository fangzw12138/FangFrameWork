using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fang.Framework.UI
{
    public sealed class UIService : Service, ITickable
    {
        private readonly Dictionary<string, IUIPanel> _panelsByPanelId = new Dictionary<string, IUIPanel>();
        private readonly Dictionary<string, IUIPanel> _panelsByConfigId = new Dictionary<string, IUIPanel>();
        private readonly HashSet<string> _registeredPanelIds = new HashSet<string>();
        private readonly UIPanelStack _stack = new UIPanelStack();
        private readonly UICanvasPool _canvasPool = new UICanvasPool();

        private UIProjectConfigDataSo _projectConfig;
        private Transform _uguiRoot;
        private Transform _visualTreeRoot;
        private Camera _followCamera;

        public event Action InputBlockingPanelOpened
        {
            add => _stack.InputBlockingPanelOpened += value;
            remove => _stack.InputBlockingPanelOpened -= value;
        }

        public event Action InputBlockingPanelClosed
        {
            add => _stack.InputBlockingPanelClosed += value;
            remove => _stack.InputBlockingPanelClosed -= value;
        }

        public bool HasInputBlockingPanel => _stack.HasInputBlocking;

        public void Configure(UIProjectConfigDataSo projectConfig)
        {
            _projectConfig = null;
            _registeredPanelIds.Clear();

            if (projectConfig == null)
            {
                Debug.LogError("[UIService] Configure failed: project config is null.");
                return;
            }

            if (!ValidateProjectConfig(projectConfig))
            {
                return;
            }

            for (var i = 0; i < projectConfig.Panels.Count; i++)
            {
                _registeredPanelIds.Add(projectConfig.Panels[i].Id);
            }

            _projectConfig = projectConfig;
            EnsureRoots();
        }

        public void SetFollowCamera(Camera camera)
        {
            _followCamera = camera;
        }

        public TPanel Open<TPanel, TConfig, TData>(TConfig config)
            where TPanel : UIPanelController<TConfig, TData>
            where TConfig : UIPanelConfigDataSo
            where TData : UIPanelData<TConfig>
        {
            return OpenPanel<TPanel, TConfig, TData>(config, true);
        }

        public TPanel OpenFollow<TPanel, TConfig, TData>(TConfig config, Transform target, Vector3 worldOffset)
            where TPanel : UIFollowPanelController<TConfig, TData>
            where TConfig : UIPanelConfigDataSo
            where TData : UIPanelData<TConfig>
        {
            var panel = OpenPanel<TPanel, TConfig, TData>(config, false);
            if (panel == null)
            {
                return null;
            }

            ((IUIFollowPanel)panel).SetTarget(target, worldOffset);
            return panel;
        }

        public bool Close(string panelId)
        {
            if (string.IsNullOrEmpty(panelId) || !_panelsByPanelId.TryGetValue(panelId, out var panel))
            {
                return false;
            }

            return _stack.Pop(panel);
        }

        public bool CloseTop()
        {
            return _stack.PopTop();
        }

        public void CloseAll()
        {
            _stack.PopAll();
        }

        public bool IsOpen(string panelId)
        {
            return !string.IsNullOrEmpty(panelId)
                && _panelsByPanelId.TryGetValue(panelId, out var panel)
                && panel.IsOpen;
        }

        public IUIPanel GetOpened(string panelId)
        {
            if (string.IsNullOrEmpty(panelId))
            {
                return null;
            }

            return _panelsByPanelId.TryGetValue(panelId, out var panel) ? panel : null;
        }

        public void OnTick(float deltaTime)
        {
            if (_followCamera == null)
            {
                return;
            }

            foreach (var panel in _panelsByPanelId.Values)
            {
                if (panel.IsVisible && panel is IUIFollowPanel follow)
                {
                    follow.UpdateScreenPosition(_followCamera);
                }
            }
        }

        public override void OnDispose()
        {
            _stack.PopAll();

            foreach (var panel in _panelsByPanelId.Values)
            {
                panel.OnDispose();
            }

            _panelsByPanelId.Clear();
            _panelsByConfigId.Clear();
            _registeredPanelIds.Clear();
            _projectConfig = null;
            _followCamera = null;
        }

        private TPanel OpenPanel<TPanel, TConfig, TData>(TConfig config, bool reuse)
            where TPanel : UIPanelController<TConfig, TData>
            where TConfig : UIPanelConfigDataSo
            where TData : UIPanelData<TConfig>
        {
            if (!ValidatePanelConfig(config))
            {
                return null;
            }

            if (reuse && _panelsByConfigId.TryGetValue(config.Id, out var opened))
            {
                if (!(opened is TPanel reused))
                {
                    Debug.LogError($"[UIService] panel '{config.Id}' is already open as '{opened.GetType().Name}', cannot open it as '{typeof(TPanel).Name}'.");
                    return null;
                }

                _stack.Push(reused, config.ClosePreviousOnOpen, config.BlocksInput);
                return reused;
            }

            var sortOrder = UILayerResolver.Resolve(_projectConfig.Layers, config.LayerId, config.SortOrder);

            var panel = UIPanelFactory.Create<TPanel, TConfig, TData>(config, sortOrder, _visualTreeRoot, _canvasPool);
            if (panel == null)
            {
                return null;
            }

            var data = panel.CreateData(config);
            if (data == null)
            {
                Debug.LogError($"[UIService] CreateData returned null for panel '{config.Id}'.");
                return null;
            }

            data.LayerId = config.LayerId;
            data.SortOrder = sortOrder;

            panel.Inject(Scope);
            panel.Initialize(data);
            panel.OnInit();

            _panelsByPanelId[panel.PanelId] = panel;
            if (reuse)
            {
                _panelsByConfigId[config.Id] = panel;
            }

            _stack.Push(panel, config.ClosePreviousOnOpen, config.BlocksInput);
            return panel;
        }

        private bool ValidatePanelConfig(UIPanelConfigDataSo config)
        {
            if (config == null)
            {
                Debug.LogError("[UIService] Open failed: panel config is null.");
                return false;
            }

            if (_projectConfig == null)
            {
                Debug.LogError("[UIService] Open failed: UIService is not configured.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(config.Id))
            {
                Debug.LogError("[UIService] Open failed: panel config has no id.");
                return false;
            }

            if (!_registeredPanelIds.Contains(config.Id))
            {
                Debug.LogError($"[UIService] Open failed: panel '{config.Id}' is not registered in the project config.");
                return false;
            }

            return true;
        }

        private static bool ValidateProjectConfig(UIProjectConfigDataSo projectConfig)
        {
            var layerIds = new HashSet<string>();

            for (var i = 0; i < projectConfig.Layers.Count; i++)
            {
                var layer = projectConfig.Layers[i];
                if (layer == null)
                {
                    Debug.LogError($"[UIService] Configure failed: layer entry {i} is null.");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(layer.Id))
                {
                    Debug.LogError($"[UIService] Configure failed: layer entry {i} has no id.");
                    return false;
                }

                if (!layerIds.Add(layer.Id))
                {
                    Debug.LogError($"[UIService] Configure failed: duplicate layer id '{layer.Id}'.");
                    return false;
                }
            }

            var panelIds = new HashSet<string>();

            for (var i = 0; i < projectConfig.Panels.Count; i++)
            {
                var panel = projectConfig.Panels[i];
                if (panel == null)
                {
                    Debug.LogError($"[UIService] Configure failed: panel entry {i} is null.");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(panel.Id))
                {
                    Debug.LogError($"[UIService] Configure failed: panel entry {i} has no id.");
                    return false;
                }

                if (!panelIds.Add(panel.Id))
                {
                    Debug.LogError($"[UIService] Configure failed: duplicate panel id '{panel.Id}'.");
                    return false;
                }

                if (!string.IsNullOrEmpty(panel.LayerId) && !layerIds.Contains(panel.LayerId))
                {
                    Debug.LogWarning($"[UIService] panel '{panel.Id}' references unknown layer '{panel.LayerId}', sort order falls back to {panel.SortOrder}.");
                }
            }

            return true;
        }

        private void EnsureRoots()
        {
            if (_uguiRoot != null && _visualTreeRoot != null)
            {
                return;
            }

            var root = new GameObject("UI");
            root.transform.SetParent(transform, false);

            var ugui = new GameObject("UGUI");
            ugui.transform.SetParent(root.transform, false);
            _uguiRoot = ugui.transform;

            var visualTree = new GameObject("UITK");
            visualTree.transform.SetParent(root.transform, false);
            _visualTreeRoot = visualTree.transform;

            _canvasPool.SetRoot(_uguiRoot);
        }
    }
}
