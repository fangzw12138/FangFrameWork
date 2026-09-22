using UnityEngine;

namespace Fang.Framework.UI
{
    public abstract class UIPanelController<TConfig, TData> : Controller<TConfig, TData>, IUIPanel
        where TConfig : UIPanelConfigDataSo
        where TData : UIPanelData<TConfig>
    {
        public string PanelId => Data.RuntimeId;

        public string ConfigId => Data.Config.Id;

        public bool IsOpen => Data.IsOpen;

        public bool IsVisible => Data.IsVisible;

        public abstract TData CreateData(TConfig config);

        public override void OnInit()
        {
        }

        public override void OnDispose()
        {
        }

        protected virtual void OnOpen()
        {
        }

        protected virtual void OnClose()
        {
        }

        protected virtual void OnFocus()
        {
        }

        protected virtual void OnBlur()
        {
        }

        protected virtual void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        void IUIPanel.Open()
        {
            SetVisible(true);
            Data.MarkOpen();
            OnOpen();
        }

        void IUIPanel.Close()
        {
            OnClose();
            Data.MarkClosed();
            SetVisible(false);
        }

        void IUIPanel.Focus()
        {
            OnFocus();
        }

        void IUIPanel.Blur()
        {
            OnBlur();
        }

        void IUIPanel.SetVisible(bool visible)
        {
            SetVisible(visible);
            Data.SetVisible(visible);
        }
    }
}
