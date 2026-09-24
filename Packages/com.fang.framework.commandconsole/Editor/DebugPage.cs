using System;
using System.Collections.Generic;
using System.Text;
using Fang.Framework.Editor.Hub;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fang.Framework.CommandConsole.Editor
{
    /// <summary>
    /// FangHub「指令控制台」页：看指令清单、校验配置、导出文档。
    /// 菜单栏只放整体操作（调试项目 ▾ / 校验 / 导出文档 / 刷新 / 导入示例）；面板相关的归到左列。
    /// 选项目靠菜单栏的下拉，页面不再内嵌 SO Inspector（要改配置就点标题行的「定位」跳过去）。
    /// </summary>
    [FangHubPage("framework-command-console", "指令控制台", "Framework",
        Description = "指令层的清单、校验与文档导出。", Order = 2)]
    public sealed class DebugPage : IFangHubVisualElementPage
    {
        private const string StateScope = "framework-command-console";
        private const string ProjectGuidKey = "ProjectGuid";
        private const string SearchKey = "Search";
        private const string CategoryFilterKey = "CategoryFilter";
        private const string SelectedCommandKey = "SelectedCommand";

        /// <summary>分类筛选的「全部」：排除内置指令分类（内置的那三条平时不占列表）。</summary>
        private const string AllCategories = "";

        private const string PackageJsonPath = "Packages/com.fang.framework.commandconsole/package.json";
        private const string SampleSceneName = "Demo.unity";

        private const float ListPaneWidth = 260f;
        private const float DetailLabelWidth = 96f;

        private static readonly Color DimColor = new Color(0.65f, 0.65f, 0.65f);
        private static readonly Color WarnColor = new Color(1f, 0.6f, 0.4f);
        private static readonly Color LineColor = new Color(0.28f, 0.28f, 0.28f);
        private static readonly Color HoverColor = new Color(0.21f, 0.29f, 0.44f);
        private static readonly Color SelectedColor = new Color(0.24f, 0.37f, 0.59f);

        private readonly List<DebugProjectSo> projectChoices = new List<DebugProjectSo>();

        private IReadOnlyList<DebugCommandEntry> commands = Array.Empty<DebugCommandEntry>();

        private ToolbarMenu projectMenu;
        private ToolbarButton validateButton;
        private ToolbarButton exportButton;
        private Label statusLabel;
        private Label titleLabel;
        private Button locateButton;
        private Label listHeaderLabel;
        private TextField searchField;
        private ToolbarMenu categoryFilterMenu;
        private Label summaryLabel;
        private VisualElement listContainer;
        private VisualElement detailContent;

        private DebugProjectSo selectedProject;
        private string search = string.Empty;
        private string categoryFilter = AllCategories;
        private string selectedCommand = string.Empty;

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

            search = FangHubPageState.GetString(StateScope, SearchKey);
            categoryFilter = FangHubPageState.GetString(StateScope, CategoryFilterKey, AllCategories);
            selectedCommand = FangHubPageState.GetString(StateScope, SelectedCommandKey);

            root.Add(BuildMenuBar());
            root.Add(BuildTitleRow());
            root.Add(BuildSplit());

            RebuildProjectChoices();
            Refresh();
            return root;
        }

        // ---------- 骨架 ----------

        private VisualElement BuildMenuBar()
        {
            var toolbar = new Toolbar();
            toolbar.style.marginBottom = 4f;

            projectMenu = new ToolbarMenu { text = "调试项目" };
            toolbar.Add(projectMenu);

            validateButton = new ToolbarButton(Validate) { text = "校验" };
            toolbar.Add(validateButton);

            exportButton = new ToolbarButton(ExportDocuments) { text = "导出文档" };
            exportButton.tooltip = "按当前配置的分类顺序与输出路径导出指令清单。";
            toolbar.Add(exportButton);

            toolbar.Add(new ToolbarButton(RefreshProjects) { text = "刷新" });

            var importSample = new ToolbarButton(ImportSample) { text = "导入示例" };
            importSample.tooltip = "把包内示例（Demo）复制到 Assets/Samples；已导入过会覆盖那份副本。";
            toolbar.Add(importSample);

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

            locateButton = new Button(() => Ping(AssetDatabase.GetAssetPath(selectedProject))) { text = "定位" };
            locateButton.tooltip = "在 Project 窗口里定位当前调试项目配置 SO。";
            locateButton.style.marginLeft = 6f;
            row.Add(locateButton);

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
            pane.Add(BuildCategoryFilterRow());

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

            searchField = new TextField();
            searchField.style.flexGrow = 1f;
            searchField.style.flexShrink = 1f;
            searchField.style.marginLeft = 6f;
            searchField.style.marginRight = 0f;
            searchField.textEdition.placeholder = "搜索指令…";
            searchField.RegisterValueChangedCallback(evt =>
            {
                search = evt.newValue;
                FangHubPageState.SetString(StateScope, SearchKey, search);
                RebuildList();
            });
            row.Add(searchField);

            return row;
        }

        private VisualElement BuildCategoryFilterRow()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.flexShrink = 0f;
            row.style.marginTop = 2f;

            categoryFilterMenu = new ToolbarMenu { text = "分类：全部" };
            categoryFilterMenu.style.flexShrink = 0f;
            row.Add(categoryFilterMenu);

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
            var all = DebugProjectLocator.FindAll();

            projectChoices.Clear();
            selectedProject = null;

            for (var i = 0; i < all.Count; i++)
            {
                projectChoices.Add(all[i]);

                if (GetGuid(all[i]) == selectedGuid)
                {
                    selectedProject = all[i];
                }
            }

            RebuildProjectMenu();
        }

        private void RebuildProjectMenu()
        {
            var menu = projectMenu.menu;
            menu.ClearItems();

            menu.AppendAction("新建调试项目配置 SO…", _ => OpenNewProjectWizard());
            menu.AppendSeparator();

            if (projectChoices.Count == 0)
            {
                menu.AppendAction("（工程里还没有调试项目）", null, DropdownMenuAction.Status.Disabled);
                return;
            }

            for (var i = 0; i < projectChoices.Count; i++)
            {
                var project = projectChoices[i];
                menu.AppendAction(
                    BuildProjectLabel(i),
                    _ => SelectProject(project),
                    _ => project == selectedProject ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            }
        }

        private string BuildProjectLabel(int index)
        {
            var name = projectChoices[index].name;

            for (var i = 0; i < projectChoices.Count; i++)
            {
                if (i != index && projectChoices[i].name == name)
                {
                    return name + "（" + AssetDatabase.GetAssetPath(projectChoices[index]) + "）";
                }
            }

            return name;
        }

        private void SelectProject(DebugProjectSo project)
        {
            selectedProject = project;
            FangHubPageState.SetString(StateScope, ProjectGuidKey, GetGuid(project));

            RebuildProjectMenu();
            Refresh();
        }

        private void OpenNewProjectWizard()
        {
            DebugNewProjectWizard.Open(project =>
            {
                RebuildProjectChoices();

                if (project == null)
                {
                    return;
                }

                SelectProject(project);
                SetStatus("调试项目已创建：" + AssetDatabase.GetAssetPath(project));
            });
        }

        // ---------- 刷新 ----------

        private void Refresh()
        {
            if (listContainer == null)
            {
                return;
            }

            titleLabel.text = selectedProject == null
                ? "当前的项目：未选择（用内置默认值）"
                : "当前的项目：" + selectedProject.name;

            locateButton?.SetEnabled(selectedProject != null);
            validateButton?.SetEnabled(true);
            exportButton?.SetEnabled(true);

            RebuildCommands();
            RebuildCategoryFilterMenu();
            RebuildList();
            RebuildDetail();
            RebuildStatus();
        }

        private void RebuildCommands()
        {
            // 运行时优先：有活着的服务就用它的注册表（那是真相）；否则按配置在编辑器侧扫一遍。
            var live = DebugCommandService.ActiveServices;
            if (live.Count > 0 && live[0].Registry != null)
            {
                commands = live[0].Registry.Commands;
                return;
            }

            commands = DebugCommandValidator.ScanEntries(DebugSettings.From(selectedProject), null);
        }

        private void RebuildStatus()
        {
            if (statusLabel == null)
            {
                return;
            }

            var live = DebugCommandService.ActiveServices;
            if (live.Count == 0)
            {
                statusLabel.text = "未运行（编辑器侧只读）";
                return;
            }

            var service = live[0];
            statusLabel.text =
                (service.IsListening ? "监听 " + service.ListenAddress + ":" + service.Port : "未监听")
                + " · " + (service.IsTicking ? "tick 正常" : "⚠ 没有 tick");
        }

        /// <summary>
        /// 分类筛选菜单按当前扫到的分类重建（分类是自由字符串，只能从指令里收集）。
        /// 「全部」= 各业务分类（排除内置指令分类）；「内置指令」= 只看内置那三条。
        /// </summary>
        private void RebuildCategoryFilterMenu()
        {
            if (categoryFilterMenu == null)
            {
                return;
            }

            var categories = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            for (var i = 0; i < commands.Count; i++)
            {
                var category = commands[i].Category;

                if (category == DebugCommandDefaults.BuiltInCategory || !seen.Add(category))
                {
                    continue;
                }

                categories.Add(category);
            }

            categories.Sort(StringComparer.Ordinal);

            var menu = categoryFilterMenu.menu;
            menu.ClearItems();

            menu.AppendAction("全部", _ => SelectCategory(AllCategories), _ => StatusFor(AllCategories));
            menu.AppendAction(
                CategoryLabel(DebugCommandDefaults.BuiltInCategory),
                _ => SelectCategory(DebugCommandDefaults.BuiltInCategory),
                _ => StatusFor(DebugCommandDefaults.BuiltInCategory));

            for (var i = 0; i < categories.Count; i++)
            {
                var category = categories[i];
                menu.AppendAction(category, _ => SelectCategory(category), _ => StatusFor(category));
            }

            categoryFilterMenu.text = "分类：" + CategoryLabel(categoryFilter);
        }

        /// <summary>筛选值 → 给人看的名字（<c>""</c> = 全部，内置指令分类显示成「内置指令」）。</summary>
        private static string CategoryLabel(string category)
        {
            if (string.IsNullOrEmpty(category))
            {
                return "全部";
            }

            return category == DebugCommandDefaults.BuiltInCategory ? "内置指令" : category;
        }

        private DropdownMenuAction.Status StatusFor(string category)
        {
            return categoryFilter == category
                ? DropdownMenuAction.Status.Checked
                : DropdownMenuAction.Status.Normal;
        }

        private void SelectCategory(string category)
        {
            categoryFilter = category ?? AllCategories;
            FangHubPageState.SetString(StateScope, CategoryFilterKey, categoryFilter);
            RebuildCategoryFilterMenu();
            RebuildList();
        }

        // ---------- 列表 ----------

        private void RebuildList()
        {
            if (listContainer == null)
            {
                return;
            }

            listContainer.Clear();

            var filter = search == null ? string.Empty : search.Trim();
            var shown = 0;

            for (var i = 0; i < commands.Count; i++)
            {
                var entry = commands[i];

                if (!PassesCategoryFilter(entry.Category))
                {
                    continue;
                }

                if (filter.Length > 0 && !CommandMatches(entry, filter))
                {
                    continue;
                }

                listContainer.Add(BuildCommandRow(entry));
                shown++;
            }

            listHeaderLabel.text = "指令列表（" + shown + "/" + commands.Count + "）";
            summaryLabel.text = BuildCommandSummary();

            if (commands.Count == 0)
            {
                listContainer.Add(BuildMessage("没扫到指令。检查「程序集前缀」扫描范围，或先进入 PlayMode。"));
            }
            else if (shown == 0)
            {
                listContainer.Add(BuildMessage("没有匹配的指令。"));
            }
        }

        /// <summary>「全部」不含内置指令分类 —— 那三条平时不用看，想看就在筛选里单点「内置指令」。</summary>
        private bool PassesCategoryFilter(string category)
        {
            if (categoryFilter == AllCategories)
            {
                return category != DebugCommandDefaults.BuiltInCategory;
            }

            return string.Equals(category, categoryFilter, StringComparison.Ordinal);
        }

        private string BuildCommandSummary()
        {
            var builtIn = 0;
            var business = new HashSet<string>(StringComparer.Ordinal);

            for (var i = 0; i < commands.Count; i++)
            {
                if (commands[i].Category == DebugCommandDefaults.BuiltInCategory)
                {
                    builtIn++;
                }
                else
                {
                    business.Add(commands[i].Category);
                }
            }

            return "共 " + commands.Count + " 条（内置 " + builtIn + " 条）· " + business.Count + " 个业务分类";
        }

        private VisualElement BuildCommandRow(DebugCommandEntry entry)
        {
            var selected = entry.Name == selectedCommand;
            var row = BuildRowShell(selected);

            var text = new VisualElement();
            text.style.flexGrow = 1f;
            text.style.overflow = Overflow.Hidden;

            var name = new Label(entry.Name);
            name.style.unityFontStyleAndWeight = FontStyle.Bold;
            name.style.overflow = Overflow.Hidden;
            text.Add(name);

            var detail = new Label(entry.Category + "  ·  " + Shorten(entry.Source));
            detail.style.color = DimColor;
            detail.style.fontSize = 10f;
            detail.style.overflow = Overflow.Hidden;
            text.Add(detail);

            row.Add(text);
            row.RegisterCallback<ClickEvent>(_ => SelectCommand(entry.Name));

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

        private void SelectCommand(string name)
        {
            selectedCommand = name ?? string.Empty;
            FangHubPageState.SetString(StateScope, SelectedCommandKey, selectedCommand);
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

            detailContent.Clear();

            var entry = FindSelectedCommand();
            if (entry == null)
            {
                detailContent.Add(BuildMessage(commands.Count == 0
                    ? "还没有可查看的指令。"
                    : "在左侧列表里选一条指令，右侧显示它的说明与调用方式。"));
                return;
            }

            detailContent.Add(BuildDetailHeader(entry.Name, entry.Category));

            var basic = BuildCollapsibleSection("基本信息", false);
            basic.Add(BuildFieldRow("分类", entry.Category));
            basic.Add(BuildFieldRow("实现", entry.Source));
            basic.Add(BuildFieldRow("参数", DescribeParameterText(entry.Description)));
            detailContent.Add(basic);

            detailContent.Add(BuildSection("说明"));
            detailContent.Add(BuildParagraph(string.IsNullOrWhiteSpace(entry.Description)
                ? "（这条指令没有写描述）"
                : entry.Description));

            detailContent.Add(BuildSection("怎么调"));
            detailContent.Add(BuildParagraph(
                "TCP 一行 JSON：" + DebugCommandProtocol.RequestExample.Replace("ping", entry.Name)));
            detailContent.Add(BuildParagraph(
                "控制台：直接输入 `" + entry.Name + " 参数1 参数2`（"
                + DebugCommandConsoleWindow.MenuPath + "）。"));
            detailContent.Add(BuildParagraph("运行时清单：`list_commands` 指令。"));

            var copy = new Button(() => CopyCommandLine(entry)) { text = "复制指令" };
            copy.tooltip = "复制「指令名 参数名…」到剪贴板，改完参数就能直接粘到控制台执行。";
            copy.style.alignSelf = Align.FlexStart;
            copy.style.marginTop = 4f;
            detailContent.Add(copy);
        }

        private void CopyCommandLine(DebugCommandEntry entry)
        {
            var text = BuildCommandLine(entry);
            EditorGUIUtility.systemCopyBuffer = text;
            SetStatus("已复制：" + text);
        }

        /// <summary>「指令名 参数名…」—— 参数名来自描述里的 <c>&lt;...&gt;</c>，不带尖括号。</summary>
        private static string BuildCommandLine(DebugCommandEntry entry)
        {
            var names = DescribeParameters(entry.Description);

            if (names.Count == 0)
            {
                return entry.Name;
            }

            var builder = new StringBuilder(entry.Name);
            for (var i = 0; i < names.Count; i++)
            {
                builder.Append(' ').Append(names[i]);
            }

            return builder.ToString();
        }

        // ---------- 菜单动作 ----------

        private void Validate()
        {
            var settings = DebugSettings.From(selectedProject);
            var issues = DebugCommandValidator.Validate(selectedProject, settings);

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

            var header = (selectedProject == null ? "（内置默认配置）" : selectedProject.name)
                + "：" + errors.Count + " 个问题，" + hints.Count + " 条提醒。";

            DebugValidationWindow.Open(
                selectedProject == null ? string.Empty : selectedProject.name,
                header,
                BuildValidationReport(errors, hints));
        }

        private static string BuildValidationReport(List<string> errors, List<string> hints)
        {
            var builder = new StringBuilder();

            if (errors.Count == 0)
            {
                builder.AppendLine("✓ 没有发现问题。");
            }
            else
            {
                builder.AppendLine("问题（" + errors.Count + "）：");
                for (var i = 0; i < errors.Count; i++)
                {
                    builder.AppendLine("    " + errors[i]);
                }
            }

            if (hints.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine("提醒（" + hints.Count + "）：");
                for (var i = 0; i < hints.Count; i++)
                {
                    builder.AppendLine("    " + hints[i]);
                }
            }

            return builder.ToString();
        }

        private void ExportDocuments()
        {
            DebugCommandDocGenerator.Export();
            SetStatus("已导出：" + DebugCommandDocBuilder.IndexPath(DebugSettings.From(selectedProject)));
        }

        // ---------- 示例 ----------

        private static bool TryFindSample(out UnityEditor.PackageManager.UI.Sample sample)
        {
            sample = default;

            var info = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(PackageJsonPath);
            if (info == null)
            {
                return false;
            }

            foreach (var candidate in UnityEditor.PackageManager.UI.Sample.FindByPackage(info.name, info.version))
            {
                sample = candidate;
                return true;
            }

            return false;
        }

        private void ImportSample()
        {
            if (!TryFindSample(out var sample))
            {
                SetStatus("没找到包内示例。");
                return;
            }

            if (sample.isImported && !EditorUtility.DisplayDialog(
                    "重新导入示例",
                    "示例已经导入过：\n" + sample.importPath + "\n\n重新导入会覆盖这份副本里你自己的改动，确定继续？",
                    "覆盖导入",
                    "取消"))
            {
                return;
            }

            if (!sample.Import(
                    UnityEditor.PackageManager.UI.Sample.ImportOptions.HideImportWindow
                    | UnityEditor.PackageManager.UI.Sample.ImportOptions.OverridePreviousImports))
            {
                SetStatus("示例导入失败。");
                return;
            }

            AssetDatabase.Refresh();
            RefreshProjects();

            var assetPath = ToAssetPath(sample.importPath);
            SetStatus("示例已导入：" + assetPath);
            Ping(assetPath + "/" + SampleSceneName);
        }

        private static string ToAssetPath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath))
            {
                return string.Empty;
            }

            var path = absolutePath.Replace('\\', '/');
            var dataPath = Application.dataPath.Replace('\\', '/');

            return path.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase)
                ? "Assets" + path.Substring(dataPath.Length)
                : path;
        }

        // ---------- 查找 ----------

        private DebugCommandEntry FindSelectedCommand()
        {
            if (string.IsNullOrEmpty(selectedCommand))
            {
                return null;
            }

            for (var i = 0; i < commands.Count; i++)
            {
                if (commands[i].Name == selectedCommand)
                {
                    return commands[i];
                }
            }

            return null;
        }

        private static bool CommandMatches(DebugCommandEntry entry, string filter)
        {
            return Contains(entry.Name, filter)
                || Contains(entry.Category, filter)
                || Contains(entry.Description, filter)
                || Contains(entry.Source, filter);
        }

        private static bool Contains(string value, string filter)
        {
            return !string.IsNullOrEmpty(value)
                && value.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>描述里的 <c>&lt;参数&gt;</c> → 参数名清单（顺序即出现顺序）。</summary>
        private static IReadOnlyList<string> DescribeParameters(string description)
        {
            var names = new List<string>();

            if (string.IsNullOrEmpty(description))
            {
                return names;
            }

            var index = 0;

            while (index < description.Length)
            {
                var open = description.IndexOf('<', index);
                if (open < 0)
                {
                    break;
                }

                var close = description.IndexOf('>', open + 1);
                if (close < 0)
                {
                    break;
                }

                names.Add(description.Substring(open + 1, close - open - 1));
                index = close + 1;
            }

            return names;
        }

        private static string DescribeParameterText(string description)
        {
            var names = DescribeParameters(description);

            if (names.Count > 0)
            {
                return string.Join(", ", names);
            }

            return string.IsNullOrEmpty(description) ? "（没有描述）" : "（描述里没有 <参数>）";
        }

        private static string Shorten(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return string.Empty;
            }

            var dot = source.LastIndexOf('.');
            return dot < 0 || dot == source.Length - 1 ? source : source.Substring(dot + 1);
        }

        private static string GetGuid(UnityEngine.Object asset)
        {
            if (asset == null)
            {
                return string.Empty;
            }

            return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
        }

        // ---------- 排版小件 ----------

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
            value.selection.isSelectable = true;
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

        private static Label BuildParagraph(string text)
        {
            var label = new Label(text);
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.color = DimColor;
            label.style.marginBottom = 4f;
            label.selection.isSelectable = true;
            return label;
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
            if (statusLabel != null)
            {
                statusLabel.text = message;
            }
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
    }
}
