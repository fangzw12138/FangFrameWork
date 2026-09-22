using UnityEngine;
using UnityEngine.UIElements;

namespace Fang.Framework.UI.Tests
{
    public sealed class UiTestScope : Scope
    {
        public T Add<T>() where T : Service
        {
            return AddService<T>();
        }
    }

    public sealed class TestPanelData : UIPanelData<UIPrefabPanelConfigDataSo>
    {
        public TestPanelData(UIPrefabPanelConfigDataSo config) : base(config)
        {
        }
    }

    public sealed class TestVisualTreePanelData : UIPanelData<UIVisualTreePanelConfigDataSo>
    {
        public TestVisualTreePanelData(UIVisualTreePanelConfigDataSo config) : base(config)
        {
        }
    }

    public sealed class TestPrefabPanel : UIPanelController<UIPrefabPanelConfigDataSo, TestPanelData>
    {
        public int OpenCount { get; private set; }

        public int CloseCount { get; private set; }

        public override TestPanelData CreateData(UIPrefabPanelConfigDataSo config)
        {
            return new TestPanelData(config);
        }

        protected override void OnOpen()
        {
            OpenCount++;
        }

        protected override void OnClose()
        {
            CloseCount++;
        }
    }

    public sealed class TestFollowPanel : UIPrefabFollowPanelController<UIPrefabPanelConfigDataSo, TestPanelData>
    {
        public override TestPanelData CreateData(UIPrefabPanelConfigDataSo config)
        {
            return new TestPanelData(config);
        }
    }

    public sealed class TestVisualTreePanel : UIVisualTreePanelController<UIVisualTreePanelConfigDataSo, TestVisualTreePanelData>
    {
        public VisualElement ExposedRoot => Root;

        public override TestVisualTreePanelData CreateData(UIVisualTreePanelConfigDataSo config)
        {
            return new TestVisualTreePanelData(config);
        }
    }
}
