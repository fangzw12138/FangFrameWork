using NUnit.Framework;
using UnityEngine;

namespace Fang.Framework.UI.Tests
{
    public class UIPanelDataTests
    {
        private UIPrefabPanelConfigDataSo _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<UIPrefabPanelConfigDataSo>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void MarkOpen_sets_open_and_visible()
        {
            var data = new UIPanelData<UIPrefabPanelConfigDataSo>(_config);

            data.MarkOpen();

            Assert.IsTrue(data.IsOpen);
            Assert.IsTrue(data.IsVisible);
        }

        [Test]
        public void MarkClosed_clears_open_and_visible()
        {
            var data = new UIPanelData<UIPrefabPanelConfigDataSo>(_config);
            data.MarkOpen();

            data.MarkClosed();

            Assert.IsFalse(data.IsOpen);
            Assert.IsFalse(data.IsVisible);
        }

        [Test]
        public void SetVisible_keeps_the_open_state()
        {
            var data = new UIPanelData<UIPrefabPanelConfigDataSo>(_config);
            data.MarkOpen();

            data.SetVisible(false);

            Assert.IsTrue(data.IsOpen);
            Assert.IsFalse(data.IsVisible);
        }

        [Test]
        public void RuntimeId_is_unique_per_instance()
        {
            var first = new UIPanelData<UIPrefabPanelConfigDataSo>(_config);
            var second = new UIPanelData<UIPrefabPanelConfigDataSo>(_config);

            Assert.IsNotEmpty(first.RuntimeId);
            Assert.AreNotEqual(first.RuntimeId, second.RuntimeId);
        }

        [Test]
        public void Config_points_to_the_asset()
        {
            var data = new UIPanelData<UIPrefabPanelConfigDataSo>(_config);

            Assert.AreSame(_config, data.Config);
        }

        [Test]
        public void LayerId_and_SortOrder_default_to_empty_and_zero()
        {
            var data = new UIPanelData<UIPrefabPanelConfigDataSo>(_config);

            Assert.IsNull(data.LayerId);
            Assert.AreEqual(0, data.SortOrder);
        }
    }
}
