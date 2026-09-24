using Fang.Framework.Editor.Hub;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fang.Framework.ConfigInspector.Editor
{
    /// <summary>
    /// FangHub「配置展开」页：说明本包做了什么、作用在哪些字段上、以及为什么没有开关。
    /// 本页只放说明，没有任何操作。
    /// </summary>
    [FangHubPage("framework-config-inspector", "配置展开", "Framework",
        Description = "在 Inspector 里展开 ConfigDataSo 引用字段，直接查看与编辑被引用的配置资产。", Order = 1)]
    public sealed class ConfigInspectorPage : IFangHubVisualElementPage
    {
        private static readonly Color DimColor = new Color(0.65f, 0.65f, 0.65f);

        public void OnInitialize(FangHubWindow window)
        {
        }

        public void OnSelected()
        {
        }

        public VisualElement CreateVisualElement()
        {
            var root = new VisualElement();
            root.style.flexGrow = 1f;

            root.Add(BuildTitle("配置展开"));
            root.Add(BuildParagraph(
                "本包给 Inspector 里的 ConfigDataSo 引用字段加一个折叠箭头：展开后内联显示被引用配置资产的内容，" +
                "可以直接改，不用再选中那个资产。它是全局生效的检查器行为，装了就起作用，没有需要打开的窗口。"));

            root.Add(BuildSection("生效范围", new[]
            {
                "只对声明类型是 ConfigDataSo 或其子类的字段生效。",
                "声明成 ScriptableObject 或其它 SO 类型的字段不受影响。",
                "全工程生效：装上本包后，Inspector、Prefab、Scene 里的这类字段都走它。",
            }));

            root.Add(BuildSection("怎么用", new[]
            {
                "字段左侧出现 ▶，点它（或点字段名）展开。",
                "展开区就是那个配置资产的属性，改完照常落盘。",
                "再点一次折叠。",
            }));

            root.Add(BuildSection("限制", new[]
            {
                "没法在运行期关掉：IMGUI 的 PropertyDrawer 里调不到同类型的默认抽屉（会递归自己），" +
                "所以关掉它的唯一干净手段是卸载本包。",
                "最多展开 2 层；自引用或超出深度时显示提示，不会继续往下套。",
            }));

            root.Add(BuildParagraph("设计理由见包内 Documentation~/配置展开.md。"));

            return root;
        }

        private static Label BuildTitle(string text)
        {
            var label = new Label(text);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.fontSize = 14f;
            label.style.marginBottom = 6f;
            return label;
        }

        private static Label BuildParagraph(string text)
        {
            var label = new Label(text);
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.color = DimColor;
            label.style.marginBottom = 10f;
            return label;
        }

        private static VisualElement BuildSection(string title, string[] lines)
        {
            var section = new VisualElement();
            section.style.marginBottom = 12f;

            var header = new Label(title);
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            section.Add(header);

            for (var i = 0; i < lines.Length; i++)
            {
                var line = new Label("· " + lines[i]);
                line.style.whiteSpace = WhiteSpace.Normal;
                line.style.marginLeft = 8f;
                line.style.marginTop = 2f;
                line.style.color = DimColor;
                section.Add(line);
            }

            return section;
        }
    }
}
