using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fang.Framework.UI.Kit.Editor
{
    public sealed class UIKitNewProjectWizard : EditorWindow
    {
        private const string DefaultProjectName = "UIKitProject";
        private const float LabelWidth = 112f;

        private static readonly Color DimColor = new Color(0.65f, 0.65f, 0.65f);
        private static readonly Color WarnColor = new Color(1f, 0.6f, 0.4f);
        private static readonly Color LineColor = new Color(0.28f, 0.28f, 0.28f);

        private static UIKitNewProjectWizard instance;

        private Action<UIKitProjectSo> onCreated;
        private TextField nameField;
        private TextField displayNameField;
        private TextField descriptionField;
        private TextField projectFolderField;
        private Label statusLabel;

        public static void Open(Action<UIKitProjectSo> onCreated)
        {
            if (instance != null)
            {
                instance.Focus();
                return;
            }

            var window = CreateInstance<UIKitNewProjectWizard>();
            instance = window;
            window.onCreated = onCreated;
            window.titleContent = new GUIContent("新建控件库项目");
            window.minSize = new Vector2(560f, 420f);
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

            var intro = new Label("建一个控件库项目 = 一份项目配置 SO（扫描范围 + token 表）。每一项都能改，改完点「创建」落位。");
            intro.style.whiteSpace = WhiteSpace.Normal;
            intro.style.color = DimColor;
            intro.style.marginBottom = 12f;
            form.Add(intro);

            nameField = new TextField { value = DefaultProjectName };
            nameField.RegisterValueChangedCallback(evt =>
            {
                if (string.IsNullOrWhiteSpace(displayNameField.value))
                {
                    displayNameField.SetValueWithoutNotify(evt.newValue);
                }

                UpdateStatus();
            });
            AddItem(form, "项目名称", nameField, "SO 资产名，同时是项目 Id（清单与校验里用它标识）。");

            displayNameField = new TextField();
            displayNameField.RegisterValueChangedCallback(_ => UpdateStatus());
            AddItem(form, "显示名", displayNameField, "给人看的名字，留空就用项目名称。");

            descriptionField = new TextField { multiline = true };
            descriptionField.style.minHeight = 52f;
            descriptionField.RegisterValueChangedCallback(_ => UpdateStatus());
            AddItem(form, "说明", descriptionField, "这个项目管哪些控件，写给以后的自己看。");

            projectFolderField = new TextField { value = ResolveDefaultFolder() };
            projectFolderField.RegisterValueChangedCallback(_ => UpdateStatus());
            AddItem(form, "项目位置", BuildPickerRow(projectFolderField, () => PickFolder(projectFolderField)), "SO 放在这里；token 资产放哪由你自己决定。");

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
            if (!UIKitScaffoldPaths.TryValidateProjectName(nameField.value, out _, out var nameError))
            {
                return nameError;
            }

            if (UIKitScaffoldPaths.NormalizeFolderPath(projectFolderField.value) == null)
            {
                return "项目位置必须是 Assets 下的工程相对路径。";
            }

            return null;
        }

        private void Create()
        {
            var options = new UIKitProjectCreateOptions
            {
                ProjectFolder = projectFolderField.value,
                ProjectName = nameField.value,
                DisplayName = displayNameField.value,
                Description = descriptionField.value,
            };

            var result = UIKitProjectScaffolder.CreateProject(options);
            if (!result.Success)
            {
                statusLabel.text = result.ToStatusMessage();
                statusLabel.style.color = WarnColor;
                return;
            }

            var projectPath = result.CreatedPaths.Count > 0 ? result.CreatedPaths[0] : string.Empty;
            var project = AssetDatabase.LoadAssetAtPath<UIKitProjectSo>(projectPath);
            if (project != null)
            {
                Selection.activeObject = project;
                EditorGUIUtility.PingObject(project);
            }

            onCreated?.Invoke(project);
            Close();
        }

        private static string ResolveDefaultFolder()
        {
            return UIKitScaffoldPaths.ResolveDefaultFolder();
        }
    }
}
