using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fang.Framework.UI.Kit.Editor
{
    /// <summary>新建控件预制体：造一个 UI 空壳（RectTransform + TokenMatch）并登记进项目 SO。</summary>
    public sealed class UIKitNewPrefabWizard : EditorWindow
    {
        private const string DefaultPrefabName = "UIWidget";
        private const float LabelWidth = 112f;

        private static readonly Color DimColor = new Color(0.65f, 0.65f, 0.65f);
        private static readonly Color WarnColor = new Color(1f, 0.6f, 0.4f);
        private static readonly Color LineColor = new Color(0.28f, 0.28f, 0.28f);

        private static UIKitNewPrefabWizard instance;

        private UIKitProjectSo project;
        private Action<GameObject> onCreated;
        private TextField nameField;
        private TextField folderField;
        private Label statusLabel;

        public static void Open(UIKitProjectSo project, Action<GameObject> onCreated)
        {
            if (instance != null)
            {
                instance.Focus();
                return;
            }

            var window = CreateInstance<UIKitNewPrefabWizard>();
            instance = window;
            window.project = project;
            window.onCreated = onCreated;
            window.titleContent = new GUIContent("新建控件预制体");
            window.minSize = new Vector2(560f, 320f);
            window.ShowUtility();
        }

        private void OnDisable()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.flexGrow = 1f;
            root.style.flexDirection = FlexDirection.Column;

            var scroll = new ScrollView();
            scroll.style.flexGrow = 1f;
            root.Add(scroll);

            var form = scroll.contentContainer;
            form.style.paddingLeft = 12f;
            form.style.paddingRight = 12f;
            form.style.paddingTop = 10f;
            form.style.paddingBottom = 10f;

            var intro = new Label("建一个控件 = 一个 UI 空壳预制体（根节点带 RectTransform 与 TokenMatch），建完登记进当前项目。尺寸与内容由你自己在预制体里摆。");
            intro.style.whiteSpace = WhiteSpace.Normal;
            intro.style.color = DimColor;
            intro.style.marginBottom = 12f;
            form.Add(intro);

            nameField = new TextField { value = DefaultPrefabName };
            nameField.RegisterValueChangedCallback(_ => UpdateStatus());
            AddItem(form, "预制体名", nameField, "预制体资产名，也是根节点名；必须是合法标识符。");

            folderField = new TextField { value = UIKitScaffoldPaths.ResolveDefaultFolder() };
            folderField.RegisterValueChangedCallback(_ => UpdateStatus());
            AddItem(form, "创建位置", BuildPickerRow(folderField, () => PickFolder(folderField)), "预制体放这里，默认取当前选中资产所在目录。");

            root.Add(BuildFooter());
            UpdateStatus();
        }

        private VisualElement BuildFooter()
        {
            var footer = new VisualElement();
            footer.style.flexShrink = 0f;
            footer.style.borderTopWidth = 1f;
            footer.style.borderTopColor = LineColor;
            footer.style.paddingLeft = 12f;
            footer.style.paddingRight = 12f;
            footer.style.paddingTop = 8f;
            footer.style.paddingBottom = 8f;

            statusLabel = new Label();
            statusLabel.style.whiteSpace = WhiteSpace.Normal;
            statusLabel.style.marginBottom = 6f;
            footer.Add(statusLabel);

            var buttons = new VisualElement();
            buttons.style.flexDirection = FlexDirection.Row;
            buttons.style.justifyContent = Justify.FlexEnd;
            buttons.Add(new Button(Create) { text = "创建" });
            buttons.Add(new Button(Close) { text = "取消" });
            footer.Add(buttons);

            return footer;
        }

        private static void AddItem(VisualElement parent, string label, VisualElement field, string description)
        {
            var block = new VisualElement();
            block.style.flexShrink = 0f;
            block.style.marginBottom = 10f;

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.flexShrink = 0f;

            var title = new Label(label);
            title.style.width = LabelWidth;
            title.style.flexShrink = 0f;
            row.Add(title);

            field.style.flexGrow = 1f;
            field.style.flexShrink = 1f;
            row.Add(field);

            block.Add(row);

            var hint = new Label(description);
            hint.style.whiteSpace = WhiteSpace.Normal;
            hint.style.color = DimColor;
            hint.style.fontSize = 11f;
            hint.style.marginLeft = LabelWidth;
            hint.style.marginTop = 2f;
            block.Add(hint);

            parent.Add(block);
        }

        private static VisualElement BuildPickerRow(TextField field, Action pick)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.flexShrink = 0f;

            field.style.flexGrow = 1f;
            field.style.flexShrink = 1f;
            row.Add(field);

            var button = new Button(pick) { text = "选择…" };
            button.style.flexShrink = 0f;
            button.style.marginLeft = 4f;
            row.Add(button);

            return row;
        }

        private static void PickFolder(TextField field)
        {
            var picked = EditorUtility.OpenFolderPanel("选择目录", "Assets", string.Empty);
            if (string.IsNullOrEmpty(picked))
            {
                return;
            }

            var relative = FileUtil.GetProjectRelativePath(picked);
            if (string.IsNullOrEmpty(relative))
            {
                return;
            }

            field.value = relative;
        }

        private void UpdateStatus()
        {
            if (statusLabel == null)
            {
                return;
            }

            var problem = FindProblem();
            statusLabel.text = problem ?? "可以创建。";
            statusLabel.style.color = problem == null ? DimColor : WarnColor;
        }

        private string FindProblem()
        {
            if (project == null)
            {
                return "没有选择控件库项目。";
            }

            if (!UIKitScaffoldPaths.TryValidateAssetName(nameField.value, "预制体名", out _, out var nameError))
            {
                return nameError;
            }

            if (UIKitScaffoldPaths.NormalizeFolderPath(folderField.value) == null)
            {
                return "创建位置必须是 Assets 下的工程相对路径。";
            }

            return null;
        }

        private void Create()
        {
            var result = UIKitProjectScaffolder.CreatePrefabShell(project, folderField.value, nameField.value);
            if (!result.Success)
            {
                statusLabel.text = result.ToStatusMessage();
                statusLabel.style.color = WarnColor;
                return;
            }

            var prefabPath = result.CreatedPaths.Count > 0 ? result.CreatedPaths[0] : string.Empty;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
            }

            onCreated?.Invoke(prefab);
            Close();
        }
    }
}
