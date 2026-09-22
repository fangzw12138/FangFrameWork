namespace Fang.Framework.Editor.Hub
{
    public interface IFangHubPage
    {
        void OnInitialize(FangHubWindow window);

        void OnSelected();
    }
}
