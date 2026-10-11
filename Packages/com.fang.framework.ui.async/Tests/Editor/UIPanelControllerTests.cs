using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Fang.Framework.UI.Async.Tests
{
    public class UIPanelControllerTests
    {
        private UIPrefabPanelConfigDataSo _config;
        private GameObject _host;
        private TestPrefabPanelController _panel;

        [TearDown]
        public void TearDown()
        {
            if (_host != null)
            {
                Object.DestroyImmediate(_host);
            }

            TestConfigs.DestroyAll();
        }

        [UnityTest]
        public IEnumerator InitAsync_binds_the_data_to_the_controller()
        {
            yield return InitPanelAsync();

            Assert.AreSame(_config, _panel.Data.Config);
            Assert.AreEqual(_panel.Data.RuntimeId, _panel.PanelId);
            Assert.AreEqual("TestPanel", _panel.ConfigId);
            Assert.IsFalse(_panel.IsOpen);
            Assert.IsFalse(_panel.IsVisible);
        }

        [UnityTest]
        public IEnumerator OpenAsync_shows_marks_open_and_only_then_runs_the_open_hook()
        {
            yield return InitPanelAsync();

            yield return _panel.OpenAsync().ToCoroutine(null);

            Assert.IsTrue(_panel.OpenFlagInsideOpenHook);
            Assert.IsTrue(_panel.VisibleFlagInsideOpenHook);
            Assert.IsTrue(_panel.IsOpen);
            Assert.IsTrue(_panel.IsVisible);
            Assert.IsTrue(_host.activeSelf);
            Assert.AreEqual(1, _panel.OpenCount);
        }

        [UnityTest]
        public IEnumerator CloseAsync_runs_the_close_hook_while_still_open_then_hides()
        {
            yield return InitPanelAsync();

            yield return _panel.OpenAsync().ToCoroutine(null);

            yield return _panel.CloseAsync().ToCoroutine(null);

            Assert.IsTrue(_panel.OpenFlagInsideCloseHook);
            Assert.IsFalse(_panel.IsOpen);
            Assert.IsFalse(_panel.IsVisible);
            Assert.IsFalse(_host.activeSelf);
            Assert.AreEqual(1, _panel.CloseCount);
        }

        [UnityTest]
        public IEnumerator SetVisible_toggles_the_game_object_and_the_data_flag()
        {
            yield return InitPanelAsync();

            _panel.SetVisible(false);

            Assert.IsFalse(_host.activeSelf);
            Assert.IsFalse(_panel.IsVisible);

            _panel.SetVisible(true);

            Assert.IsTrue(_host.activeSelf);
            Assert.IsTrue(_panel.IsVisible);
        }

        [UnityTest]
        public IEnumerator Focus_and_Blur_run_the_hooks()
        {
            yield return InitPanelAsync();

            _panel.Focus();
            _panel.Blur();

            Assert.AreEqual(1, _panel.FocusCount);
            Assert.AreEqual(1, _panel.BlurCount);
        }

        [UnityTest]
        public IEnumerator DisposeAsync_runs_the_dispose_hook_once()
        {
            yield return InitPanelAsync();

            yield return _panel.OpenAsync().ToCoroutine(null);

            yield return _panel.DisposeAsync().ToCoroutine(null);
            yield return _panel.DisposeAsync().ToCoroutine(null);

            Assert.AreEqual(1, _panel.DisposeCount);
        }

        private IEnumerator InitPanelAsync()
        {
            _config = TestConfigs.CreatePrefabPanel("TestPanel", "Default", 10, false, false, false);

            _host = new GameObject("TestPanel");
            _panel = _host.AddComponent<TestPrefabPanelController>();

            yield return _panel.InitAsync(new TestPrefabPanelData(_config)).ToCoroutine(null);
        }
    }
}
