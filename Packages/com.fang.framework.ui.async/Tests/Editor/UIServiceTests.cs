using System.Collections;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Fang.Framework.UI.Async.Tests
{
    public class UIServiceTests
    {
        private static readonly Regex NotConfigured =
            new Regex(Regex.Escape("[Fang.Framework.UI.Async] UIService is not configured") + ".*");

        private static readonly Regex DuplicatePanelId =
            new Regex(Regex.Escape("[Fang.Framework.UI.Async] Project config") + ".*" +
                      Regex.Escape("duplicate panel id 'TestPanel'") + ".*");

        private UILayerConfigDataSo _layer;
        private UIVisualTreePanelConfigDataSo _panelConfig;
        private UIProjectConfigDataSo _project;
        private ProbeUiScope _scope;
        private UIService _service;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _layer = TestConfigs.CreateLayer("Default", 100);
            _panelConfig = TestConfigs.CreateVisualTreePanel("TestPanel", "Default", 10, false, true);
            _project = TestConfigs.CreateProject(new[] { _layer }, new[] { _panelConfig });

            _scope = ProbeScope.Create<ProbeUiScope>();
            yield return _scope.InitAsync().ToCoroutine(null);
            _service = _scope.Add<UIService>();
        }

        [TearDown]
        public void TearDown()
        {
            ProbeScope.DestroyAll();
            TestConfigs.DestroyAll();
        }

        [UnityTest]
        public IEnumerator OpenAsync_without_configure_logs_an_error_and_returns_null()
        {
            LogAssert.Expect(LogType.Error, NotConfigured);

            TestVisualTreePanelController panel = null;
            yield return OpenPanel().ToCoroutine(result => panel = result);

            Assert.IsNull(panel);
        }

        [UnityTest]
        public IEnumerator OpenAsync_with_an_unregistered_config_logs_an_error_and_returns_null()
        {
            _service.Configure(_project);
            var stray = TestConfigs.CreateVisualTreePanel("StrayPanel", "Default", 0, false, false);

            LogAssert.Expect(
                LogType.Error,
                new Regex(Regex.Escape("[Fang.Framework.UI.Async] Panel config 'StrayPanel' is not registered") + ".*"));

            TestVisualTreePanelController panel = null;
            yield return _service
                .OpenAsync<TestVisualTreePanelController, UIVisualTreePanelConfigDataSo, TestPanelData>(stray)
                .ToCoroutine(result => panel = result);

            Assert.IsNull(panel);
        }

        [UnityTest]
        public IEnumerator Configure_rejects_a_project_with_duplicate_panel_ids()
        {
            var duplicated = TestConfigs.CreateProject(new[] { _layer }, new[] { _panelConfig, _panelConfig });

            LogAssert.Expect(LogType.Error, DuplicatePanelId);
            _service.Configure(duplicated);

            LogAssert.Expect(LogType.Error, NotConfigured);

            TestVisualTreePanelController panel = null;
            yield return OpenPanel().ToCoroutine(result => panel = result);

            Assert.IsNull(panel);
        }

        [UnityTest]
        public IEnumerator OpenAsync_builds_a_uidocument_with_the_resolved_sort_order()
        {
            _service.Configure(_project);

            TestVisualTreePanelController panel = null;
            yield return OpenPanel().ToCoroutine(result => panel = result);

            Assert.IsNotNull(panel);
            Assert.AreEqual("UITK", panel.transform.parent.name);
            Assert.AreEqual("Default", panel.Data.LayerId);
            Assert.AreEqual(110, panel.Data.SortOrder);

            var document = panel.Document;

            Assert.IsNotNull(document);
            Assert.AreSame(TestConfigs.TestPanelSettings, document.panelSettings);
            Assert.AreSame(TestConfigs.TestVisualTreeAsset, document.visualTreeAsset);
            Assert.AreEqual(110, document.sortingOrder);

            Assert.IsNotNull(panel.RootElement);
            Assert.IsNotNull(panel.RootElement.Q<Label>("caption"));
            Assert.AreEqual(DisplayStyle.Flex, panel.RootElement.style.display.value);
            Assert.IsTrue(_service.IsOpen("TestPanel"));
            Assert.AreSame(panel, _service.GetOpened("TestPanel"));
        }

        [UnityTest]
        public IEnumerator CloseAsync_hides_the_visual_tree_root_without_touching_the_game_object()
        {
            _service.Configure(_project);

            TestVisualTreePanelController panel = null;
            yield return OpenPanel().ToCoroutine(result => panel = result);

            var closed = false;
            yield return _service.CloseAsync("TestPanel").ToCoroutine(result => closed = result);

            Assert.IsTrue(closed);
            Assert.IsFalse(_service.IsOpen("TestPanel"));
            Assert.IsNull(_service.GetOpened("TestPanel"));
            Assert.AreEqual(1, panel.CloseCount);
            Assert.AreEqual(DisplayStyle.None, panel.RootElement.style.display.value);
            Assert.IsTrue(panel.gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator OpenAsync_logs_and_returns_null_when_the_visual_tree_asset_is_missing()
        {
            var config = TestConfigs.CreateVisualTreePanel("NoUxml", "Default", 0, false, false, false);
            var project = TestConfigs.CreateProject(new[] { _layer }, new[] { config });
            _service.Configure(project);

            LogAssert.Expect(
                LogType.Error,
                new Regex(Regex.Escape("[Fang.Framework.UI.Async] UI Toolkit panel 'NoUxml' has no visual tree asset") + ".*"));

            TestVisualTreePanelController panel = null;
            yield return OpenPanelById("NoUxml").ToCoroutine(result => panel = result);

            Assert.IsNull(panel);
        }

        [UnityTest]
        public IEnumerator OpenAsync_logs_and_returns_null_when_the_panel_settings_are_missing()
        {
            var config = TestConfigs.CreateVisualTreePanel("NoSettings", "Default", 0, false, false, true, false);
            var project = TestConfigs.CreateProject(new[] { _layer }, new[] { config });
            _service.Configure(project);

            LogAssert.Expect(
                LogType.Error,
                new Regex(Regex.Escape("[Fang.Framework.UI.Async] UI Toolkit panel 'NoSettings' has no panel settings") + ".*"));

            TestVisualTreePanelController panel = null;
            yield return OpenPanelById("NoSettings").ToCoroutine(result => panel = result);

            Assert.IsNull(panel);
        }

        [UnityTest]
        public IEnumerator OpenAsync_reuses_the_same_instance_and_ignores_a_second_push()
        {
            _service.Configure(_project);

            TestVisualTreePanelController first = null;
            yield return OpenPanel().ToCoroutine(result => first = result);

            TestVisualTreePanelController second = null;
            yield return OpenPanel().ToCoroutine(result => second = result);

            Assert.AreSame(first, second);
            Assert.AreEqual(1, first.OpenCount);
        }

        [UnityTest]
        public IEnumerator OpenAsync_after_close_reuses_the_cached_instance()
        {
            _service.Configure(_project);

            TestVisualTreePanelController first = null;
            yield return OpenPanel().ToCoroutine(result => first = result);

            var closed = false;
            yield return _service.CloseAsync("TestPanel").ToCoroutine(result => closed = result);
            Assert.IsTrue(closed);

            TestVisualTreePanelController second = null;
            yield return OpenPanel().ToCoroutine(result => second = result);

            Assert.AreSame(first, second);
            Assert.AreEqual(1, first.CloseCount);
            Assert.AreEqual(2, first.OpenCount);
            Assert.IsTrue(_service.IsOpen("TestPanel"));
        }

        [UnityTest]
        public IEnumerator CloseAsync_returns_false_for_an_unknown_panel()
        {
            _service.Configure(_project);

            var closed = true;
            yield return _service.CloseAsync("Missing").ToCoroutine(result => closed = result);
            Assert.IsFalse(closed);

            var closedTop = true;
            yield return _service.CloseTopAsync().ToCoroutine(result => closedTop = result);
            Assert.IsFalse(closedTop);
        }

        [UnityTest]
        public IEnumerator Input_blocking_events_are_raised_through_the_service()
        {
            _service.Configure(_project);

            var opened = 0;
            var closed = 0;
            _service.InputBlockingPanelOpened += () => opened++;
            _service.InputBlockingPanelClosed += () => closed++;

            TestVisualTreePanelController panel = null;
            yield return OpenPanel().ToCoroutine(result => panel = result);

            Assert.AreEqual(1, opened);
            Assert.AreEqual(0, closed);
            Assert.IsTrue(_service.HasInputBlockingPanel);

            var didClose = false;
            yield return _service.CloseAsync("TestPanel").ToCoroutine(result => didClose = result);

            Assert.IsTrue(didClose);
            Assert.AreEqual(1, closed);
            Assert.IsFalse(_service.HasInputBlockingPanel);
        }

        [UnityTest]
        public IEnumerator DisposeAsync_closes_and_disposes_the_open_panels()
        {
            _service.Configure(_project);

            TestVisualTreePanelController panel = null;
            yield return OpenPanel().ToCoroutine(result => panel = result);

            yield return _service.DisposeAsync().ToCoroutine(null);

            Assert.AreEqual(1, panel.CloseCount);
            Assert.AreEqual(1, panel.DisposeCount);
            Assert.IsFalse(_service.HasInputBlockingPanel);
        }

        [UnityTest]
        public IEnumerator DisposeAsync_is_idempotent()
        {
            _service.Configure(_project);

            TestVisualTreePanelController panel = null;
            yield return OpenPanel().ToCoroutine(result => panel = result);

            yield return _service.DisposeAsync().ToCoroutine(null);
            yield return _service.DisposeAsync().ToCoroutine(null);

            Assert.AreEqual(1, panel.DisposeCount);
            Assert.AreEqual(1, panel.CloseCount);
        }

        [UnityTest]
        public IEnumerator OpenAsync_by_id_resolves_the_registered_config()
        {
            _service.Configure(_project);

            TestVisualTreePanelController panel = null;
            yield return OpenPanelById("TestPanel").ToCoroutine(result => panel = result);

            Assert.IsNotNull(panel);
            Assert.AreEqual("TestPanel", panel.ConfigId);
            Assert.AreEqual("Default", panel.Data.LayerId);
            Assert.AreEqual(110, panel.Data.SortOrder);
            Assert.IsTrue(_service.IsOpen("TestPanel"));
        }

        [UnityTest]
        public IEnumerator OpenAsync_by_id_reuses_the_same_instance()
        {
            _service.Configure(_project);

            TestVisualTreePanelController first = null;
            yield return OpenPanelById("TestPanel").ToCoroutine(result => first = result);

            TestVisualTreePanelController second = null;
            yield return OpenPanelById("TestPanel").ToCoroutine(result => second = result);

            Assert.AreSame(first, second);
            Assert.AreEqual(1, first.OpenCount);
        }

        [UnityTest]
        public IEnumerator OpenAsync_by_id_after_close_reuses_the_cached_instance()
        {
            _service.Configure(_project);

            TestVisualTreePanelController first = null;
            yield return OpenPanelById("TestPanel").ToCoroutine(result => first = result);

            var closed = false;
            yield return _service.CloseAsync("TestPanel").ToCoroutine(result => closed = result);
            Assert.IsTrue(closed);

            TestVisualTreePanelController second = null;
            yield return OpenPanelById("TestPanel").ToCoroutine(result => second = result);

            Assert.AreSame(first, second);
            Assert.AreEqual(2, first.OpenCount);
        }

        [UnityTest]
        public IEnumerator OpenAsync_by_id_without_configure_logs_an_error_and_returns_null()
        {
            LogAssert.Expect(LogType.Error, NotConfigured);

            TestVisualTreePanelController panel = null;
            yield return OpenPanelById("TestPanel").ToCoroutine(result => panel = result);

            Assert.IsNull(panel);
        }

        [UnityTest]
        public IEnumerator OpenAsync_by_id_with_an_unknown_id_logs_an_error_and_returns_null()
        {
            _service.Configure(_project);

            LogAssert.Expect(
                LogType.Error,
                new Regex(Regex.Escape("[Fang.Framework.UI.Async] Panel config 'Missing' is not registered") + ".*"));

            TestVisualTreePanelController panel = null;
            yield return OpenPanelById("Missing").ToCoroutine(result => panel = result);

            Assert.IsNull(panel);
        }

        [UnityTest]
        public IEnumerator OpenAsync_by_id_with_a_mismatched_track_logs_an_error_and_returns_null()
        {
            _service.Configure(_project);

            LogAssert.Expect(
                LogType.Error,
                new Regex(Regex.Escape("[Fang.Framework.UI.Async] Panel config 'TestPanel' is not registered as") + ".*"));

            TestPrefabPanelController panel = null;
            yield return _service
                .OpenAsync<TestPrefabPanelController, UIPrefabPanelConfigDataSo, TestPrefabPanelData>("TestPanel")
                .ToCoroutine(result => panel = result);

            Assert.IsNull(panel);
        }

        [UnityTest]
        public IEnumerator A_ugui_panel_and_a_visual_tree_panel_share_one_stack_with_layer_ordering()
        {
            var prefabConfig = TestConfigs.CreatePrefabPanel("PrefabPanel", "Default", 5, false, true);
            var project = TestConfigs.CreateProject(new[] { _layer }, new UIPanelConfigDataSo[] { prefabConfig, _panelConfig });
            _service.Configure(project);

            TestPrefabPanelController ugui = null;
            yield return _service
                .OpenAsync<TestPrefabPanelController, UIPrefabPanelConfigDataSo, TestPrefabPanelData>("PrefabPanel")
                .ToCoroutine(result => ugui = result);

            TestVisualTreePanelController visualTree = null;
            yield return OpenPanelById("TestPanel").ToCoroutine(result => visualTree = result);

            Assert.IsNotNull(ugui);
            Assert.IsNotNull(visualTree);
            Assert.AreEqual(105, ugui.Data.SortOrder);
            Assert.AreEqual(110, visualTree.Data.SortOrder);

            Assert.AreEqual("Canvas_105", ugui.transform.parent.name);
            Assert.AreEqual("UGUI", ugui.transform.parent.parent.name);
            Assert.AreEqual(105, ugui.GetComponentInParent<Canvas>().sortingOrder);
            Assert.IsTrue(ugui.gameObject.activeSelf);

            Assert.AreEqual("UITK", visualTree.transform.parent.name);
            Assert.IsNotNull(visualTree.Document);
            Assert.AreEqual(110, visualTree.Document.sortingOrder);

            Assert.IsTrue(_service.IsOpen("PrefabPanel"));
            Assert.IsTrue(_service.IsOpen("TestPanel"));
            Assert.IsTrue(_service.HasInputBlockingPanel);
        }

        private UniTask<TestVisualTreePanelController> OpenPanel()
        {
            return _service
                .OpenAsync<TestVisualTreePanelController, UIVisualTreePanelConfigDataSo, TestPanelData>(_panelConfig);
        }

        private UniTask<TestVisualTreePanelController> OpenPanelById(string configId)
        {
            return _service
                .OpenAsync<TestVisualTreePanelController, UIVisualTreePanelConfigDataSo, TestPanelData>(configId);
        }
    }
}
