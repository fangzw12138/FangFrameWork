using Cysharp.Threading.Tasks;

namespace Fang.Framework.UI.Async
{
    public interface IUIPanel
    {
        string PanelId { get; }

        string ConfigId { get; }

        bool IsOpen { get; }

        bool IsVisible { get; }

        UniTask OpenAsync();

        UniTask CloseAsync();

        void Focus();

        void Blur();

        void SetVisible(bool visible);

        UniTask DisposeAsync();
    }
}
