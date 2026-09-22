using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fang.Framework.UI.Editor
{
    public sealed class UINewProjectWizard : EditorWindow
    {
        private const string DefaultProjectName = "UIProject";
        private const string DefaultRootFolder = "Assets";
        private const float LabelWidth = 112f;

        private static readonly string[] FolderDescriptions =
        {
            "每个面板一个 {面板名}ConfigData.asset，平铺放这里。",
            "UGUI 轨面板的 {面板名}.prefab，平铺放这里。",
            "每面板一个子目录：{面板名}Data.cs 与 {面板名}Controller.cs。",
            "UITK 轨面板：每面板一个子目录，放 {面板名}.uxml 与 {面板名}.uss。",
            "本项目自己的层配置，别的 UI 项目不受影响。",
        };

        private static readonly Color DimColor = new Color(0.65f, 0.65f, 0.65f);
        private static readonly Color WarnColor = new Color(1f, 0.6f, 0.4f);
        private static readonly Color LineColor = new Color(0.28f, 0.28f, 0.28f);

        private static UINewProjectWizard instance;

        private Action<UIProjectConfigDataSo> onCreated;
        private TextField nameField;
        private TextField projectFolderField;
        private TextField namespaceField;
        private TextField[] folderFields;
        private Label statusLabel;
        private bool namespaceEdited;
        private bool foldersEdited;
        private bool applyingDefaults;

        public static void Open(Action<UIProjectConfigDataSo> onCreated)
        {
            if (instance != null)
            {
                instance.Focus();
                return;
            }

            var window = CreateInstance<UINewProjectWizard>();
            instance = window;
            window.onCreated = onCreated;
            window.titleContent = new GUIContent("新建 UI 项目");
            window.minSize = new Vector2(560f, 460f);
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

            var intro = new Label("建一个 UI 项目 = 一份项目 SO（目录 + 层清单 + 面板清单）。目录都能改，改完点「创建」，SO 与 5 个目录一起落位。");
            intro.style.whiteSpace = WhiteSpace.Normal;
            intro.style.color = DimColor;
            intro.style.marginBottom = 12f;
            form.Add(intro);

            nameField = new TextField { value = DefaultProjectName };
            nameField.RegisterValueChangedCallback(evt =>
            {
                if (!namespaceEdited)
                {
                    namespaceField.SetValueWithoutNotify(DeriveNamespace(evt.newValue));
                }

                UpdateStatus();
            });
            AddItem(form, "项目名称", nameField, "SO 资产名，同时是项目 Id（清单与校验里用它标识）。");

            projectFolderField = new TextField { value = ResolveDefaultFolder() };
            projectFolderField.RegisterValueChangedCallback(_ =>
            {
                ApplyFolderDefaults();
                UpdateStatus();
            });
            AddItem(form, "项目位置", BuildPickerRow(projectFolderField, () => PickFolder(projectFolderField)), "SO 放在这里，下面 5 个目录默认建在它下面。");

            folderFields = new TextField[UIPanelScaffolder.DefaultProjectFolderNames.Length];
            for (var i = 0; i < folderFields.Length; i++)
            {
                var index = i;
                var field = new TextField();
                field.RegisterValueChangedCallback(_ =>
                {
                    if (!applyingDefaults)
                    {
                        foldersEdited = true;
                    }

                    UpdateStatus();
                });

                folderFields[i] = field;
                AddItem(
                    form,
                    UIPanelScaffolder.DefaultProjectFolderLabels[i],
                    BuildPickerRow(field, () => PickFolder(field)),
                    FolderDescriptions[i]);
            }

            namespaceField = new TextField { value = DeriveNamespace(DefaultProjectName) };
            namespaceField.RegisterValueChangedCallback(_ =>
            {
                namespaceEdited = true;
                UpdateStatus();
            });
            AddItem(form, "根命名空间", namespaceField, "生成脚本的命名空间前缀，实际写入 {前缀}.UI.Panels。");

            ApplyFolderDefaults();

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

        private void ApplyFolderDefaults()
        {
            if (foldersEdited || folderFields == null)
            {
                return;
            }

            var root = UIPanelScaffoldPaths.NormalizeFolderPath(projectFolderField.value);
            if (root == null)
            {
                return;
            }

            applyingDefaults = true;
            for (var i = 0; i < folderFields.Length; i++)
            {
                folderFields[i].SetValueWithoutNotify(root + "/" + UIPanelScaffolder.DefaultProjectFolderNames[i]);
            }

            applyingDefaults = false;
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
            if (!UIPanelScaffoldPaths.TryValidateProjectName(nameField.value, out _, out var nameError))
            {
                return nameError;
            }

            if (UIPanelScaffoldPaths.NormalizeFolderPath(projectFolderField.value) == null)
            {
                return "项目位置必须是 Assets 下的工程相对路径。";
            }

            for (var i = 0; i < folderFields.Length; i++)
            {
                if (UIPanelScaffoldPaths.NormalizeFolderPath(folderFields[i].value) == null)
                {
                    return UIPanelScaffolder.DefaultProjectFolderLabels[i] + "必须是 Assets 下的工程相对路径。";
                }
            }

            if (!UIPanelScaffoldPaths.TryValidateNamespace(namespaceField.value, out _, out var namespaceError))
            {
                return namespaceError;
            }

            return null;
        }

        private void Create()
        {
            var options = new UIProjectCreateOptions
            {
                ProjectFolder = projectFolderField.value,
                ProjectName = nameField.value,
                RootNamespace = namespaceField.value,
                PanelConfigFolder = folderFields[0].value,
                PanelPrefabFolder = folderFields[1].value,
                PanelScriptFolder = folderFields[2].value,
                PanelVisualTreeFolder = folderFields[3].value,
                LayerConfigFolder = folderFields[4].value,
            };

            var result = UIPanelScaffolder.CreateProject(options);
            if (!result.Success)
            {
                statusLabel.text = result.ToStatusMessage();
                statusLabel.style.color = WarnColor;
                return;
            }

            var projectPath = result.CreatedPaths.Count > 0 ? result.CreatedPaths[0] : string.Empty;
            var project = AssetDatabase.LoadAssetAtPath<UIProjectConfigDataSo>(projectPath);
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
            var selected = Selection.activeObject;
            if (selected == null)
            {
                return DefaultRootFolder;
            }

            var path = AssetDatabase.GetAssetPath(selected);
            if (string.IsNullOrEmpty(path) || !AssetDatabase.IsValidFolder(path))
            {
                return DefaultRootFolder;
            }

            return UIPanelScaffoldPaths.NormalizeFolderPath(path) ?? DefaultRootFolder;
        }

        private static string DeriveNamespace(string projectName)
        {
            if (string.IsNullOrWhiteSpace(projectName))
            {
                return string.Empty;
            }

            var trimmed = projectName.Trim();
            var builder = new System.Text.StringBuilder(trimmed.Length);
            for (var i = 0; i < trimmed.Length; i++)
            {
                var character = trimmed[i];
                if (char.IsLetterOrDigit(character) || character == '_')
                {
                    builder.Append(character);
                }
            }

            if (builder.Length == 0)
            {
                return string.Empty;
            }

            if (!char.IsLetter(builder[0]) && builder[0] != '_')
            {
                builder.Insert(0, '_');
            }

            return builder.ToString();
        }
    }
}
