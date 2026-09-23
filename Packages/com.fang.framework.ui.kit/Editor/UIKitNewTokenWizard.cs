using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fang.Framework.UI.Kit.Editor
{
    /// <summary>新建 token：选类型 → 填该类型的字段 → 手填匹配 id（MatchId，可留空）→ 落成资产并登记进项目 SO。</summary>
    public sealed class UIKitNewTokenWizard : EditorWindow
    {
        private const string DefaultAssetName = "NewToken";
        private const float LabelWidth = 112f;

        private static readonly Color DimColor = new Color(0.65f, 0.65f, 0.65f);
        private static readonly Color WarnColor = new Color(1f, 0.6f, 0.4f);
        private static readonly Color LineColor = new Color(0.28f, 0.28f, 0.28f);

        /// <summary>这些是资产身份字段，由向导自己写，不给手填。</summary>
        private static readonly string[] IdentityFields = { "m_Script", "_id", "_displayName", "_description", "_matchId" };

        private static UIKitNewTokenWizard instance;

        private UIKitProjectSo project;
        private Action<TokenSo> onCreated;

        private readonly List<Type> tokenTypes = new List<Type>();

        private DropdownField typeField;
        private VisualElement fieldRows;
        private TextField matchIdField;
        private TextField nameField;
        private TextField folderField;
        private Label statusLabel;

        private Type selectedType;
        private TokenSo draft;
        private SerializedObject draftSerialized;

        public static void Open(UIKitProjectSo project, Action<TokenSo> onCreated)
        {
            if (instance != null)
            {
                instance.Focus();
                return;
            }

            var window = CreateInstance<UIKitNewTokenWizard>();
            instance = window;
            window.project = project;
            window.onCreated = onCreated;
            window.titleContent = new GUIContent("新建 token");
            window.minSize = new Vector2(600f, 480f);
            window.ShowUtility();
        }

        private void OnDisable()
        {
            DisposeDraft();

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

            var intro = new Label("建一个 token = 一份资产（一个属性值）+ 一个匹配 id。选类型后下面会列出该类型的字段，每一项都能改，改完点「创建」落位。");
            intro.style.whiteSpace = WhiteSpace.Normal;
            intro.style.color = DimColor;
            intro.style.marginBottom = 12f;
            form.Add(intro);

            CollectTokenTypes();
            typeField = new DropdownField("类型", BuildTypeChoices(), 0);
            typeField.RegisterValueChangedCallback(evt => SelectType(FindType(evt.newValue)));
            AddItem(form, "类型", typeField, "决定这份 token 改哪个属性、认哪种 target；列表来自工程里所有继承 TokenSo 的类。");

            fieldRows = new VisualElement();
            form.Add(fieldRows);

            matchIdField = new TextField();
            matchIdField.RegisterValueChangedCallback(_ => UpdateStatus());
            AddItem(form, "匹配 id", matchIdField, "手填的字符串（如 Text/Title）：预制体端 TokenMatch 的条目写同一个字符串来配对；留空表示这条 token 还没启用，之后可以在列表行里改。");

            nameField = new TextField { value = DefaultAssetName };
            nameField.RegisterValueChangedCallback(_ => UpdateStatus());
            AddItem(form, "资产名", nameField, "资产文件名，也是这份资产的 Id；必须是合法标识符。");

            folderField = new TextField { value = UIKitScaffoldPaths.ResolveDefaultFolder() };
            folderField.RegisterValueChangedCallback(_ => UpdateStatus());
            AddItem(form, "创建位置", BuildPickerRow(folderField, () => PickFolder(folderField)), "资产放这里，默认取当前选中资产所在目录。");

            root.Add(BuildFooter());

            SelectType(tokenTypes.Count > 0 ? tokenTypes[0] : null);
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

        private void CollectTokenTypes()
        {
            tokenTypes.Clear();

            var derived = TypeCache.GetTypesDerivedFrom<TokenSo>();
            for (var i = 0; i < derived.Count; i++)
            {
                var type = derived[i];
                if (type.IsAbstract || !type.IsClass || type.IsGenericTypeDefinition)
                {
                    continue;
                }

                tokenTypes.Add(type);
            }

            tokenTypes.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        }

        private List<string> BuildTypeChoices()
        {
            var choices = new List<string>();
            for (var i = 0; i < tokenTypes.Count; i++)
            {
                choices.Add(tokenTypes[i].Name);
            }

            return choices;
        }

        private Type FindType(string typeName)
        {
            for (var i = 0; i < tokenTypes.Count; i++)
            {
                if (tokenTypes[i].Name == typeName)
                {
                    return tokenTypes[i];
                }
            }

            return null;
        }

        private void SelectType(Type type)
        {
            selectedType = type;
            RebuildFieldRows();
            UpdateStatus();
        }

        private void RebuildFieldRows()
        {
            fieldRows.Clear();
            DisposeDraft();

            if (selectedType == null)
            {
                fieldRows.Add(BuildMessage("工程里没有继承 TokenSo 的类。"));
                return;
            }

            draft = ScriptableObject.CreateInstance(selectedType) as TokenSo;
            if (draft == null)
            {
                fieldRows.Add(BuildMessage("这个类型不是 TokenSo，跳过。"));
                return;
            }

            draftSerialized = new SerializedObject(draft);

            var property = draftSerialized.GetIterator();
            var enterChildren = true;

            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (IsIdentityField(property.name))
                {
                    continue;
                }

                var block = new VisualElement();
                block.style.flexShrink = 0f;
                block.style.marginBottom = 10f;

                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.flexShrink = 0f;

                var title = new Label(property.displayName);
                title.style.width = LabelWidth;
                title.style.flexShrink = 0f;
                row.Add(title);

                var field = new PropertyField(property.Copy());
                field.style.flexGrow = 1f;
                field.style.flexShrink = 1f;
                row.Add(field);

                block.Add(row);

                var tooltip = TooltipFor(selectedType, property.name);
                if (!string.IsNullOrEmpty(tooltip))
                {
                    var hint = new Label(tooltip);
                    hint.style.whiteSpace = WhiteSpace.Normal;
                    hint.style.color = DimColor;
                    hint.style.fontSize = 11f;
                    hint.style.marginLeft = LabelWidth;
                    hint.style.marginTop = 2f;
                    block.Add(hint);
                }

                fieldRows.Add(block);
            }

            fieldRows.Bind(draftSerialized);
        }

        private static bool IsIdentityField(string fieldName)
        {
            for (var i = 0; i < IdentityFields.Length; i++)
            {
                if (IdentityFields[i] == fieldName)
                {
                    return true;
                }
            }

            return false;
        }

        private static string TooltipFor(Type type, string fieldName)
        {
            var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
            {
                return null;
            }

            var tooltip = field.GetCustomAttribute<TooltipAttribute>();
            return tooltip == null ? null : tooltip.tooltip;
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

        private static Label BuildMessage(string text)
        {
            var label = new Label(text);
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.color = DimColor;
            return label;
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

            if (selectedType == null)
            {
                return "没有可选的 token 类型。";
            }

            if (!UIKitScaffoldPaths.TryValidateAssetName(nameField.value, "资产名", out _, out var nameError))
            {
                return nameError;
            }

            if (UIKitScaffoldPaths.NormalizeFolderPath(folderField.value) == null)
            {
                return "创建位置必须是 Assets 下的工程相对路径。";
            }

            var path = UIKitScaffoldPaths.TokenAssetPath(folderField.value, nameField.value.Trim());
            if (!string.IsNullOrEmpty(path) && AssetDatabase.LoadAssetAtPath<TokenSo>(path) != null)
            {
                return "资产已存在：" + path;
            }

            return null;
        }

        private void Create()
        {
            if (draft == null || draftSerialized == null)
            {
                statusLabel.text = "没有可创建的 token 类型。";
                statusLabel.style.color = WarnColor;
                return;
            }

            draftSerialized.ApplyModifiedPropertiesWithoutUndo();

            var result = UIKitProjectScaffolder.CreateToken(project, draft, folderField.value, nameField.value, SelectedMatchId());
            if (!result.Success)
            {
                statusLabel.text = result.ToStatusMessage();
                statusLabel.style.color = WarnColor;
                return;
            }

            var tokenPath = result.CreatedPaths.Count > 0 ? result.CreatedPaths[0] : string.Empty;
            var token = AssetDatabase.LoadAssetAtPath<TokenSo>(tokenPath);
            if (token != null)
            {
                Selection.activeObject = token;
                EditorGUIUtility.PingObject(token);
            }

            DisposeDraft();
            onCreated?.Invoke(token);
            Close();
        }

        private string SelectedMatchId()
        {
            return matchIdField == null || matchIdField.value == null ? string.Empty : matchIdField.value.Trim();
        }

        private void DisposeDraft()
        {
            if (draftSerialized != null)
            {
                draftSerialized.Dispose();
                draftSerialized = null;
            }

            if (draft != null)
            {
                DestroyImmediate(draft);
                draft = null;
            }
        }
    }
}
