namespace Fang.Framework.UI
{
    public class UIPanelData<TConfig> : Data<TConfig> where TConfig : UIPanelConfigDataSo
    {
        public UIPanelData(TConfig config) : base(config)
        {
        }

        public string LayerId { get; internal set; }

        public int SortOrder { get; internal set; }

        public bool IsOpen { get; private set; }

        public bool IsVisible { get; private set; }

        internal void MarkOpen()
        {
            IsOpen = true;
            IsVisible = true;
        }

        internal void MarkClosed()
        {
            IsOpen = false;
            IsVisible = false;
        }

        internal void SetVisible(bool visible)
        {
            IsVisible = visible;
        }
    }
}
