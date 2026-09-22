namespace Fang.Framework.UI
{
    public interface IUIPanel
    {
        string PanelId { get; }

        string ConfigId { get; }

        bool IsOpen { get; }

        bool IsVisible { get; }

        void Open();

        void Close();

        void Focus();

        void Blur();

        void SetVisible(bool visible);

        void OnDispose();
    }
}
