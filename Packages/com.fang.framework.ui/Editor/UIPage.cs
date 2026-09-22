using System.Collections.Generic;
using System.IO;
using Fang.Framework.Editor.Hub;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fang.Framework.UI.Editor
{
    [FangHubPage("framework-ui", "UI", "UI",
        Description = "UI 面板清单、定位与脚手架。", Order = 0)]
    public sealed class UIPage : IFangHubVisualElementPage
    {
        private const string StateScope = "framework-ui";
        private const string ProjectGuidKey = "ProjectGuid";
        private const string SearchKey = "Search";
        private const string SelectedPanelKey = "SelectedPanel";

        private const float ListPaneWidth = 260f;
        private const float DetailLabelWidth = 76f;

        private static readonly Color DimColor = new Color(0.65f, 0.65f, 0.65f);
        private static readonly Color WarnColor = new Color(1f, 0.6f, 0.4f);
        private static readonly Color LineColor = new Color(0.28f, 0.28f, 0.28f);
        private static readonly Color HoverColor = new Color(0.21f, 0.29f, 0.44f);
        private static readonly Color SelectedColor = new Color(0.24f, 0.37f, 0.59f);

        private readonly List<UIProjectConfigDataSo> projectChoices = new List<UIProjectConfigDataSo>();
        private readonly List<UIPanelScaffoldInfo> panelInfos = new List<UIPanelScaffoldInfo>();

        private ToolbarMenu projectMenu;
        private ToolbarButton validateButton;
        private UIProjectConfigDataSo selectedProject;
        private Label titleLabel;
        private Label summaryLabel;
        private TextField searchField;
        private Label statusLabel;
        private Label listHeaderLabel;
        private VisualElement panelList;
        private VisualElement detailContent;
        private string selectedPanelId;
        private SerializedObject inspectorSerializedObject;

        public void OnInitialize(FangHubWindow window)
        {
        }

        public void OnSelected()
        {
            RebuildProjectChoices();
            Refresh();
        }

        public VisualElement CreateVisualElement()
        {
            var root = new VisualElement();
            root.style.flexGrow = 1f;

            selectedPanelId = FangHubPageState.GetString(StateScope, SelectedPanelKey, string.Empty);

            root.Add(BuildMenuBar());
            root.Add(BuildTitleRow());
            root.Add(BuildSplit());

            UIPanelPrefabQueue.PrefabCreated += OnPrefabCreated;
            root.RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                UIPanelPrefabQueue.PrefabCreated -= OnPrefabCreated;
                DisposeInspector();
            });

            RebuildProjectChoices();
            Refresh();
            return root;
        }

        private void OnPrefabCreated()
        {
            if (panelList == null || panelList.panel == null)
            {
                return;
            }

            Refresh();
        }

        private VisualElement BuildMenuBar()
        {
            var toolbar = new Toolbar();
            toolbar.style.marginBottom = 4f;

            projectMenu = new ToolbarMenu { text = "UI 项目" };
            toolbar.Add(projectMenu);

            validateButton = new ToolbarButton(Validate) { text = "校验" };
            toolbar.Add(validateButton);
            toolbar.Add(new ToolbarButton(RefreshProjects) { text = "刷新" });

            var spacer = new VisualElement();
            spacer.style.flexGrow = 1f;
            toolbar.Add(spacer);

            statusLabel = new Label();
            statusLabel.style.color = DimColor;
            statusLabel.style.whiteSpace = WhiteSpace.NoWrap;
            statusLabel.style.overflow = Overflow.Hidden;
            statusLabel.style.marginRight = 6f;
            toolbar.Add(statusLabel);

            return toolbar;
        }

        private VisualElement BuildTitleRow()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = 4f;
            row.style.marginBottom = 4f;

            titleLabel = new Label();
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.fontSize = 14f;
            row.Add(titleLabel);

            return row;
        }

        private VisualElement BuildListHeader()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.flexShrink = 0f;
            row.style.marginTop = 4f;

            listHeaderLabel = new Label();
            listHeaderLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            listHeaderLabel.style.flexShrink = 0f;
            row.Add(listHeaderLabel);

            var addButton = new Button(OpenNewPanelWizard) { text = "+" };
            addButton.tooltip = "新建面板";
            addButton.style.width = 22f;
            addButton.style.height = 20f;
            addButton.style.flexShrink = 0f;
            addButton.style.marginLeft = 4f;
            addButton.style.paddingLeft = 0f;
            addButton.style.paddingRight = 0f;
            row.Add(addButton);

            searchField = new TextField
            {
                value = FangHubPageState.GetString(StateScope, SearchKey, string.Empty)
            };
            searchField.style.flexGrow = 1f;
            searchField.style.flexShrink = 1f;
            searchField.style.marginLeft = 6f;
            searchField.style.marginRight = 0f;
            searchField.textEdition.placeholder = "搜索面板…";
            searchField.RegisterValueChangedCallback(evt =>
            {
                FangHubPageState.SetString(StateScope, SearchKey, evt.newValue);
                RebuildPanelList();
            });
            row.Add(searchField);

            return row;
        }

        private VisualElement BuildSplit()
        {
            var split = new VisualElement();
            split.style.flexDirection = FlexDirection.Row;
            split.style.flexGrow = 1f;
            split.style.alignItems = Align.Stretch;

            split.Add(BuildListPane());
            split.Add(BuildDetailPane());
            return split;
        }

        private VisualElement BuildListPane()
        {
            var pane = new VisualElement();
            pane.style.width = ListPaneWidth;
            pane.style.flexShrink = 0f;
            pane.style.borderRightWidth = 1f;
            pane.style.borderRightColor = LineColor;
            pane.style.paddingLeft = 6f;
            pane.style.paddingRight = 6f;

            pane.Add(BuildListHeader());

            summaryLabel = new Label();
            summaryLabel.style.whiteSpace = WhiteSpace.Normal;
            summaryLabel.style.color = DimColor;
            summaryLabel.style.fontSize = 10f;
            summaryLabel.style.marginBottom = 2f;
            pane.Add(summaryLabel);

            var scroll = new ScrollView();
            scroll.style.flexGrow = 1f;
            panelList = scroll.contentContainer;
            pane.Add(scroll);

            return pane;
        }

        private VisualElement BuildDetailPane()
        {
            var pane = new VisualElement();
            pane.style.flexGrow = 1f;
            pane.style.paddingLeft = 12f;
            pane.style.paddingRight = 8f;

            var scroll = new ScrollView();
            scroll.style.flexGrow = 1f;
            detailContent = scroll.contentContainer;
            pane.Add(scroll);

            return pane;
        }

        private void RefreshProjects()
        {
            RebuildProjectChoices();
            Refresh();
        }

        private void RebuildProjectChoices()
        {
            if (projectMenu == null)
            {
                return;
            }

            var selectedGuid = FangHubPageState.GetString(StateScope, ProjectGuidKey);
            projectChoices.Clear();

            var paths = new List<string>();
            var guids = AssetDatabase.FindAssets("t:" + nameof(UIProjectConfigDataSo));
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!string.IsNullOrEmpty(path))
                {
                    paths.Add(path);
                }
            }

            paths.Sort(System.StringComparer.Ordinal);

            selectedProject = null;
            for (var i = 0; i < paths.Count; i++)
            {
                var project = AssetDatabase.LoadAssetAtPath<UIProjectConfigDataSo>(paths[i]);
                if (project == null)
                {
                    continue;
                }

                projectChoices.Add(project);

                if (AssetDatabase.AssetPathToGUID(paths[i]) == selectedGuid)
                {
                    selectedProject = project;
                }
            }

            RebuildProjectMenu();
        }

        private void RebuildProjectMenu()
        {
            var menu = projectMenu.menu;
            menu.ClearItems();

            menu.AppendAction("新建 UI 项目配置 SO…", _ => CreateProject());
            menu.AppendSeparator();

            if (projectChoices.Count == 0)
            {
                menu.AppendAction("（工程里还没有 UI 项目）", null, DropdownMenuAction.Status.Disabled);
            }
            else
            {
                for (var i = 0; i < projectChoices.Count; i++)
                {
                    var project = projectChoices[i];
                    menu.AppendAction(
                        BuildProjectLabel(projectChoices, i),
                        _ => SelectProject(project),
                        _ => project == selectedProject ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
                }
            }
        }

        private static string BuildProjectLabel(List<UIProjectConfigDataSo> projects, int index)
        {
            var project = projects[index];
            var name = project.name;

            var duplicated = false;
            for (var i = 0; i < projects.Count; i++)
            {
                if (i != index && projects[i].name == name)
                {
                    duplicated = true;
                    break;
                }
            }

            if (!duplicated)
            {
                return name;
            }

            return name + "（" + AssetDatabase.GetAssetPath(project) + "）";
        }

        private void SelectProject(UIProjectConfigDataSo project)
        {
            selectedProject = project;
            FangHubPageState.SetString(StateScope, ProjectGuidKey, GetAssetGuid(project));
            RebuildProjectMenu();
            Refresh();
        }

        private void Refresh()
        {
            if (panelList == null)
            {
                return;
            }

            panelInfos.Clear();

            var project = SelectedProject;
            titleLabel.text = project == null ? "当前的项目：未选择" : "当前的项目：" + project.name;

            if (validateButton != null)
            {
                validateButton.SetEnabled(project != null);
            }

            if (project == null)
            {
                summaryLabel.text = string.Empty;
                RebuildPanelList();
                RebuildDetail();
                return;
            }

            panelInfos.AddRange(UIPanelScaffolder.ScanPanels(project));
            summaryLabel.text = BuildPanelSummary();

            RebuildPanelList();
            RebuildDetail();
        }

        private string BuildPanelSummary()
        {
            var incomplete = 0;
            for (var i = 0; i < panelInfos.Count; i++)
            {
                if (!panelInfos[i].IsComplete)
                {
                    incomplete++;
                }
            }

            return panelInfos.Count + " 个面板，" + incomplete + " 个缺引用。";
        }

        private void RebuildPanelList()
        {
            if (panelList == null)
            {
                return;
            }

            panelList.Clear();

            var search = searchField == null || searchField.value == null ? string.Empty : searchField.value.Trim();
            var shown = 0;

            for (var i = 0; i < panelInfos.Count; i++)
            {
                var info = panelInfos[i];
                if (search.Length > 0 && !Matches(info, search))
                {
                    continue;
                }

                panelList.Add(BuildPanelRow(info));
                shown++;
            }

            listHeaderLabel.text = search.Length > 0
                ? "面板列表（" + shown + "/" + panelInfos.Count + "）"
                : "面板列表（" + panelInfos.Count + "）";

            if (panelInfos.Count == 0)
            {
                panelList.Add(BuildMessage(SelectedProject == null
                    ? "用菜单栏「UI 项目」新建一个，或直接选工程里已有的项目。"
                    : "这个 UI 项目还没有面板，用上面的「生成」新增。"));
            }
            else if (shown == 0)
            {
                panelList.Add(BuildMessage("没有匹配的面板。"));
            }
        }

        private VisualElement BuildPanelRow(UIPanelScaffoldInfo info)
        {
            var selected = !string.IsNullOrEmpty(selectedPanelId) && info.PanelId == selectedPanelId;

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.paddingLeft = 6f;
            row.style.paddingRight = 6f;
            row.style.paddingTop = 3f;
            row.style.paddingBottom = 3f;
            row.style.backgroundColor = selected ? SelectedColor : Color.clear;

            var mark = new Label(info.IsComplete ? "✓" : "✗");
            mark.style.width = 14f;
            mark.style.flexShrink = 0f;
            mark.style.color = info.IsComplete ? Color.white : WarnColor;
            row.Add(mark);

            var text = new VisualElement();
            text.style.flexGrow = 1f;
            text.style.overflow = Overflow.Hidden;

            var name = new Label(info.DisplayName);
            name.style.unityFontStyleAndWeight = FontStyle.Bold;
            name.style.overflow = Overflow.Hidden;
            text.Add(name);

            var detail = new Label(BuildRowDetail(info));
            detail.style.color = DimColor;
            detail.style.fontSize = 10f;
            detail.style.overflow = Overflow.Hidden;
            text.Add(detail);

            row.Add(text);

            if (!selected)
            {
                row.RegisterCallback<MouseEnterEvent>(_ => row.style.backgroundColor = HoverColor);
                row.RegisterCallback<MouseLeaveEvent>(_ => row.style.backgroundColor = Color.clear);
            }

            row.RegisterCallback<ClickEvent>(_ => SelectPanel(info.PanelId));
            return row;
        }

        private static string BuildRowDetail(UIPanelScaffoldInfo info)
        {
            var layer = string.IsNullOrWhiteSpace(info.LayerId) ? "未设层" : info.LayerId;
            return info.PanelId + "  ·  " + layer + "  ·  " + info.TrackName;
        }

        private void SelectPanel(string panelId)
        {
            selectedPanelId = panelId ?? string.Empty;
            FangHubPageState.SetString(StateScope, SelectedPanelKey, selectedPanelId);
            RebuildPanelList();
            RebuildDetail();
        }

        private void RebuildDetail()
        {
            if (detailContent == null)
            {
                return;
            }

            DisposeInspector();
            detailContent.Clear();

            var info = FindSelectedInfo();
            if (info == null)
            {
                detailContent.Add(BuildMessage(panelInfos.Count == 0
                    ? "还没有可查看的面板。"
                    : "在左侧列表里选一个面板，右侧显示它的详情与配置。"));
                return;
            }

            detailContent.Add(BuildDetailHeader(info));

            var basicSection = BuildCollapsibleSection("基本信息", false);
            basicSection.Add(BuildFieldRow("Id", info.PanelId));
            basicSection.Add(BuildFieldRow("显示名", info.DisplayName));
            basicSection.Add(BuildFieldRow("层", string.IsNullOrWhiteSpace(info.LayerId) ? "（未设置）" : info.LayerId));
            basicSection.Add(BuildFieldRow("轨", info.TrackName));

            if (info.Config != null)
            {
                basicSection.Add(BuildFieldRow("排序偏移", info.Config.SortOrder.ToString()));
                basicSection.Add(BuildFieldRow("拦截输入", info.Config.BlocksInput ? "是" : "否"));
                basicSection.Add(BuildFieldRow("关闭上一层", info.Config.ClosePreviousOnOpen ? "是" : "否"));
            }

            detailContent.Add(basicSection);

            detailContent.Add(BuildSection("资产"));
            detailContent.Add(BuildPathRow("配置 SO", info.ConfigPath));
            detailContent.Add(BuildPathRow("控制器脚本", info.Paths.ControllerScriptPath));
            detailContent.Add(BuildPathRow("Data 脚本", info.Paths.DataScriptPath));

            if (info.IsVisualTree)
            {
                detailContent.Add(BuildPathRow("UXML", info.Paths.UxmlPath));
                detailContent.Add(BuildPathRow("USS", info.Paths.UssPath));
            }
            else
            {
                detailContent.Add(BuildPathRow("预制体", info.Paths.PrefabPath));
            }

            detailContent.Add(BuildPathRow("脚本目录", info.Paths.ScriptFolderPath));

            detailContent.Add(BuildSection("完整性"));
            detailContent.Add(BuildIntegrity(info));

            detailContent.Add(BuildSection("操作"));
            detailContent.Add(BuildActionRow(info));

            detailContent.Add(BuildSection("面板配置（可直接编辑）"));
            detailContent.Add(BuildInspector(info));
        }

        private static VisualElement BuildDetailHeader(UIPanelScaffoldInfo info)
        {
            var header = new VisualElement();

            var title = new Label(info.DisplayName);
            title.style.fontSize = 14f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(title);

            var path = new Label(string.IsNullOrEmpty(info.ConfigPath) ? "（配置资产路径未知）" : info.ConfigPath);
            path.style.color = DimColor;
            path.style.whiteSpace = WhiteSpace.Normal;
            header.Add(path);

            return header;
        }

        private VisualElement BuildIntegrity(UIPanelScaffoldInfo info)
        {
            var missing = info.GetMissingItems();
            if (missing.Count == 0)
            {
                var ok = new Label("✓ 完整");
                ok.style.color = Color.white;
                return ok;
            }

            var bad = new Label("✗ 缺失：" + string.Join("、", missing));
            bad.style.color = WarnColor;
            bad.style.whiteSpace = WhiteSpace.Normal;
            return bad;
        }

        private VisualElement BuildActionRow(UIPanelScaffoldInfo info)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;

            row.Add(new Button(() => Ping(info.ConfigPath)) { text = "定位 SO" });
            row.Add(new Button(() => Ping(info.Paths.ControllerScriptPath)) { text = "定位脚本" });
            row.Add(new Button(() => OpenFolder(info.Paths.ScriptFolderPath)) { text = "打开目录" });

            if (!info.IsVisualTree)
            {
                row.Add(new Button(() => CreatePrefab(info)) { text = "创建预制体" });
            }

            row.Add(new Button(() => DeletePanel(info)) { text = "删除" });
            return row;
        }

        private VisualElement BuildInspector(UIPanelScaffoldInfo info)
        {
            if (info.Config == null)
            {
                return BuildMessage("面板配置为空引用。");
            }

            inspectorSerializedObject = new SerializedObject(info.Config);
            var inspector = new InspectorElement(inspectorSerializedObject);
            inspector.style.marginTop = 2f;
            return inspector;
        }

        private void DisposeInspector()
        {
            if (inspectorSerializedObject == null)
            {
                return;
            }

            inspectorSerializedObject.Dispose();
            inspectorSerializedObject = null;
        }

        private UIPanelScaffoldInfo FindSelectedInfo()
        {
            if (string.IsNullOrEmpty(selectedPanelId))
            {
                return null;
            }

            for (var i = 0; i < panelInfos.Count; i++)
            {
                if (panelInfos[i].PanelId == selectedPanelId)
                {
                    return panelInfos[i];
                }
            }

            return null;
        }

        private static VisualElement BuildFieldRow(string label, string value)
        {
            var row = BuildDetailRow(label);
            row.Add(BuildDetailValue(string.IsNullOrEmpty(value) ? "（空）" : value));
            return row;
        }

        private static VisualElement BuildPathRow(string label, string assetPath)
        {
            var row = BuildDetailRow(label);

            var exists = !string.IsNullOrEmpty(assetPath) && AssetDatabase.LoadAssetAtPath<Object>(assetPath) != null;
            var value = BuildDetailValue(string.IsNullOrEmpty(assetPath) ? "（未填写）" : assetPath);
            if (!exists)
            {
                value.style.color = WarnColor;
            }

            row.Add(value);

            if (exists)
            {
                row.Add(new Button(() => Ping(assetPath)) { text = "定位" });
            }

            return row;
        }

        private static VisualElement BuildDetailRow(string label)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            var name = new Label(label);
            name.style.width = DetailLabelWidth;
            name.style.flexShrink = 0f;
            name.style.color = DimColor;
            row.Add(name);

            return row;
        }

        private static Label BuildDetailValue(string text)
        {
            var value = new Label(text);
            value.style.flexGrow = 1f;
            value.style.whiteSpace = WhiteSpace.Normal;
            value.style.color = DimColor;
            return value;
        }

        private static Label BuildSection(string title)
        {
            var label = new Label(title);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginTop = 8f;
            label.style.marginBottom = 2f;
            return label;
        }

        private static Foldout BuildCollapsibleSection(string title, bool expanded)
        {
            var foldout = new Foldout { text = title, value = expanded };
            foldout.style.marginTop = 8f;

            var header = foldout.Q<Toggle>();
            if (header != null && header.labelElement != null)
            {
                header.labelElement.style.unityFontStyleAndWeight = FontStyle.Bold;
            }

            return foldout;
        }

        private static bool Matches(UIPanelScaffoldInfo info, string search)
        {
            var id = info.PanelId ?? string.Empty;
            var displayName = info.DisplayName ?? string.Empty;
            var layerId = info.LayerId ?? string.Empty;

            return id.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) >= 0
                || displayName.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) >= 0
                || layerId.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void CreateProject()
        {
            UINewProjectWizard.Open(project =>
            {
                if (project == null)
                {
                    return;
                }

                RebuildProjectChoices();
                SelectProject(project);
                SetStatus("UI 项目已创建：" + AssetDatabase.GetAssetPath(project));
            });
        }

        private void OpenNewPanelWizard()
        {
            var project = SelectedProject;
            if (project == null)
            {
                SetStatus("未选择 UI 项目。");
                return;
            }

            UINewPanelWizard.Open(project, config =>
            {
                Refresh();

                if (config == null)
                {
                    return;
                }

                SelectPanel(config.Id);
                SetStatus("面板已生成：" + config.Id);
            });
        }

        private void CreatePrefab(UIPanelScaffoldInfo info)
        {
            var project = SelectedProject;
            if (project == null)
            {
                SetStatus("未选择 UI 项目。");
                return;
            }

            var result = UIPanelScaffolder.CreatePrefab(project, info.Paths.PanelName);
            SetStatus(result.ToStatusMessage());
            Refresh();
        }

        private void DeletePanel(UIPanelScaffoldInfo info)
        {
            var project = SelectedProject;
            if (project == null)
            {
                SetStatus("未选择 UI 项目。");
                return;
            }

            var preview = UIPanelScaffolder.GetDeletionPreview(project, info.Config);
            var confirmed = EditorUtility.DisplayDialog(
                "确认删除面板",
                "以下内容将被删除：\n\n" + string.Join("\n", preview),
                "删除",
                "取消");

            if (!confirmed)
            {
                return;
            }

            var result = UIPanelScaffolder.DeletePanel(project, info.Config);
            SetStatus(result.ToStatusMessage());

            if (info.PanelId == selectedPanelId)
            {
                SelectPanel(string.Empty);
                return;
            }

            Refresh();
        }

        private void Validate()
        {
            var project = SelectedProject;
            if (project == null)
            {
                SetStatus("未选择 UI 项目。");
                return;
            }

            UIValidationWindow.Open(project.name, BuildPanelSummary(), UIPanelScaffolder.ValidateProject(project));
        }

        private void SetStatus(string message)
        {
            if (statusLabel == null)
            {
                return;
            }

            statusLabel.text = message;
        }

        private UIProjectConfigDataSo SelectedProject => selectedProject;

        private static string GetAssetGuid(UIProjectConfigDataSo project)
        {
            if (project == null)
            {
                return string.Empty;
            }

            return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(project));
        }

        private static void Ping(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            if (asset == null)
            {
                return;
            }

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private static void OpenFolder(string assetFolderPath)
        {
            if (string.IsNullOrEmpty(assetFolderPath) || !AssetDatabase.IsValidFolder(assetFolderPath))
            {
                return;
            }

            EditorUtility.RevealInFinder(Path.GetFullPath(assetFolderPath));
        }

        private static Label BuildMessage(string text)
        {
            var label = new Label(text);
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.color = DimColor;
            return label;
        }
    }
}
