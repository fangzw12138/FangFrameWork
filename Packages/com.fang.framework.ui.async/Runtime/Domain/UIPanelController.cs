using Cysharp.Threading.Tasks;
using Fang.Framework.Async;

namespace Fang.Framework.UI.Async
{
    public abstract class UIPanelController<TConfig, TData> : AsyncController<TConfig, TData>, IUIPanel
        where TConfig : UIPanelConfigDataSo
        where TData : UIPanelData<TConfig>
    {
        public string PanelId => Data.RuntimeId;

        public string ConfigId => Data.Config.Id;

        public bool IsOpen => Data.IsOpen;

        public bool IsVisible => Data.IsVisible;

        public abstract TData CreateData(TConfig config);

        public async UniTask OpenAsync()
        {
            if (this == null)
            {
                return;
            }

            SetVisible(true);
            Data.MarkOpen();
            await OnOpenAsync();
        }

        public async UniTask CloseAsync()
        {
            if (this == null)
            {
                Data.MarkClosed();
                return;
            }

            await OnCloseAsync();

            Data.MarkClosed();
            SetVisible(false);
        }

        public void Focus()
        {
            OnFocus();
        }

        public void Blur()
        {
            OnBlur();
        }

        public void SetVisible(bool visible)
        {
            if (this == null)
            {
                return;
            }

            SetVisibleCore(visible);
            Data.SetVisible(visible);
        }

        protected virtual UniTask OnOpenAsync()
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask OnCloseAsync()
        {
            return UniTask.CompletedTask;
        }

        protected virtual void OnFocus()
        {
        }

        protected virtual void OnBlur()
        {
        }

        protected virtual void SetVisibleCore(bool visible)
        {
            if (this == null)
            {
                return;
            }

            gameObject.SetActive(visible);
        }
    }
}
