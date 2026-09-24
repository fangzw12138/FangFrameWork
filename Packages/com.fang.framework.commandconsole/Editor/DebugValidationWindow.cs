using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fang.Framework.CommandConsole.Editor
{
    internal sealed class DebugValidationWindow : EditorWindow
    {
        private string headerText;
        private string reportText;

        public static void Open(string projectName, string header, string report)
        {
            Open(projectName, "校验结果", header, report);
        }

        public static void Open(string projectName, string title, string header, string report)
        {
            var window = CreateInstance<DebugValidationWindow>();
            window.titleContent = new GUIContent(string.IsNullOrEmpty(projectName) ? title : title + " · " + projectName);
            window.headerText = header;
            window.reportText = report;
            window.minSize = new Vector2(480f, 300f);
            window.ShowUtility();
        }

        private void CreateGUI()
        {
            rootVisualElement.style.flexGrow = 1f;
            rootVisualElement.style.paddingLeft = 10f;
            rootVisualElement.style.paddingRight = 10f;
            rootVisualElement.style.paddingTop = 8f;
            rootVisualElement.style.paddingBottom = 8f;

            var header = new Label(headerText ?? string.Empty);
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.whiteSpace = WhiteSpace.Normal;
            header.style.marginBottom = 6f;
            rootVisualElement.Add(header);

            var scroll = new ScrollView();
            scroll.style.flexGrow = 1f;
            scroll.style.marginBottom = 6f;

            var report = new Label(reportText ?? string.Empty);
            report.style.whiteSpace = WhiteSpace.Normal;
            report.selection.isSelectable = true;
            scroll.Add(report);
            rootVisualElement.Add(scroll);

            var close = new Button(Close) { text = "关闭" };
            close.style.alignSelf = Align.FlexEnd;
            rootVisualElement.Add(close);
        }
    }
}
