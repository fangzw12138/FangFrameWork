using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Fang.Framework.UI.Tests
{
    public class UIServiceTests
    {
        private readonly List<Object> _assets = new List<Object>();
        private readonly List<GameObject> _objects = new List<GameObject>();
        private UiTestScope _scope;
        private UIService _service;

        [TearDown]
        public void TearDown()
        {
            if (_scope != null)
            {
                _scope.OnDispose();
                Object.DestroyImmediate(_scope.gameObject);
                _scope = null;
            }

            for (var i = 0; i < _objects.Count; i++)
            {
                if (_objects[i] != null)
                {
                    Object.DestroyImmediate(_objects[i]);
                }
            }

            _objects.Clear();

            for (var i = 0; i < _assets.Count; i++)
            {
                if (_assets[i] != null)
                {
                    Object.DestroyImmediate(_assets[i]);
                }
            }

            _assets.Clear();

            _service = null;
        }

        [Test]
        public void Open_creates_the_prefab_panel_under_a_canvas_with_the_resolved_sort_order()
        {
            var panelConfig = CreatePrefabPanelConfig("hud", "hud-layer", 5, false, false);
            SetUpService(new[] { CreateLayer("hud-layer", 100) }, new UIPanelConfigDataSo[] { panelConfig });

            var panel = _service.Open<TestPrefabPanel, UIPrefabPanelConfigDataSo, TestPanelData>(panelConfig);

            Assert.IsNotNull(panel);
            Assert.AreEqual("hud", panel.gameObject.name);
            Assert.IsTrue(panel.IsOpen);
            Assert.IsTrue(panel.IsVisible);
            Assert.AreEqual("hud-layer", panel.Data.LayerId);
            Assert.AreEqual(105, panel.Data.SortOrder);
            Assert.AreEqual(1, panel.OpenCount);

            var canvas = panel.transform.parent.GetComponent<Canvas>();
            Assert.IsNotNull(canvas);
            Assert.AreEqual(105, canvas.sortingOrder);
        }

        [Test]
        public void Configure_ignores_empty_project_folders()
        {
            // 目录字段只归编辑器；运行期 Configure 不读它们。
            var panelConfig = CreatePrefabPanelConfig("hud", "hud-layer", 0, false, false);
            SetUpService(new[] { CreateLayer("hud-layer", 100) }, new UIPanelConfigDataSo[] { panelConfig });

            var panel = _service.Open<TestPrefabPanel, UIPrefabPanelConfigDataSo, TestPanelData>(panelConfig);

            Assert.IsNotNull(panel);
        }

        [Test]
        public void Open_reuses_the_instance_and_does_not_open_it_twice()
        {
            var panelConfig = CreatePrefabPanelConfig("hud", "hud-layer", 0, false, false);
            SetUpService(new[] { CreateLayer("hud-layer", 100) }, new UIPanelConfigDataSo[] { panelConfig });

            var first = _service.Open<TestPrefabPanel, UIPrefabPanelConfigDataSo, TestPanelData>(panelConfig);
            var second = _service.Open<TestPrefabPanel, UIPrefabPanelConfigDataSo, TestPanelData>(panelConfig);

            Assert.AreSame(first, second);
            Assert.AreEqual(1, first.OpenCount);
        }

        [Test]
        public void Close_then_open_reuses_the_instance_and_opens_it_again()
        {
            var panelConfig = CreatePrefabPanelConfig("hud", "hud-layer", 0, false, false);
            SetUpService(new[] { CreateLayer("hud-layer", 100) }, new UIPanelConfigDataSo[] { panelConfig });

            var panel = _service.Open<TestPrefabPanel, UIPrefabPanelConfigDataSo, TestPanelData>(panelConfig);
            var panelId = panel.PanelId;

            Assert.IsTrue(_service.Close(panelId));
            Assert.IsFalse(panel.IsOpen);
            Assert.IsFalse(_service.IsOpen(panelId));
            Assert.AreEqual(1, panel.CloseCount);

            var reopened = _service.Open<TestPrefabPanel, UIPrefabPanelConfigDataSo, TestPanelData>(panelConfig);

            Assert.AreSame(panel, reopened);
            Assert.AreEqual(2, panel.OpenCount);
            Assert.IsTrue(_service.IsOpen(panelId));
            Assert.AreSame(panel, _service.GetOpened(panelId));
        }

        [Test]
        public void Open_with_close_previous_on_open_closes_the_previous_panel_and_restores_it()
        {
            var first = CreatePrefabPanelConfig("first", "hud-layer", 0, false, false);
            var second = CreatePrefabPanelConfig("second", "hud-layer", 1, true, false);
            SetUpService(new[] { CreateLayer("hud-layer", 100) }, new UIPanelConfigDataSo[] { first, second });

            var firstPanel = _service.Open<TestPrefabPanel, UIPrefabPanelConfigDataSo, TestPanelData>(first);
            var secondPanel = _service.Open<TestPrefabPanel, UIPrefabPanelConfigDataSo, TestPanelData>(second);

            Assert.IsFalse(firstPanel.IsOpen);
            Assert.IsFalse(firstPanel.IsVisible);
            Assert.AreEqual(1, firstPanel.CloseCount);

            Assert.IsTrue(_service.Close(secondPanel.PanelId));

            Assert.IsTrue(firstPanel.IsOpen);
            Assert.IsTrue(firstPanel.IsVisible);
            Assert.AreEqual(2, firstPanel.OpenCount);
        }

        [Test]
        public void CloseTop_closes_the_last_opened_panel()
        {
            var first = CreatePrefabPanelConfig("first", "hud-layer", 0, false, false);
            var second = CreatePrefabPanelConfig("second", "hud-layer", 1, false, false);
            SetUpService(new[] { CreateLayer("hud-layer", 100) }, new UIPanelConfigDataSo[] { first, second });

            var firstPanel = _service.Open<TestPrefabPanel, UIPrefabPanelConfigDataSo, TestPanelData>(first);
            var secondPanel = _service.Open<TestPrefabPanel, UIPrefabPanelConfigDataSo, TestPanelData>(second);

            Assert.IsTrue(_service.CloseTop());

            Assert.IsFalse(secondPanel.IsOpen);
            Assert.IsTrue(firstPanel.IsOpen);
        }

        [Test]
        public void CloseAll_closes_every_open_panel()
        {
            var first = CreatePrefabPanelConfig("first", "hud-layer", 0, false, false);
            var second = CreatePrefabPanelConfig("second", "hud-layer", 1, false, false);
            SetUpService(new[] { CreateLayer("hud-layer", 100) }, new UIPanelConfigDataSo[] { first, second });

            var firstPanel = _service.Open<TestPrefabPanel, UIPrefabPanelConfigDataSo, TestPanelData>(first);
            var secondPanel = _service.Open<TestPrefabPanel, UIPrefabPanelConfigDataSo, TestPanelData>(second);

            _service.CloseAll();

            Assert.IsFalse(firstPanel.IsOpen);
            Assert.IsFalse(secondPanel.IsOpen);
            Assert.IsFalse(_service.IsOpen(firstPanel.PanelId));
        }

        [Test]
        public void Open_falls_back_to_the_panel_sort_order_when_the_layer_is_unknown()
        {
            var panelConfig = CreatePrefabPanelConfig("hud", "missing-layer", 7, false, false);
            LogAssert.Expect(LogType.Warning, new Regex("references unknown layer 'missing-layer'"));
            SetUpService(new UILayerConfigDataSo[0], new UIPanelConfigDataSo[] { panelConfig });

            var panel = _service.Open<TestPrefabPanel, UIPrefabPanelConfigDataSo, TestPanelData>(panelConfig);

            Assert.IsNotNull(panel);
            Assert.AreEqual(7, panel.Data.SortOrder);
        }

        [Test]
        public void Open_rejects_a_panel_that_is_not_registered_in_the_project_config()
        {
            var registered = CreatePrefabPanelConfig("hud", "hud-layer", 0, false, false);
            var unregistered = CreatePrefabPanelConfig("other", "hud-layer", 0, false, false);
            SetUpService(new[] { CreateLayer("hud-layer", 100) }, new UIPanelConfigDataSo[] { registered });

            LogAssert.Expect(LogType.Error, new Regex("is not registered in the project config"));

            var panel = _service.Open<TestPrefabPanel, UIPrefabPanelConfigDataSo, TestPanelData>(unregistered);

            Assert.IsNull(panel);
        }

        [Test]
        public void Configure_rejects_duplicate_panel_ids()
        {
            var first = CreatePrefabPanelConfig("hud", "hud-layer", 0, false, false);
            var second = CreatePrefabPanelConfig("hud", "hud-layer", 0, false, false);
            var projectConfig = CreateProjectConfig(
                new[] { CreateLayer("hud-layer", 100) },
                new UIPanelConfigDataSo[] { first, second });

            LogAssert.Expect(LogType.Error, new Regex("duplicate panel id 'hud'"));

            var service = CreateService();
            service.Configure(projectConfig);

            LogAssert.Expect(LogType.Error, new Regex("UIService is not configured"));
            Assert.IsNull(service.Open<TestPrefabPanel, UIPrefabPanelConfigDataSo, TestPanelData>(first));
        }

        [Test]
        public void Open_forwards_input_blocking_events()
        {
            var panelConfig = CreatePrefabPanelConfig("modal", "hud-layer", 0, false, true);
            SetUpService(new[] { CreateLayer("hud-layer", 100) }, new UIPanelConfigDataSo[] { panelConfig });

            var opened = 0;
            var closed = 0;
            _service.InputBlockingPanelOpened += () => opened++;
            _service.InputBlockingPanelClosed += () => closed++;

            var panel = _service.Open<TestPrefabPanel, UIPrefabPanelConfigDataSo, TestPanelData>(panelConfig);

            Assert.AreEqual(1, opened);
            Assert.IsTrue(_service.HasInputBlockingPanel);

            _service.Close(panel.PanelId);

            Assert.AreEqual(1, closed);
            Assert.IsFalse(_service.HasInputBlockingPanel);
        }

        [Test]
        public void Open_creates_the_visual_tree_panel_with_a_ui_document()
        {
            var panelConfig = CreateVisualTreePanelConfig("settings", "hud-layer", 3, false, false);
            SetUpService(new[] { CreateLayer("hud-layer", 100) }, new UIPanelConfigDataSo[] { panelConfig });

            var panel = _service.Open<TestVisualTreePanel, UIVisualTreePanelConfigDataSo, TestVisualTreePanelData>(panelConfig);

            Assert.IsNotNull(panel);
            Assert.IsNotNull(panel.gameObject.GetComponent<UIDocument>());
            Assert.IsNotNull(panel.ExposedRoot);
            Assert.AreEqual(103, panel.Data.SortOrder);
            Assert.IsTrue(panel.IsVisible);
            Assert.AreEqual(DisplayStyle.Flex, panel.ExposedRoot.style.display.value);
        }

        [Test]
        public void OpenFollow_updates_the_panel_position_on_tick()
        {
            var panelConfig = CreatePrefabPanelConfig("marker", "hud-layer", 0, false, false);
            SetUpService(new[] { CreateLayer("hud-layer", 100) }, new UIPanelConfigDataSo[] { panelConfig });

            var cameraObject = new GameObject("TestCamera");
            _objects.Add(cameraObject);
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -10f);

            var targetObject = new GameObject("Target");
            _objects.Add(targetObject);
            var target = targetObject.transform;
            target.position = new Vector3(1f, 2f, 0f);

            _service.SetFollowCamera(camera);

            var offset = new Vector3(0f, 0.5f, 0f);
            var panel = _service.OpenFollow<TestFollowPanel, UIPrefabPanelConfigDataSo, TestPanelData>(
                panelConfig,
                target,
                offset);

            Assert.IsNotNull(panel);

            _service.OnTick(0.016f);

            var expected = camera.WorldToScreenPoint(target.position + offset);

            Assert.AreEqual(expected.x, panel.transform.position.x, 0.5f);
            Assert.AreEqual(expected.y, panel.transform.position.y, 0.5f);
        }

        private void SetUpService(IReadOnlyList<UILayerConfigDataSo> layers, IReadOnlyList<UIPanelConfigDataSo> panels)
        {
            var service = CreateService();
            service.Configure(CreateProjectConfig(layers, panels));
            _service = service;
        }

        private UIService CreateService()
        {
            var host = new GameObject("UiTestScope");
            _scope = host.AddComponent<UiTestScope>();
            _scope.OnInit();
            return _scope.Add<UIService>();
        }

        private UILayerConfigDataSo CreateLayer(string id, int baseSortOrder)
        {
            var layer = ScriptableObject.CreateInstance<UILayerConfigDataSo>();
            var serialized = new SerializedObject(layer);
            serialized.FindProperty("_id").stringValue = id;
            serialized.FindProperty("_baseSortOrder").intValue = baseSortOrder;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            _assets.Add(layer);
            return layer;
        }

        private UIPrefabPanelConfigDataSo CreatePrefabPanelConfig(
            string id,
            string layerId,
            int sortOrder,
            bool closePreviousOnOpen,
            bool blocksInput)
        {
            var prefab = new GameObject(id + "-prefab");
            prefab.AddComponent<TestPrefabPanel>();
            prefab.AddComponent<TestFollowPanel>();
            _objects.Add(prefab);

            var config = ScriptableObject.CreateInstance<UIPrefabPanelConfigDataSo>();
            var serialized = new SerializedObject(config);
            serialized.FindProperty("_id").stringValue = id;
            serialized.FindProperty("_layerId").stringValue = layerId;
            serialized.FindProperty("_sortOrder").intValue = sortOrder;
            serialized.FindProperty("_closePreviousOnOpen").boolValue = closePreviousOnOpen;
            serialized.FindProperty("_blocksInput").boolValue = blocksInput;
            serialized.FindProperty("_prefab").objectReferenceValue = prefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            _assets.Add(config);
            return config;
        }

        private UIVisualTreePanelConfigDataSo CreateVisualTreePanelConfig(
            string id,
            string layerId,
            int sortOrder,
            bool closePreviousOnOpen,
            bool blocksInput)
        {
            var visualTreeAsset = ScriptableObject.CreateInstance<VisualTreeAsset>();
            var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            _assets.Add(visualTreeAsset);
            _assets.Add(panelSettings);

            var config = ScriptableObject.CreateInstance<UIVisualTreePanelConfigDataSo>();
            var serialized = new SerializedObject(config);
            serialized.FindProperty("_id").stringValue = id;
            serialized.FindProperty("_layerId").stringValue = layerId;
            serialized.FindProperty("_sortOrder").intValue = sortOrder;
            serialized.FindProperty("_closePreviousOnOpen").boolValue = closePreviousOnOpen;
            serialized.FindProperty("_blocksInput").boolValue = blocksInput;
            serialized.FindProperty("_visualTreeAsset").objectReferenceValue = visualTreeAsset;
            serialized.FindProperty("_panelSettings").objectReferenceValue = panelSettings;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            _assets.Add(config);
            return config;
        }

        private UIProjectConfigDataSo CreateProjectConfig(
            IReadOnlyList<UILayerConfigDataSo> layers,
            IReadOnlyList<UIPanelConfigDataSo> panels)
        {
            var projectConfig = ScriptableObject.CreateInstance<UIProjectConfigDataSo>();
            var serialized = new SerializedObject(projectConfig);

            var layersProperty = serialized.FindProperty("_layers");
            layersProperty.arraySize = layers.Count;
            for (var i = 0; i < layers.Count; i++)
            {
                layersProperty.GetArrayElementAtIndex(i).objectReferenceValue = layers[i];
            }

            var panelsProperty = serialized.FindProperty("_panels");
            panelsProperty.arraySize = panels.Count;
            for (var i = 0; i < panels.Count; i++)
            {
                panelsProperty.GetArrayElementAtIndex(i).objectReferenceValue = panels[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            _assets.Add(projectConfig);
            return projectConfig;
        }
    }
}
