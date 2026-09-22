using UnityEngine.UIElements;

namespace Fang.Framework.Editor.Hub
{
    public interface IFangHubVisualElementPage : IFangHubPage
    {
        VisualElement CreateVisualElement();
    }
}
