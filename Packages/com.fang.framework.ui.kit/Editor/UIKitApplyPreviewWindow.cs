using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fang.Framework.UI.Kit.Editor
{
    /// <summary>应用预览：先看清要改什么，点「应用」才落盘。不做撤销，回退靠 git。</summary>
    internal sealed class UIKitApplyPreviewWindow : EditorWindow
    {
        private static readonly Color DimColor = new Color(0.65f, 0.65f, 0.65f);
        private static readonly Color LineColor = new Color(0.28f, 0.28f, 0.28f);

        private string headerText;
        private string reportText;
        private UIKitApplyPlan plan;
        private Action<UIKitApplyPlan> onApply;

        public static void Open(string projectName, UIKitApplyPlan plan, Action<UIKitApplyPlan> onApply)
        {
            var window = CreateInstance<UIKitApplyPreviewWindow>();
            window.titleContent = new GUIContent(string.IsNullOrEmpty(projectName) ? "应用预览" : "应用预览 · " + projectName);
            window.plan = plan;
            window.onApply = onApply;
            window.reportText = plan == null ? string.Empty : plan.BuildReport();
            window.headerText = plan == null
                ? "没有计划。"
                : "范围：" + plan.Scope + " —— 将写 " + plan.Rows.Count + " 条，跳过 " + plan.Skips.Count + " 条。确认后点「应用」落盘（不可撤销）。";
            window.minSize = new Vector2(520f, 320f);
            window.ShowUtility();
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.flexGrow = 1f;
            root.style.flexDirection = FlexDirection.Column;
            root.style.paddingLeft = 10f;
            root.style.paddingRight = 10f;
            root.style.paddingTop = 8f;
            root.style.paddingBottom = 8f;

            var header = new Label(headerText ?? string.Empty);
            header.style.whiteSpace = WhiteSpace.Normal;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.marginBottom = 6f;
            root.Add(header);

            var scroll = new ScrollView();
            scroll.style.flexGrow = 1f;
            scroll.style.marginBottom = 6f;

            var report = new Label(reportText ?? string.Empty);
            report.style.whiteSpace = WhiteSpace.Normal;
            report.selection.isSelectable = true;
            scroll.Add(report);
            root.Add(scroll);

            var footer = new VisualElement();
            footer.style.flexShrink = 0f;
            footer.style.flexDirection = FlexDirection.Row;
            footer.style.justifyContent = Justify.FlexEnd;
            footer.style.borderTopWidth = 1f;
            footer.style.borderTopColor = LineColor;
            footer.style.paddingTop = 8f;

            var apply = new Button(ApplyNow) { text = "应用" };
            apply.SetEnabled(plan != null && plan.HasWork);
            footer.Add(apply);
            footer.Add(new Button(Close) { text = "取消" });
            root.Add(footer);

            if (plan == null || !plan.HasWork)
            {
                var note = new Label("没有可应用的条目。");
                note.style.color = DimColor;
                note.style.marginTop = 2f;
                root.Add(note);
            }
        }

        private void ApplyNow()
        {
            var callback = onApply;
            var current = plan;
            Close();
            callback?.Invoke(current);
        }
    }
}
