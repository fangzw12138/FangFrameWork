using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fang.Framework.CommandConsole.Editor
{
    /// <summary>
    /// 新建调试项目配置的引导窗口：每一项都能手填、每项一句说明、内容可滚动、底栏固定。
    /// 与 <c>UIKitNewProjectWizard</c> 同一套惯例 —— 不接受一个空白保存框，也不接受建完再去 Inspector 补字段。
    /// </summary>
    public sealed class DebugNewProjectWizard : EditorWindow
    {
        private const string DefaultProjectName = "DebugProject";
        private const float LabelWidth = 116f;

        private static readonly Color DimColor = new Color(0.65f, 0.65f, 0.65f);
        private static readonly Color WarnColor = new Color(1f, 0.6f, 0.4f);
        private static readonly Color LineColor = new Color(0.28f, 0.28f, 0.28f);

        private static DebugNewProjectWizard instance;

        private Action<DebugProjectSo> onCreated;

        private TextField nameField;
        private TextField displayNameField;
        private TextField descriptionField;
        private TextField projectFolderField;
        private TextField listenAddressField;
        private IntegerField portField;
        private Toggle autoStartToggle;
        private Toggle consoleEnabledToggle;
        private EnumField consoleToggleKeyField;
        private TextField prefixesField;
        private TextField categoryOrderField;
        private TextField docFolderField;
        private TextField docFileNameField;
        private Toggle docSplitToggle;
        private Label statusLabel;

        public static void Open(Action<DebugProjectSo> onCreated)
        {
            if (instance != null)
            {
                instance.Focus();
                return;
            }

            var window = CreateInstance<DebugNewProjectWizard>();
            instance = window;
            window.onCreated = onCreated;
            window.titleContent = new GUIContent("新建调试项目");
            window.minSize = new Vector2(600f, 460f);
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

            var intro = new Label(
                "调试项目 = 一份项目配置 SO：运行参数 + 分类顺序 + 文档输出。"
                + "所有字段都留空也能跑（回落到内置默认值），改完点「创建」落位。");
            intro.style.whiteSpace = WhiteSpace.Normal;
            intro.style.color = DimColor;
            intro.style.marginBottom = 12f;
            form.Add(intro);

            BuildIdentitySection(form);
            BuildRuntimeSection(form);
            BuildScanSection(form);
            BuildDocSection(form);

            root.Add(BuildFooter());
            UpdateStatus();
        }

        private void BuildIdentitySection(VisualElement form)
        {
            form.Add(BuildGroupTitle("项目"));

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
            AddItem(form, "说明", descriptionField, "这套配置管什么，写给以后的自己看。");

            projectFolderField = new TextField { value = DebugScaffoldPaths.ResolveDefaultFolder() };
            projectFolderField.RegisterValueChangedCallback(_ => UpdateStatus());
            AddItem(
                form,
                "项目位置",
                BuildPickerRow(projectFolderField, () => PickFolder(projectFolderField)),
                "SO 放在这里；必须是 Assets 下的工程相对路径。");
        }

        private void BuildRuntimeSection(VisualElement form)
        {
            form.Add(BuildGroupTitle("运行参数"));

            listenAddressField = new TextField { value = DebugCommandDefaults.ListenAddress };
            listenAddressField.RegisterValueChangedCallback(_ => UpdateStatus());
            AddItem(form, "监听地址", listenAddressField, "必须是 IP（例如 127.0.0.1）；填主机名会启动失败。");

            portField = new IntegerField { value = DebugCommandDefaults.Port };
            portField.RegisterValueChangedCallback(_ => UpdateStatus());
            AddItem(form, "端口", portField, "1~65535；越界会回落到 " + DebugCommandDefaults.Port + "。");

            autoStartToggle = new Toggle { value = DebugCommandDefaults.AutoStart };
            autoStartToggle.RegisterValueChangedCallback(_ => UpdateStatus());
            AddItem(form, "自动启动", autoStartToggle, "进入游戏就监听；关掉就要自己调 StartTransport()。");

            consoleEnabledToggle = new Toggle { value = DebugCommandDefaults.ConsoleEnabled };
            consoleEnabledToggle.RegisterValueChangedCallback(_ => UpdateStatus());
            AddItem(form, "游戏内控制台", consoleEnabledToggle, "是否创建游戏内 IMGUI 控制台。");

            consoleToggleKeyField = new EnumField("快捷键", DebugCommandDefaults.ConsoleToggleKey);
            consoleToggleKeyField.RegisterValueChangedCallback(_ => UpdateStatus());
            AddItem(form, "控制台快捷键", consoleToggleKeyField, "游戏里按它开关控制台。");
        }

        private void BuildScanSection(VisualElement form)
        {
            form.Add(BuildGroupTitle("扫描"));

            prefixesField = new TextField { multiline = true };
            prefixesField.style.minHeight = 48f;
            prefixesField.RegisterValueChangedCallback(_ => UpdateStatus());
            AddItem(
                form,
                "程序集前缀",
                prefixesField,
                "一行一个。留空 = 只扫引用了本包的程序集（快得多，且扫到的指令一致）。");
        }

        private void BuildDocSection(VisualElement form)
        {
            form.Add(BuildGroupTitle("文档"));

            categoryOrderField = new TextField { multiline = true };
            categoryOrderField.style.minHeight = 48f;
            categoryOrderField.RegisterValueChangedCallback(_ => UpdateStatus());
            AddItem(
                form,
                "分类顺序",
                categoryOrderField,
                "一行一个分类名。留空 = 内置指令分类在最前、其余按名称排序；没列出的分类排在后面。");

            docFolderField = new TextField { value = DebugCommandDefaults.DocFolder };
            docFolderField.RegisterValueChangedCallback(_ => UpdateStatus());
            AddItem(form, "文档目录", docFolderField, "工程相对路径（例如 Docs），不存在会自动建。");

            docFileNameField = new TextField { value = DebugCommandDefaults.DocFileName };
            docFileNameField.RegisterValueChangedCallback(_ => UpdateStatus());
            AddItem(form, "文档文件名", docFileNameField, "总索引的文件名（例如 DebugCommands.md）。");

            docSplitToggle = new Toggle { value = DebugCommandDefaults.DocSplitByCategory };
            docSplitToggle.RegisterValueChangedCallback(_ => UpdateStatus());
            AddItem(form, "按分类拆文件", docSplitToggle, "每个分类再单独出一个文件；总索引始终生成。");
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

        private static Label BuildGroupTitle(string text)
        {
            var label = new Label(text);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginTop = 6f;
            label.style.marginBottom = 4f;
            return label;
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
            if (!string.IsNullOrEmpty(relative))
            {
                field.value = relative;
            }
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
            if (!DebugScaffoldPaths.TryValidateProjectName(nameField.value, out _, out var nameError))
            {
                return nameError;
            }

            if (DebugScaffoldPaths.NormalizeFolderPath(projectFolderField.value) == null)
            {
                return "项目位置必须是 Assets 下的工程相对路径。";
            }

            var port = portField.value;
            if (port < 1 || port > 65535)
            {
                return "端口必须在 1~65535 之间（越界会回落到 " + DebugCommandDefaults.Port + "）。";
            }

            if (string.IsNullOrWhiteSpace(listenAddressField.value))
            {
                return "监听地址不能为空（回落值是 " + DebugCommandDefaults.ListenAddress + "）。";
            }

            return null;
        }

        private void Create()
        {
            var problem = FindProblem();
            if (problem != null)
            {
                statusLabel.text = problem;
                statusLabel.style.color = WarnColor;
                return;
            }

            if (!DebugScaffoldPaths.TryValidateProjectName(nameField.value, out var projectName, out _))
            {
                return;
            }

            var folder = DebugScaffoldPaths.NormalizeFolderPath(projectFolderField.value);
            var path = DebugScaffoldPaths.ProjectAssetPath(folder, projectName);

            if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path)))
            {
                statusLabel.text = "已经存在同名资产：" + path;
                statusLabel.style.color = WarnColor;
                return;
            }

            if (!AssetDatabase.IsValidFolder(folder))
            {
                var parent = System.IO.Path.GetDirectoryName(folder)?.Replace('\\', '/');
                if (string.IsNullOrEmpty(parent) || !AssetDatabase.IsValidFolder(parent))
                {
                    statusLabel.text = "目录不存在，请先建好：" + folder;
                    statusLabel.style.color = WarnColor;
                    return;
                }

                AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folder));
            }

            var project = CreateProjectAsset(
                path,
                projectName,
                displayNameField.value,
                descriptionField.value);

            if (project != null)
            {
                Selection.activeObject = project;
                EditorGUIUtility.PingObject(project);
            }

            onCreated?.Invoke(project);
            Close();
        }

        private DebugProjectSo CreateProjectAsset(string path, string projectName, string displayName, string description)
        {
            var project = ScriptableObject.CreateInstance<DebugProjectSo>();
            var serialized = new SerializedObject(project);

            serialized.FindProperty("_id").stringValue = projectName;
            serialized.FindProperty("_displayName").stringValue = string.IsNullOrWhiteSpace(displayName)
                ? projectName
                : displayName.Trim();
            serialized.FindProperty("_description").stringValue = description ?? string.Empty;

            serialized.FindProperty("_autoStart").boolValue = autoStartToggle.value;
            serialized.FindProperty("_listenAddress").stringValue = listenAddressField.value.Trim();
            serialized.FindProperty("_port").intValue = portField.value;
            serialized.FindProperty("_consoleEnabled").boolValue = consoleEnabledToggle.value;
            serialized.FindProperty("_consoleToggleKey").intValue = (int)(KeyCode)consoleToggleKeyField.value;

            SetStringList(serialized.FindProperty("_commandAssemblyNamePrefixes"), SplitLines(prefixesField.value));
            SetStringList(serialized.FindProperty("_categoryOrder"), SplitLines(categoryOrderField.value));

            serialized.FindProperty("_docFolder").stringValue = docFolderField.value.Trim();
            serialized.FindProperty("_docFileName").stringValue = docFileNameField.value.Trim();
            serialized.FindProperty("_docSplitByCategory").boolValue = docSplitToggle.value;

            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(project, path);
            AssetDatabase.SaveAssets();

            return AssetDatabase.LoadAssetAtPath<DebugProjectSo>(path);
        }

        private static IReadOnlyList<string> SplitLines(string text)
        {
            var result = new List<string>();

            if (string.IsNullOrWhiteSpace(text))
            {
                return result;
            }

            var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(lines[i]))
                {
                    result.Add(lines[i].Trim());
                }
            }

            return result;
        }

        private static void SetStringList(SerializedProperty property, IReadOnlyList<string> values)
        {
            property.arraySize = values.Count;

            for (var i = 0; i < values.Count; i++)
            {
                property.GetArrayElementAtIndex(i).stringValue = values[i];
            }
        }
    }
}
