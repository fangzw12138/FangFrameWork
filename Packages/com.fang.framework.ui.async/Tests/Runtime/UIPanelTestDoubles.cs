using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Fang.Framework.Async;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fang.Framework.UI.Async.Tests
{
    internal sealed class FakePanel : IUIPanel
    {
        public FakePanel(string configId)
        {
            ConfigId = configId;
            PanelId = Guid.NewGuid().ToString();
        }

        public string PanelId { get; }

        public string ConfigId { get; }

        public bool IsOpen { get; private set; }

        public bool IsVisible { get; private set; }

        public int OpenCount { get; private set; }

        public int CloseCount { get; private set; }

        public int FocusCount { get; private set; }

        public int BlurCount { get; private set; }

        public List<string> Trace { get; } = new List<string>();

        public UniTask OpenAsync()
        {
            OpenCount++;
            IsOpen = true;
            IsVisible = true;
            Trace.Add("open");
            return UniTask.CompletedTask;
        }

        public UniTask CloseAsync()
        {
            CloseCount++;
            IsOpen = false;
            IsVisible = false;
            Trace.Add("close");
            return UniTask.CompletedTask;
        }

        public void Focus()
        {
            FocusCount++;
            Trace.Add("focus");
        }

        public void Blur()
        {
            BlurCount++;
            Trace.Add("blur");
        }

        public void SetVisible(bool visible)
        {
            IsVisible = visible;
            Trace.Add(visible ? "visible:true" : "visible:false");
        }

        public UniTask DisposeAsync()
        {
            Trace.Add("dispose");
            return UniTask.CompletedTask;
        }
    }

    public sealed class TestPrefabPanelData : UIPanelData<UIPrefabPanelConfigDataSo>
    {
        public TestPrefabPanelData(UIPrefabPanelConfigDataSo config) : base(config)
        {
        }
    }

    public sealed class TestPrefabPanelController : UIPanelController<UIPrefabPanelConfigDataSo, TestPrefabPanelData>
    {
        public int OpenCount { get; private set; }

        public int CloseCount { get; private set; }

        public int FocusCount { get; private set; }

        public int BlurCount { get; private set; }

        public int DisposeCount { get; private set; }

        public bool? OpenFlagInsideOpenHook { get; private set; }

        public bool? VisibleFlagInsideOpenHook { get; private set; }

        public bool? OpenFlagInsideCloseHook { get; private set; }

        public override TestPrefabPanelData CreateData(UIPrefabPanelConfigDataSo config)
        {
            return new TestPrefabPanelData(config);
        }

        protected override UniTask OnOpenAsync()
        {
            OpenCount++;
            OpenFlagInsideOpenHook = IsOpen;
            VisibleFlagInsideOpenHook = IsVisible;
            return UniTask.CompletedTask;
        }

        protected override UniTask OnCloseAsync()
        {
            CloseCount++;
            OpenFlagInsideCloseHook = IsOpen;
            return UniTask.CompletedTask;
        }

        protected override void OnFocus()
        {
            FocusCount++;
        }

        protected override void OnBlur()
        {
            BlurCount++;
        }

        protected override UniTask OnDisposeAsync()
        {
            DisposeCount++;
            return UniTask.CompletedTask;
        }
    }

    public sealed class TestPanelData : UIPanelData<UIVisualTreePanelConfigDataSo>
    {
        public TestPanelData(UIVisualTreePanelConfigDataSo config) : base(config)
        {
        }
    }

    public sealed class TestVisualTreePanelController : UIVisualTreePanelController<UIVisualTreePanelConfigDataSo, TestPanelData>
    {
        public VisualElement RootElement => Root;

        public UIDocument Document => GetComponent<UIDocument>();

        public int OpenCount { get; private set; }

        public int CloseCount { get; private set; }

        public int DisposeCount { get; private set; }

        public override TestPanelData CreateData(UIVisualTreePanelConfigDataSo config)
        {
            return new TestPanelData(config);
        }

        protected override UniTask OnOpenAsync()
        {
            OpenCount++;
            return UniTask.CompletedTask;
        }

        protected override UniTask OnCloseAsync()
        {
            CloseCount++;
            return UniTask.CompletedTask;
        }

        protected override UniTask OnDisposeAsync()
        {
            DisposeCount++;
            return UniTask.CompletedTask;
        }
    }

    internal class ProbeScope : AsyncScope
    {
        private static readonly List<GameObject> Hosts = new List<GameObject>();

        public static TScope Create<TScope>() where TScope : AsyncScope
        {
            var host = new GameObject(typeof(TScope).Name);
            Hosts.Add(host);
            return host.AddComponent<TScope>();
        }

        public static void DestroyAll()
        {
            for (var i = Hosts.Count - 1; i >= 0; i--)
            {
                if (Hosts[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(Hosts[i]);
                }
            }

            Hosts.Clear();
        }

        public TService Add<TService>() where TService : MonoBehaviour, IAsyncLifecycle
        {
            return AddService<TService>();
        }
    }

    internal sealed class ProbeUiScope : ProbeScope
    {
    }
}
