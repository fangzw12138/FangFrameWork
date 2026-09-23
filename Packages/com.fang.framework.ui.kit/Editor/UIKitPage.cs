using System;
using System.Collections.Generic;
using System.Text;
using Fang.Framework.Editor.Hub;
using Fang.Framework.UI.Kit.Internal;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fang.Framework.UI.Kit.Editor
{
    /// <summary>
    /// FangHub「控件库」页：登记控件预制体与主题 token，校验、统计并执行应用。
    /// </summary>
    [FangHubPage("framework-ui-kit", "控件库", "UI",
        Description = "UGUI 控件库与主题 token 的登记、校验与应用。", Order = 1)]
    public sealed class UIKitPage : IFangHubVisualElementPage
    {
        private const string StateScope = "framework-ui-kit";
        private const string ProjectGuidKey = "ProjectGuid";
        private const string LibraryKey = "Library";
        private const string SearchPrefabKey = "Search.Prefab";
        private const string SearchTokenKey = "Search.Token";
        private const string SelectedPrefabKey = "SelectedPrefab";
        private const string SelectedTokenKey = "SelectedToken";

        private const string LibraryPrefab = "Prefab";
        private const string LibraryToken = "Token";

        private const string NotEnabledText = "（未启用）";

        private const float ListPaneWidth = 260f;
        private const float DetailLabelWidth = 92f;

        private static readonly Color DimColor = new Color(0.65f, 0.65f, 0.65f);
        private static readonly Color WarnColor = new Color(1f, 0.6f, 0.4f);
        private static readonly Color LineColor = new Color(0.28f, 0.28f, 0.28f);
        private static readonly Color HoverColor = new Color(0.21f, 0.29f, 0.44f);
        private static readonly Color SelectedColor = new Color(0.24f, 0.37f, 0.59f);
        private static readonly Color TabActiveColor = new Color(0.24f, 0.37f, 0.59f);

        private readonly List<UIKitProjectSo> projectChoices = new List<UIKitProjectSo>();

        private ToolbarMenu projectMenu;
        private ToolbarButton statisticsButton;
        private ToolbarButton validateButton;
        private ToolbarButton applyAllButton;
        private UIKitProjectSo selectedProject;

        private Label titleLabel;
        private Label statusLabel;
        private ToolbarButton prefabTab;
        private ToolbarButton tokenTab;
        private Label listHeaderLabel;
        private Label summaryLabel;
        private TextField searchField;
        private VisualElement listContainer;
        private VisualElement detailContent;

        private string library = LibraryPrefab;
        private string searchPrefab = string.Empty;
        private string searchToken = string.Empty;
        private string selectedPrefabPath = string.Empty;
        private string selectedTokenPath = string.Empty;
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

            library = FangHubPageState.GetString(StateScope, LibraryKey, LibraryPrefab);
            if (library != LibraryToken)
            {
                library = LibraryPrefab;
            }

            searchPrefab = FangHubPageState.GetString(StateScope, SearchPrefabKey);
            searchToken = FangHubPageState.GetString(StateScope, SearchTokenKey);
            selectedPrefabPath = FangHubPageState.GetString(StateScope, SelectedPrefabKey);
            selectedTokenPath = FangHubPageState.GetString(StateScope, SelectedTokenKey);

            root.Add(BuildMenuBar());
            root.Add(BuildTitleRow());
            root.Add(BuildSplit());

            root.RegisterCallback<DetachFromPanelEvent>(_ => DisposeInspector());

            RebuildProjectChoices();
            Refresh();
            return root;
        }

        // ---------- 骨架 ----------

        private VisualElement BuildMenuBar()
        {
            var toolbar = new Toolbar();
            toolbar.style.marginBottom = 4f;

            projectMenu = new ToolbarMenu { text = "控件库项目" };
            toolbar.Add(projectMenu);

            statisticsButton = new ToolbarButton(ShowStatistics) { text = "统计" };
            toolbar.Add(statisticsButton);

            validateButton = new ToolbarButton(Validate) { text = "校验" };
            toolbar.Add(validateButton);

            applyAllButton = new ToolbarButton(() => ApplyOne(null)) { text = "全体应用" };
            toolbar.Add(applyAllButton);

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

            pane.Add(BuildLibraryTabs());
            pane.Add(BuildListHeader());

            summaryLabel = new Label();
            summaryLabel.style.whiteSpace = WhiteSpace.Normal;
            summaryLabel.style.color = DimColor;
            summaryLabel.style.fontSize = 10f;
            summaryLabel.style.marginBottom = 2f;
            pane.Add(summaryLabel);

            var scroll = new ScrollView();
            scroll.style.flexGrow = 1f;
            listContainer = scroll.contentContainer;
            pane.Add(scroll);

            return pane;
        }

        private VisualElement BuildLibraryTabs()
        {
            var toolbar = new Toolbar();
            toolbar.style.marginTop = 4f;
            toolbar.style.marginBottom = 2f;

            prefabTab = new ToolbarButton(() => SelectLibrary(LibraryPrefab)) { text = "组件库" };
            prefabTab.style.flexGrow = 1f;
            prefabTab.style.unityTextAlign = TextAnchor.MiddleCenter;
            toolbar.Add(prefabTab);

            tokenTab = new ToolbarButton(() => SelectLibrary(LibraryToken)) { text = "token 库" };
            tokenTab.style.flexGrow = 1f;
            tokenTab.style.unityTextAlign = TextAnchor.MiddleCenter;
            toolbar.Add(tokenTab);

            return toolbar;
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

            var addButton = new Button(OpenNewWizard) { text = "+" };
            addButton.tooltip = "新建";
            addButton.style.width = 22f;
            addButton.style.height = 20f;
            addButton.style.flexShrink = 0f;
            addButton.style.marginLeft = 4f;
            addButton.style.paddingLeft = 0f;
            addButton.style.paddingRight = 0f;
            row.Add(addButton);

            searchField = new TextField();
            searchField.style.flexGrow = 1f;
            searchField.style.flexShrink = 1f;
            searchField.style.marginLeft = 6f;
            searchField.style.marginRight = 0f;
            searchField.RegisterValueChangedCallback(evt =>
            {
                if (IsTokenLibrary)
                {
                    searchToken = evt.newValue;
                    FangHubPageState.SetString(StateScope, SearchTokenKey, searchToken);
                }
                else
                {
                    searchPrefab = evt.newValue;
                    FangHubPageState.SetString(StateScope, SearchPrefabKey, searchPrefab);
                }

                RebuildList();
            });
            row.Add(searchField);

            return row;
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

        // ---------- 项目 ----------

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
            var guids = AssetDatabase.FindAssets("t:" + nameof(UIKitProjectSo));
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!string.IsNullOrEmpty(path))
                {
                    paths.Add(path);
                }
            }

            paths.Sort(StringComparer.Ordinal);

            selectedProject = null;
            for (var i = 0; i < paths.Count; i++)
            {
                var project = AssetDatabase.LoadAssetAtPath<UIKitProjectSo>(paths[i]);
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

            menu.AppendAction("新建控件库项目配置 SO…", _ => CreateProject());
            menu.AppendSeparator();

            if (projectChoices.Count == 0)
            {
                menu.AppendAction("（工程里还没有控件库项目）", null, DropdownMenuAction.Status.Disabled);
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

        private static string BuildProjectLabel(List<UIKitProjectSo> projects, int index)
        {
            var name = projects[index].name;

            for (var i = 0; i < projects.Count; i++)
            {
                if (i != index && projects[i].name == name)
                {
                    return name + "（" + AssetDatabase.GetAssetPath(projects[index]) + "）";
                }
            }

            return name;
        }

        private void SelectProject(UIKitProjectSo project)
        {
            selectedProject = project;
            FangHubPageState.SetString(StateScope, ProjectGuidKey, GetAssetGuid(project));
            RebuildProjectMenu();
            Refresh();
        }

        private void CreateProject()
        {
            UIKitNewProjectWizard.Open(project =>
            {
                if (project == null)
                {
                    return;
                }

                RebuildProjectChoices();
                SelectProject(project);
                SetStatus("控件库项目已创建：" + AssetDatabase.GetAssetPath(project));
            });
        }

        private void Refresh()
        {
            if (listContainer == null)
            {
                return;
            }

            var project = selectedProject;
            titleLabel.text = project == null ? "当前的项目：未选择" : "当前的项目：" + project.name;

            var hasProject = project != null;
            statisticsButton?.SetEnabled(hasProject);
            validateButton?.SetEnabled(hasProject);
            applyAllButton?.SetEnabled(hasProject);

            RebuildLibraryTabs();
            RebuildList();
            RebuildDetail();
        }

        private void SelectLibrary(string value)
        {
            library = value;
            FangHubPageState.SetString(StateScope, LibraryKey, library);
            RebuildLibraryTabs();
            RebuildList();
            RebuildDetail();
        }

        private void RebuildLibraryTabs()
        {
            if (prefabTab == null || tokenTab == null)
            {
                return;
            }

            var tokenActive = IsTokenLibrary;

            prefabTab.style.backgroundColor = tokenActive ? Color.clear : TabActiveColor;
            tokenTab.style.backgroundColor = tokenActive ? TabActiveColor : Color.clear;

            if (searchField != null)
            {
                searchField.SetValueWithoutNotify(tokenActive ? searchToken : searchPrefab);
                searchField.textEdition.placeholder = tokenActive ? "搜索 token…" : "搜索控件…";
            }
        }

        // ---------- 列表 ----------

        private void RebuildList()
        {
            if (listContainer == null)
            {
                return;
            }

            listContainer.Clear();

            if (selectedProject == null)
            {
                listHeaderLabel.text = IsTokenLibrary ? "token 列表（0）" : "控件列表（0）";
                summaryLabel.text = string.Empty;
                listContainer.Add(BuildMessage("用菜单栏「控件库项目」新建一个，或直接选工程里已有的项目。"));
                return;
            }

            if (IsTokenLibrary)
            {
                RebuildTokenList();
            }
            else
            {
                RebuildPrefabList();
            }
        }

        private void RebuildPrefabList()
        {
            var prefabs = selectedProject.Prefabs;
            var search = searchPrefab == null ? string.Empty : searchPrefab.Trim();

            var missing = 0;
            var shown = 0;

            for (var i = 0; i < prefabs.Count; i++)
            {
                var prefab = prefabs[i];
                if (prefab == null)
                {
                    missing++;
                }

                if (search.Length > 0 && !PrefabMatches(prefab, search))
                {
                    continue;
                }

                listContainer.Add(BuildPrefabRow(prefab, i));
                shown++;
            }

            listHeaderLabel.text = search.Length > 0
                ? "控件列表（" + shown + "/" + prefabs.Count + "）"
                : "控件列表（" + prefabs.Count + "）";
            summaryLabel.text = prefabs.Count + " 个控件，" + missing + " 个缺引用。";

            if (prefabs.Count == 0)
            {
                listContainer.Add(BuildMessage("这个项目还没有控件，用上面的「＋」新建一个空壳预制体。"));
            }
            else if (shown == 0)
            {
                listContainer.Add(BuildMessage("没有匹配的控件。"));
            }
        }

        private VisualElement BuildPrefabRow(GameObject prefab, int index)
        {
            var path = PrefabPath(prefab);
            var selected = prefab != null && path == selectedPrefabPath;
            var complete = prefab != null && HasNoBlockingIssue(prefab);

            var row = BuildRowShell(selected);

            var mark = new Label(prefab == null || !complete ? "✗" : "✓");
            mark.style.width = 14f;
            mark.style.flexShrink = 0f;
            mark.style.color = complete ? Color.white : WarnColor;
            row.Add(mark);

            var text = new VisualElement();
            text.style.flexGrow = 1f;
            text.style.overflow = Overflow.Hidden;

            var name = new Label(prefab == null ? "（空）" : prefab.name);
            name.style.unityFontStyleAndWeight = FontStyle.Bold;
            name.style.overflow = Overflow.Hidden;
            text.Add(name);

            var detail = new Label(prefab == null ? "第 " + index + " 行是空引用" : path);
            detail.style.color = DimColor;
            detail.style.fontSize = 10f;
            detail.style.overflow = Overflow.Hidden;
            text.Add(detail);

            row.Add(text);

            if (prefab != null)
            {
                row.RegisterCallback<ClickEvent>(_ => SelectPrefab(path));
            }

            return row;
        }

        private void RebuildTokenList()
        {
            var tokens = selectedProject.Tokens;
            var search = searchToken == null ? string.Empty : searchToken.Trim();

            var notEnabled = 0;
            var shown = 0;

            for (var i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                if (token != null && string.IsNullOrEmpty(token.MatchId))
                {
                    notEnabled++;
                }

                if (search.Length > 0 && !TokenMatches(token, search))
                {
                    continue;
                }

                listContainer.Add(BuildTokenRow(token, i));
                shown++;
            }

            listHeaderLabel.text = search.Length > 0
                ? "token 列表（" + shown + "/" + tokens.Count + "）"
                : "token 列表（" + tokens.Count + "）";
            summaryLabel.text = tokens.Count + " 条 token，" + notEnabled + " 条未启用。";

            if (tokens.Count == 0)
            {
                listContainer.Add(BuildMessage("这个项目还没有 token，用上面的「＋」新建一个。"));
            }
            else if (shown == 0)
            {
                listContainer.Add(BuildMessage("没有匹配的 token。"));
            }
        }

        private VisualElement BuildTokenRow(TokenSo token, int index)
        {
            var path = TokenPath(token);
            var selected = token != null && path == selectedTokenPath;

            var row = BuildRowShell(selected);

            var text = new VisualElement();
            text.style.flexGrow = 1f;
            text.style.overflow = Overflow.Hidden;

            var id = new Label(token == null
                ? "（空）"
                : (string.IsNullOrEmpty(token.MatchId) ? NotEnabledText : token.MatchId));
            id.style.unityFontStyleAndWeight = FontStyle.Bold;
            id.style.overflow = Overflow.Hidden;
            id.style.color = token != null && !string.IsNullOrEmpty(token.MatchId)
                ? Color.white
                : WarnColor;
            text.Add(id);

            var detail = new Label(token == null
                ? "第 " + index + " 行是空引用"
                : token.GetType().Name + "  ·  " + token.name);
            detail.style.color = DimColor;
            detail.style.fontSize = 10f;
            detail.style.overflow = Overflow.Hidden;
            text.Add(detail);

            row.Add(text);

            if (token != null)
            {
                row.RegisterCallback<ClickEvent>(_ => SelectToken(path));
            }

            return row;
        }

        private static VisualElement BuildRowShell(bool selected)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.paddingLeft = 6f;
            row.style.paddingRight = 6f;
            row.style.paddingTop = 3f;
            row.style.paddingBottom = 3f;
            row.style.backgroundColor = selected ? SelectedColor : Color.clear;

            if (!selected)
            {
                row.RegisterCallback<MouseEnterEvent>(_ => row.style.backgroundColor = HoverColor);
                row.RegisterCallback<MouseLeaveEvent>(_ => row.style.backgroundColor = Color.clear);
            }

            return row;
        }

        private void SelectPrefab(string path)
        {
            selectedPrefabPath = path ?? string.Empty;
            FangHubPageState.SetString(StateScope, SelectedPrefabKey, selectedPrefabPath);
            RebuildList();
            RebuildDetail();
        }

        private void SelectToken(string path)
        {
            selectedTokenPath = path ?? string.Empty;
            FangHubPageState.SetString(StateScope, SelectedTokenKey, selectedTokenPath);
            RebuildList();
            RebuildDetail();
        }

        // ---------- 详情 ----------

        private void RebuildDetail()
        {
            if (detailContent == null)
            {
                return;
            }

            DisposeInspector();
            detailContent.Clear();

            if (selectedProject == null)
            {
                detailContent.Add(BuildMessage("先选一个控件库项目。"));
                return;
            }

            if (IsTokenLibrary)
            {
                RebuildTokenDetail();
            }
            else
            {
                RebuildPrefabDetail();
            }
        }

        private void RebuildPrefabDetail()
        {
            var prefab = FindSelectedPrefab();
            if (prefab == null)
            {
                detailContent.Add(BuildMessage(selectedProject.Prefabs.Count == 0
                    ? "还没有可查看的控件。"
                    : "在左侧列表里选一个控件，右侧显示它的详情与配置。"));
                return;
            }

            var path = PrefabPath(prefab);
            var matches = prefab.GetComponentsInChildren<TokenMatch>(true);

            detailContent.Add(BuildDetailHeader(prefab.name, path));

            var basic = BuildCollapsibleSection("基本信息", false);
            basic.Add(BuildFieldRow("资产路径", path));
            basic.Add(BuildFieldRow("TokenMatch", matches.Length.ToString()));
            basic.Add(BuildFieldRow("条目数", CountEntries(matches).ToString()));
            detailContent.Add(basic);

            detailContent.Add(BuildSection("资产"));
            detailContent.Add(BuildPathRow("预制体", path));

            detailContent.Add(BuildSection("完整性"));
            detailContent.Add(BuildPrefabIntegrity(prefab, matches));

            detailContent.Add(BuildSection("条目"));
            detailContent.Add(BuildEntryList(prefab, matches));

            detailContent.Add(BuildSection("操作"));
            detailContent.Add(BuildPrefabActions(path));

            detailContent.Add(BuildSection("预制体配置（可直接编辑）"));
            detailContent.Add(BuildInspector(prefab));
        }

        private void RebuildTokenDetail()
        {
            var token = FindSelectedToken();
            if (token == null)
            {
                detailContent.Add(BuildMessage(selectedProject.Tokens.Count == 0
                    ? "还没有可查看的 token。"
                    : "在左侧列表里选一个 token，右侧显示它的详情与配置。"));
                return;
            }

            var path = TokenPath(token);
            var id = token.MatchId;

            detailContent.Add(BuildDetailHeader(token.name, string.IsNullOrEmpty(id) ? NotEnabledText : id));

            var basic = BuildCollapsibleSection("基本信息", false);
            basic.Add(BuildFieldRow("MatchId", string.IsNullOrEmpty(id) ? NotEnabledText : id));
            basic.Add(BuildFieldRow("类型", token.GetType().Name));
            basic.Add(BuildFieldRow("资产路径", path));
            basic.Add(BuildFieldRow("目标组件类型", token.TargetType.Name));
            detailContent.Add(basic);

            detailContent.Add(BuildSection("资产"));
            detailContent.Add(BuildPathRow("token", path));

            detailContent.Add(BuildSection("校验"));
            detailContent.Add(BuildTokenChecks(token, id));

            detailContent.Add(BuildSection("token 配置（可直接编辑）"));
            detailContent.Add(BuildInspector(token));
        }

        private VisualElement BuildPrefabIntegrity(GameObject prefab, TokenMatch[] matches)
        {
            if (matches.Length == 0)
            {
                var none = new Label("✗ 这个预制体上没有 TokenMatch。");
                none.style.color = WarnColor;
                return none;
            }

            var issues = TokenMatchValidator.Validate(matches, selectedProject);
            if (issues.Count == 0)
            {
                var ok = new Label("✓ 完整");
                ok.style.color = Color.white;
                return ok;
            }

            var container = new VisualElement();
            for (var i = 0; i < issues.Count; i++)
            {
                var line = new Label("✗ " + issues[i].Message);
                line.style.whiteSpace = WhiteSpace.Normal;
                line.style.color = WarnColor;
                container.Add(line);
            }

            return container;
        }

        private VisualElement BuildEntryList(GameObject prefab, TokenMatch[] matches)
        {
            var container = new VisualElement();

            if (matches.Length == 0)
            {
                container.Add(BuildMessage("没有条目可列。"));
                return container;
            }

            for (var m = 0; m < matches.Length; m++)
            {
                var match = matches[m];
                var nodePath = NodePath(match.transform, prefab.transform);
                var heading = new Label("TokenMatch " + m + "（" + (string.IsNullOrEmpty(nodePath) ? prefab.name : nodePath) + "）");
                heading.style.unityFontStyleAndWeight = FontStyle.Bold;
                heading.style.marginTop = 4f;
                container.Add(heading);

                var entries = match.Entries;
                if (entries.Count == 0)
                {
                    container.Add(BuildMessage("这个 TokenMatch 还没有条目。"));
                    continue;
                }

                for (var e = 0; e < entries.Count; e++)
                {
                    container.Add(BuildEntryRow(entries[e], e));
                }
            }

            return container;
        }

        private VisualElement BuildEntryRow(TokenMatchEntry entry, int index)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 1f;

            var id = entry == null || string.IsNullOrEmpty(entry.Id) ? "（没有 id）" : entry.Id;
            var target = entry == null || entry.Target == null ? "（没有 target）" : entry.Target.GetType().Name;
            var count = entry == null ? 0 : selectedProject.GetTokens(entry.Id).Count;

            var text = new Label(
                "第 " + index + " 条  ·  " + id
                + "  ·  " + target
                + "  ·  该 id 有 " + count + " 个 token");
            text.style.flexGrow = 1f;
            text.style.whiteSpace = WhiteSpace.Normal;
            text.style.color = count > 0 && entry != null && entry.Target != null ? Color.white : DimColor;
            row.Add(text);

            var apply = new Button(() => ApplyOne(entry == null ? null : entry.Id)) { text = "应用" };
            apply.style.flexShrink = 0f;
            apply.SetEnabled(entry != null && !string.IsNullOrEmpty(entry.Id));
            apply.tooltip = "只应用这个 id（范围 = 全项目里所有写这个 id 的地方）";
            row.Add(apply);

            return row;
        }

        private VisualElement BuildPrefabActions(string path)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;

            row.Add(new Button(() => Ping(path)) { text = "定位预制体" });
            row.Add(new Button(() => OpenFolder(path)) { text = "打开目录" });

            return row;
        }

        private VisualElement BuildTokenChecks(TokenSo token, string id)
        {
            var container = new VisualElement();

            if (string.IsNullOrEmpty(id))
            {
                container.Add(BuildMessage("这条 token 还没填匹配 id，不参与匹配。"));
            }
            else
            {
                var ok = new Label("✓ 匹配 id：「" + id + "」（手填字符串，只在 token 库与预制体条目之间配对）");
                ok.style.whiteSpace = WhiteSpace.Normal;
                ok.style.color = Color.white;
                container.Add(ok);
            }

            var siblings = new List<TokenSo>();
            var tokens = selectedProject.Tokens;
            for (var i = 0; i < tokens.Count; i++)
            {
                var other = tokens[i];
                if (other != null && other != token && !string.IsNullOrEmpty(id) && other.MatchId == id)
                {
                    siblings.Add(other);
                }
            }

            if (siblings.Count == 0)
            {
                container.Add(BuildMessage("没有别的 token 挂在这个 id 上。"));
            }
            else
            {
                var names = new List<string>();
                for (var i = 0; i < siblings.Count; i++)
                {
                    names.Add(siblings[i].GetType().Name + "（" + siblings[i].name + "）");
                }

                var line = new Label("同 id 还有 " + siblings.Count + " 个 token：" + string.Join("、", names) + "；应用时按 token 库顺序，后面的覆盖前面的。");
                line.style.whiteSpace = WhiteSpace.Normal;
                line.style.color = DimColor;
                container.Add(line);
            }

            return container;
        }

        // ---------- 菜单动作 ----------

        private void OpenNewWizard()
        {
            var project = selectedProject;
            if (project == null)
            {
                SetStatus("未选择控件库项目。");
                return;
            }

            if (IsTokenLibrary)
            {
                UIKitNewTokenWizard.Open(project, token =>
                {
                    Refresh();

                    if (token == null)
                    {
                        return;
                    }

                    SelectToken(TokenPath(token));
                    SetStatus("token 已创建：" + token.name);
                });
            }
            else
            {
                UIKitNewPrefabWizard.Open(project, prefab =>
                {
                    Refresh();

                    if (prefab == null)
                    {
                        return;
                    }

                    SelectPrefab(PrefabPath(prefab));
                    SetStatus("预制体已创建：" + prefab.name);
                });
            }
        }

        private void Validate()
        {
            var project = selectedProject;
            if (project == null)
            {
                SetStatus("未选择控件库项目。");
                return;
            }

            var issues = new List<TokenIssue>();
            issues.AddRange(TokenMatchValidator.ValidateProject(project));

            var matches = new List<TokenMatch>();
            var prefabs = project.Prefabs;
            for (var i = 0; i < prefabs.Count; i++)
            {
                if (prefabs[i] == null)
                {
                    continue;
                }

                matches.AddRange(prefabs[i].GetComponentsInChildren<TokenMatch>(true));
            }

            issues.AddRange(TokenMatchValidator.Validate(matches, project));

            UIKitValidationWindow.Open(project.name, BuildValidationHeader(project, issues), BuildValidationReport(issues));
        }

        private static string BuildValidationHeader(UIKitProjectSo project, List<TokenIssue> issues)
        {
            var errors = 0;
            var hints = 0;

            for (var i = 0; i < issues.Count; i++)
            {
                if (issues[i].IsHint)
                {
                    hints++;
                }
                else
                {
                    errors++;
                }
            }

            return project.name + "：" + errors + " 个问题，" + hints + " 条提醒。";
        }

        private static string BuildValidationReport(List<TokenIssue> issues)
        {
            var errors = new List<string>();
            var hints = new List<string>();

            for (var i = 0; i < issues.Count; i++)
            {
                if (issues[i].IsHint)
                {
                    hints.Add(issues[i].Message);
                }
                else
                {
                    errors.Add(issues[i].Message);
                }
            }

            var sb = new StringBuilder();

            if (errors.Count == 0)
            {
                sb.AppendLine("✓ 没有发现问题。");
            }
            else
            {
                sb.AppendLine("问题（" + errors.Count + "）：");
                for (var i = 0; i < errors.Count; i++)
                {
                    sb.AppendLine("    " + errors[i]);
                }
            }

            if (hints.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("提醒（" + hints.Count + "）：");
                for (var i = 0; i < hints.Count; i++)
                {
                    sb.AppendLine("    " + hints[i]);
                }
            }

            return sb.ToString();
        }

        private void ShowStatistics()
        {
            var project = selectedProject;
            if (project == null)
            {
                SetStatus("未选择控件库项目。");
                return;
            }

            var order = new List<string>();
            var groups = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var tokens = project.Tokens;

            for (var i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                if (token == null)
                {
                    continue;
                }

                var key = token.GetType().Name;
                if (!groups.TryGetValue(key, out var list))
                {
                    list = new List<string>();
                    groups[key] = list;
                    order.Add(key);
                }

                list.Add(string.IsNullOrEmpty(token.MatchId) ? NotEnabledText : token.MatchId);
            }

            var sb = new StringBuilder();
            sb.AppendLine("token 库共 " + tokens.Count + " 条，按类型列出它们的匹配 id：");
            sb.AppendLine();

            if (order.Count == 0)
            {
                sb.AppendLine("（还没有 token）");
            }

            for (var i = 0; i < order.Count; i++)
            {
                var list = groups[order[i]];
                sb.AppendLine(order[i] + "（" + list.Count + " 个）");
                for (var j = 0; j < list.Count; j++)
                {
                    sb.AppendLine("    " + list[j]);
                }
            }

            UIKitValidationWindow.Open(project.name, "统计", "匹配 id 清单", sb.ToString());
        }

        private void ApplyOne(string matchId)
        {
            var project = selectedProject;
            if (project == null)
            {
                SetStatus("未选择控件库项目。");
                return;
            }

            var plan = UIKitApplier.Build(project, matchId);
            UIKitApplyPreviewWindow.Open(project.name, plan, ApplyPlan);
        }

        private void ApplyPlan(UIKitApplyPlan plan)
        {
            var errors = new List<string>();
            var applied = UIKitApplier.Apply(plan, errors);

            SetStatus("已应用 " + applied + " 条" + (errors.Count > 0 ? "，" + errors.Count + " 条没写成功" : "。"));

            if (errors.Count > 0)
            {
                UIKitValidationWindow.Open(
                    selectedProject == null ? string.Empty : selectedProject.name,
                    "应用结果",
                    "有 " + errors.Count + " 条没有写成功：",
                    string.Join("\n", errors));
            }

            Refresh();
        }

        // ---------- 查找 ----------

        private bool IsTokenLibrary => library == LibraryToken;

        private GameObject FindSelectedPrefab()
        {
            if (selectedProject == null || string.IsNullOrEmpty(selectedPrefabPath))
            {
                return null;
            }

            var prefabs = selectedProject.Prefabs;
            for (var i = 0; i < prefabs.Count; i++)
            {
                var prefab = prefabs[i];
                if (prefab != null && PrefabPath(prefab) == selectedPrefabPath)
                {
                    return prefab;
                }
            }

            return null;
        }

        private TokenSo FindSelectedToken()
        {
            if (selectedProject == null || string.IsNullOrEmpty(selectedTokenPath))
            {
                return null;
            }

            var tokens = selectedProject.Tokens;
            for (var i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                if (token != null && TokenPath(token) == selectedTokenPath)
                {
                    return token;
                }
            }

            return null;
        }

        private bool HasNoBlockingIssue(GameObject prefab)
        {
            var matches = prefab.GetComponentsInChildren<TokenMatch>(true);
            if (matches.Length == 0)
            {
                return false;
            }

            var issues = TokenMatchValidator.Validate(matches, selectedProject);
            for (var i = 0; i < issues.Count; i++)
            {
                if (!issues[i].IsHint)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool PrefabMatches(GameObject prefab, string search)
        {
            if (prefab == null)
            {
                return false;
            }

            return prefab.name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
                || PrefabPath(prefab).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool TokenMatches(TokenSo token, string search)
        {
            if (token == null)
            {
                return false;
            }

            var id = token.MatchId ?? string.Empty;
            return id.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
                || token.GetType().Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
                || token.name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static int CountEntries(TokenMatch[] matches)
        {
            var total = 0;
            for (var i = 0; i < matches.Length; i++)
            {
                total += matches[i].Entries.Count;
            }

            return total;
        }

        private static string PrefabPath(GameObject prefab)
        {
            return prefab == null ? string.Empty : AssetDatabase.GetAssetPath(prefab);
        }

        private static string TokenPath(TokenSo token)
        {
            return token == null ? string.Empty : AssetDatabase.GetAssetPath(token);
        }

        private static string NodePath(Transform node, Transform root)
        {
            if (node == null || root == null || node == root)
            {
                return string.Empty;
            }

            var names = new List<string>();
            var current = node;

            while (current != null && current != root)
            {
                names.Add(current.name);
                current = current.parent;
            }

            names.Reverse();
            return string.Join("/", names);
        }

        private static string GetAssetGuid(UnityEngine.Object asset)
        {
            if (asset == null)
            {
                return string.Empty;
            }

            return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
        }

        // ---------- 排版小件 ----------

        private VisualElement BuildInspector(UnityEngine.Object asset)
        {
            if (asset == null)
            {
                return BuildMessage("资产为空引用。");
            }

            inspectorSerializedObject = new SerializedObject(asset);
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

        private static VisualElement BuildDetailHeader(string title, string subtitle)
        {
            var header = new VisualElement();

            var name = new Label(title);
            name.style.fontSize = 14f;
            name.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(name);

            var line = new Label(string.IsNullOrEmpty(subtitle) ? "（没有路径）" : subtitle);
            line.style.color = DimColor;
            line.style.whiteSpace = WhiteSpace.Normal;
            header.Add(line);

            return header;
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

            var exists = !string.IsNullOrEmpty(assetPath) && AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) != null;
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

        private static Label BuildMessage(string text)
        {
            var label = new Label(text);
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.color = DimColor;
            return label;
        }

        private void SetStatus(string message)
        {
            if (statusLabel == null)
            {
                return;
            }

            statusLabel.text = message;
        }

        private static void Ping(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (asset == null)
            {
                return;
            }

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private static void OpenFolder(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            var folder = System.IO.Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(folder) || !AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            EditorUtility.RevealInFinder(folder);
        }
    }
}
