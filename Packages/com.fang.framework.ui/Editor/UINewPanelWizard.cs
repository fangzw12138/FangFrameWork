using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fang.Framework.UI.Editor
{
    public sealed class UINewPanelWizard : EditorWindow
    {
        private const string DefaultPanelName = "NewPanel";
        private const float LabelWidth = 112f;
        private const string NoLayerText = "（不设层）";

        private static readonly Color DimColor = new Color(0.65f, 0.65f, 0.65f);
        private static readonly Color WarnColor = new Color(1f, 0.6f, 0.4f);
        private static readonly Color LineColor = new Color(0.28f, 0.28f, 0.28f);

        private static UINewPanelWizard instance;

        private readonly List<string> layerIds = new List<string>();

        private UIProjectConfigDataSo project;
        private Action<UIPanelConfigDataSo> onCreated;
        private TextField nameField;
        private DropdownField trackField;
        private DropdownField layerField;
        private IntegerField sortOrderField;
        private Toggle blocksInputField;
        private Toggle closePreviousField;
        private Label statusLabel;
        private Label previewLabel;

        public static void Open(UIProjectConfigDataSo project, Action<UIPanelConfigDataSo> onCreated)
        {
            if (project == null)
            {
                return;
            }

            if (instance != null)
            {
                instance.Focus();
                return;
            }

            var window = CreateInstance<UINewPanelWizard>();
            instance = window;
            window.project = project;
            window.onCreated = onCreated;
            window.titleContent = new GUIContent("新建面板");
            window.minSize = new Vector2(560f, 520f);
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
                "建一个面板 = 一份面板 SO + 控制器脚本 + Data 脚本（UGUI 另加预制体，UITK 另加 UXML·USS）。" +
                "落位目录来自 UI 项目「" + project.name + "」的配置。");
            intro.style.whiteSpace = WhiteSpace.Normal;
            intro.style.color = DimColor;
            intro.style.marginBottom = 12f;
            form.Add(intro);

            nameField = new TextField { value = DefaultPanelName };
            nameField.RegisterValueChangedCallback(_ =>
            {
                UpdatePreview();
                UpdateStatus();
            });
            AddItem(form, "面板名称", nameField, "面板 Id、SO 资产名与脚本前缀：{面板名}ConfigData.asset、{面板名}Controller.cs。");

            trackField = new DropdownField(new List<string> { "UGUI（预制体轨）", "UITK（UXML·USS 轨）" }, 0);
            trackField.RegisterValueChangedCallback(_ =>
            {
                UpdatePreview();
                UpdateStatus();
            });
            AddItem(form, "轨", trackField, "UGUI 生成 {面板名}.prefab；UITK 生成 {面板名}.uxml 与 .uss，需要 PanelSettings。");

            layerField = new DropdownField(BuildLayerChoices(), 0);
            layerField.RegisterValueChangedCallback(_ => UpdateStatus());
            AddItem(form, "层", layerField, "面板归属的 UI 层，来自 UI 项目的层清单；层决定排序基准。");

            sortOrderField = new IntegerField { value = 0 };
            AddItem(form, "排序偏移", sortOrderField, "同层内再叠加的排序值，越大越靠上层。");

            blocksInputField = new Toggle { value = false };
            AddItem(form, "拦截输入", blocksInputField, "面板打开时是否拦截下层输入。");

            closePreviousField = new Toggle { value = false };
            AddItem(form, "关闭上一层", closePreviousField, "打开这个面板时是否先关闭上一层。");

            form.Add(BuildPreviewBlock());

            root.Add(BuildFooter());
            UpdatePreview();
            UpdateStatus();
        }

        private VisualElement BuildPreviewBlock()
        {
            var block = new VisualElement();
            block.style.flexShrink = 0f;

            var title = new Label("将创建");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginTop = 4f;
            title.style.marginBottom = 2f;
            block.Add(title);

            previewLabel = new Label();
            previewLabel.style.whiteSpace = WhiteSpace.Normal;
            previewLabel.style.color = DimColor;
            previewLabel.style.fontSize = 11f;
            block.Add(previewLabel);

            return block;
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
            buttons.Add(new Button(Create) { text = "生成" });
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

        private List<string> BuildLayerChoices()
        {
            layerIds.Clear();
            layerIds.Add(string.Empty);

            var choices = new List<string> { NoLayerText };
            for (var i = 0; i < project.Layers.Count; i++)
            {
                var layer = project.Layers[i];
                if (layer == null)
                {
                    continue;
                }

                layerIds.Add(layer.Id);
                choices.Add(string.IsNullOrWhiteSpace(layer.DisplayName)
                    ? layer.Id
                    : layer.DisplayName + "（" + layer.Id + "）");
            }

            return choices;
        }

        private string SelectedLayerId()
        {
            var index = layerField.index;
            return index >= 0 && index < layerIds.Count ? layerIds[index] : string.Empty;
        }

        private void UpdatePreview()
        {
            if (previewLabel == null)
            {
                return;
            }

            if (!UIPanelScaffoldPaths.TryValidatePanelName(nameField.value, out var panelName, out _))
            {
                previewLabel.text = "（面板名称合法后显示要落位的文件）";
                return;
            }

            var paths = new UIPanelScaffoldPaths(project, panelName);
            if (string.IsNullOrEmpty(paths.ConfigPath))
            {
                previewLabel.text = "（UI 项目的目录没配全，先到项目 SO 里补目录）";
                return;
            }

            var lines = new List<string>
            {
                "配置 SO：" + paths.ConfigPath,
                "控制器脚本：" + paths.ControllerScriptPath,
                "Data 脚本：" + paths.DataScriptPath,
            };

            lines.Add(trackField.index == 1
                ? "UXML·USS：" + paths.UxmlPath
                : "预制体：" + paths.PrefabPath);

            previewLabel.text = string.Join("\n", lines);
        }

        private void UpdateStatus()
        {
            if (statusLabel == null)
            {
                return;
            }

            var problem = FindProblem();
            statusLabel.text = problem ?? "可以生成。";
            statusLabel.style.color = problem == null ? DimColor : WarnColor;
        }

        private string FindProblem()
        {
            if (project == null)
            {
                return "未选择 UI 项目。";
            }

            if (!UIPanelScaffoldPaths.TryValidateProjectFolders(project, out var folderErrors))
            {
                return folderErrors.Count > 0 ? folderErrors[0] : "UI 项目的目录未配置完整。";
            }

            if (!UIPanelScaffoldPaths.TryValidatePanelName(nameField.value, out var panelName, out var nameError))
            {
                return nameError;
            }

            var paths = new UIPanelScaffoldPaths(project, panelName);
            if (AssetDatabase.LoadAssetAtPath<UIPanelConfigDataSo>(paths.ConfigPath) != null)
            {
                return "面板已存在：" + paths.ConfigPath;
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

            UIPanelScaffoldPaths.TryValidatePanelName(nameField.value, out var panelName, out _);

            var options = new UIPanelCreateOptions
            {
                PanelName = panelName,
                Track = trackField.index == 1 ? UIPanelTrack.VisualTree : UIPanelTrack.Prefab,
                LayerId = SelectedLayerId(),
                SortOrder = sortOrderField.value,
                BlocksInput = blocksInputField.value,
                ClosePreviousOnOpen = closePreviousField.value,
            };

            var result = UIPanelScaffolder.CreatePanel(project, options);
            if (!result.Success)
            {
                statusLabel.text = result.ToStatusMessage();
                statusLabel.style.color = WarnColor;
                return;
            }

            var config = AssetDatabase.LoadAssetAtPath<UIPanelConfigDataSo>(new UIPanelScaffoldPaths(project, panelName).ConfigPath);
            if (config != null)
            {
                Selection.activeObject = config;
                EditorGUIUtility.PingObject(config);
            }

            onCreated?.Invoke(config);
            Close();
        }
    }
}
