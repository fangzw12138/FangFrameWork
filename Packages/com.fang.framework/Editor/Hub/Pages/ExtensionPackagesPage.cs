using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Fang.Framework.Editor.Hub.Pages
{
    [FangHubPage("framework-extension-packages", "框架与扩展", "Framework",
        Description = "更新框架包，查找、安装、更新与卸载扩展包。", Order = 0)]
    public sealed class ExtensionPackagesPage : IFangHubVisualElementPage
    {
        private const string IndexUrlKey = "Fang.Framework.ExtensionPackages.IndexUrl";
        private const string CorePackageName = "com.fang.framework";
        private const string DefaultIndexUrl = "https://raw.githubusercontent.com/fangzw12138/FangFrameWork/main/packages.json";

        private const string StateScope = "framework-extension-packages";
        private const string SearchKey = "Search";
        private const string FilterKey = "Filter";
        private const string SelectedKey = "SelectedPackage";

        private const float ListPaneWidth = 260f;
        private const float DetailLabelWidth = 76f;
        private const float MarkWidth = 14f;
        private const float FilterWidth = 66f;

        private static readonly string[] FilterNames = { "全部", "已安装", "可更新", "未安装" };

        private static readonly Color DimColor = new Color(0.65f, 0.65f, 0.65f);
        private static readonly Color WarnColor = new Color(1f, 0.6f, 0.4f);
        private static readonly Color LineColor = new Color(0.28f, 0.28f, 0.28f);
        private static readonly Color HoverColor = new Color(0.21f, 0.29f, 0.44f);
        private static readonly Color SelectedColor = new Color(0.24f, 0.37f, 0.59f);

        private readonly ExtensionPackageInstaller _installer = new ExtensionPackageInstaller();
        private readonly List<ExtensionPackageRow> _allRows = new List<ExtensionPackageRow>();
        private readonly List<ExtensionPackageRow> _rows = new List<ExtensionPackageRow>();

        private string _indexUrl;
        private ExtensionPackageIndexDocument _index;
        private UnityWebRequest _fetch;
        private ExtensionPackageRow _core;
        private bool _coreInstalled;

        private string _selectedName = string.Empty;
        private string _search = string.Empty;
        private ExtensionPackageFilter _filter = ExtensionPackageFilter.All;

        private Label _statusLabel;
        private VisualElement _coreRow;
        private Label _coreTitle;
        private Label _coreId;
        private Label _coreMeta;
        private Label _coreState;
        private Button _coreAction;
        private Label _listHeaderLabel;
        private DropdownField _filterField;
        private TextField _searchField;
        private Label _summaryLabel;
        private VisualElement _packageList;
        private VisualElement _detailContent;
        private TextField _indexField;

        public void OnInitialize(FangHubWindow window)
        {
        }

        public void OnSelected()
        {
        }

        public VisualElement CreateVisualElement()
        {
            _indexUrl = EditorPrefs.GetString(IndexUrlKey, DefaultIndexUrl);
            _search = FangHubPageState.GetString(StateScope, SearchKey, string.Empty);
            _filter = ParseFilter(FangHubPageState.GetString(StateScope, FilterKey, FilterNames[0]));
            _selectedName = FangHubPageState.GetString(StateScope, SelectedKey, string.Empty);

            var root = new VisualElement();
            root.style.flexGrow = 1f;

            root.Add(BuildToolbar());
            root.Add(BuildCoreRow());
            root.Add(BuildSplit());
            root.Add(BuildAdvancedSection());

            _installer.Changed += OnInstallerChanged;
            _installer.RefreshInstalledPackages();

            if (_index == null)
            {
                RefreshIndex();
            }
            else
            {
                Refresh();
            }

            return root;
        }

        private VisualElement BuildToolbar()
        {
            var toolbar = new Toolbar();
            toolbar.style.marginBottom = 4f;

            var indexMenu = new ToolbarMenu { text = "索引" };
            indexMenu.menu.AppendAction("刷新列表", _ => RefreshIndex());
            indexMenu.menu.AppendAction("恢复默认索引地址", _ => ResetIndexUrl());
            toolbar.Add(indexMenu);

            toolbar.Add(new ToolbarButton(RefreshIndex) { text = "刷新" });

            var spacer = new VisualElement();
            spacer.style.flexGrow = 1f;
            toolbar.Add(spacer);

            _statusLabel = new Label();
            _statusLabel.style.color = DimColor;
            _statusLabel.style.whiteSpace = WhiteSpace.NoWrap;
            _statusLabel.style.overflow = Overflow.Hidden;
            _statusLabel.style.marginRight = 6f;
            toolbar.Add(_statusLabel);

            return toolbar;
        }

        private VisualElement BuildCoreRow()
        {
            _coreRow = new VisualElement();
            _coreRow.style.flexDirection = FlexDirection.Row;
            _coreRow.style.alignItems = Align.Center;
            _coreRow.style.flexShrink = 0f;
            _coreRow.style.paddingLeft = 6f;
            _coreRow.style.paddingRight = 6f;
            _coreRow.style.paddingTop = 4f;
            _coreRow.style.paddingBottom = 4f;
            _coreRow.style.borderBottomWidth = 1f;
            _coreRow.style.borderBottomColor = LineColor;

            _coreTitle = new Label("核心包");
            _coreTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            _coreTitle.style.flexShrink = 0f;
            _coreTitle.style.marginRight = 8f;

            _coreId = new Label(CorePackageName);
            _coreId.style.color = DimColor;
            _coreId.style.flexShrink = 0f;
            _coreId.style.marginRight = 8f;

            _coreMeta = new Label();
            _coreMeta.style.flexShrink = 0f;

            _coreState = new Label();
            _coreState.style.marginLeft = 12f;
            _coreState.style.color = DimColor;

            var spacer = new VisualElement();
            spacer.style.flexGrow = 1f;

            _coreAction = new Button(OnCoreAction) { text = "更新" };
            _coreAction.style.flexShrink = 0f;
            _coreAction.style.marginLeft = 8f;

            _coreRow.Add(_coreTitle);
            _coreRow.Add(_coreId);
            _coreRow.Add(_coreMeta);
            _coreRow.Add(_coreState);
            _coreRow.Add(spacer);
            _coreRow.Add(_coreAction);
            return _coreRow;
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

            _summaryLabel = new Label();
            _summaryLabel.style.whiteSpace = WhiteSpace.Normal;
            _summaryLabel.style.color = DimColor;
            _summaryLabel.style.fontSize = 10f;
            _summaryLabel.style.marginBottom = 2f;
            pane.Add(_summaryLabel);

            var scroll = new ScrollView();
            scroll.style.flexGrow = 1f;
            _packageList = scroll.contentContainer;
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

            _listHeaderLabel = new Label("扩展包");
            _listHeaderLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _listHeaderLabel.style.flexShrink = 0f;
            row.Add(_listHeaderLabel);

            _filterField = new DropdownField(new List<string>(FilterNames), (int)_filter);
            _filterField.style.width = FilterWidth;
            _filterField.style.flexShrink = 0f;
            _filterField.style.marginLeft = 4f;
            _filterField.tooltip = "按安装状态筛选";
            _filterField.RegisterValueChangedCallback(evt =>
            {
                _filter = ParseFilter(evt.newValue);
                FangHubPageState.SetString(StateScope, FilterKey, FilterNames[(int)_filter]);
                RebuildList();
            });
            row.Add(_filterField);

            _searchField = new TextField { value = _search };
            _searchField.style.flexGrow = 1f;
            _searchField.style.flexShrink = 1f;
            _searchField.style.marginLeft = 4f;
            _searchField.style.marginRight = 0f;
            _searchField.textEdition.placeholder = "搜索扩展包…";
            _searchField.RegisterValueChangedCallback(evt =>
            {
                _search = evt.newValue ?? string.Empty;
                FangHubPageState.SetString(StateScope, SearchKey, _search);
                RebuildList();
            });
            row.Add(_searchField);

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
            _detailContent = scroll.contentContainer;
            pane.Add(scroll);

            return pane;
        }

        private VisualElement BuildAdvancedSection()
        {
            var advanced = new Foldout { text = "索引设置（高级）", value = false };
            advanced.style.marginTop = 8f;
            advanced.style.flexShrink = 0f;

            _indexField = new TextField("索引地址") { value = _indexUrl };
            _indexField.style.flexGrow = 1f;
            _indexField.RegisterValueChangedCallback(evt =>
            {
                _indexUrl = evt.newValue;
                EditorPrefs.SetString(IndexUrlKey, _indexUrl ?? string.Empty);
            });

            var hint = new Label();
            hint.style.whiteSpace = WhiteSpace.Normal;
            hint.style.fontSize = 11f;
            hint.style.color = DimColor;
            hint.text = "索引是仓库根目录的 packages.json，页面靠它知道有哪些扩展包、各自在哪个 tag。"
                + "只有离线验证才需要改成 file:// 本地索引。";

            advanced.Add(_indexField);
            advanced.Add(hint);
            return advanced;
        }

        private void Refresh()
        {
            if (_packageList == null)
            {
                return;
            }

            _core = ExtensionPackageCatalog.Build(
                _index == null ? null : _index.core,
                ResolveLocal(CorePackageName));
            _coreInstalled = _core.Local != null;

            RebuildCoreRow();
            RebuildList();
            RebuildDetail();
        }

        private void RebuildCoreRow()
        {
            if (_coreTitle == null || _core == null)
            {
                return;
            }

            var local = _core.Local;
            var known = local != null;

            _coreId.text = known ? _core.Name : CorePackageName;
            _coreMeta.text = known
                ? _core.InstalledVersion + " · " + ExtensionPackageCatalog.DescribeSource(local.Source)
                : "读取中…";

            var canUpdate = _core.State == ExtensionPackageState.UpdateAvailable;

            switch (_core.State)
            {
                case ExtensionPackageState.UpdateAvailable:
                    _coreState.text = "可更新到 " + _core.IndexVersion;
                    break;
                case ExtensionPackageState.LocalAhead:
                    _coreState.text = "本地领先索引";
                    break;
                case ExtensionPackageState.Installed:
                    _coreState.text = "已是最新";
                    break;
                case ExtensionPackageState.Embedded:
                    _coreState.text = "内嵌（本仓库开发）";
                    break;
                default:
                    _coreState.text = known ? "未安装" : string.Empty;
                    break;
            }

            _coreState.style.color = canUpdate ? WarnColor : DimColor;
            _coreAction.style.display = canUpdate ? DisplayStyle.Flex : DisplayStyle.None;
            _coreAction.SetEnabled(canUpdate && !_installer.IsBusy);
            _coreRow.tooltip = _core.Description;
        }

        private void RebuildList()
        {
            if (_packageList == null)
            {
                return;
            }

            var installed = CollectInstalled();
            _allRows.Clear();
            _allRows.AddRange(ExtensionPackageCatalog.Build(_index, installed, ExtensionPackageFilter.All, null));

            _rows.Clear();
            var updateCount = 0;
            for (var i = 0; i < _allRows.Count; i++)
            {
                var row = _allRows[i];
                if (row.State == ExtensionPackageState.UpdateAvailable)
                {
                    updateCount++;
                }

                if (ExtensionPackageCatalog.Matches(row, _filter, _search))
                {
                    _rows.Add(row);
                }
            }

            _packageList.Clear();
            for (var i = 0; i < _rows.Count; i++)
            {
                _packageList.Add(BuildPackageRow(_rows[i]));
            }

            var searching = !string.IsNullOrWhiteSpace(_search) || _filter != ExtensionPackageFilter.All;
            _listHeaderLabel.text = searching
                ? "扩展包（" + _rows.Count + "/" + _allRows.Count + "）"
                : "扩展包（" + _allRows.Count + "）";

            _summaryLabel.text = _allRows.Count + " 个扩展包，" + updateCount + " 个可更新。";

            if (_allRows.Count == 0)
            {
                _packageList.Add(BuildMessage(_index == null
                    ? "还没有扩展包列表。用菜单栏「索引 → 刷新列表」重试。"
                    : "索引里没有扩展包。"));
            }
            else if (_rows.Count == 0)
            {
                _packageList.Add(BuildMessage("没有匹配的扩展包。"));
            }
        }

        private VisualElement BuildPackageRow(ExtensionPackageRow row)
        {
            var selected = !string.IsNullOrEmpty(_selectedName) && row.Name == _selectedName;

            var element = new VisualElement();
            element.style.flexDirection = FlexDirection.Row;
            element.style.alignItems = Align.Center;
            element.style.paddingLeft = 6f;
            element.style.paddingRight = 6f;
            element.style.paddingTop = 3f;
            element.style.paddingBottom = 3f;
            element.style.backgroundColor = selected ? SelectedColor : Color.clear;

            var mark = new Label(StateMark(row.State));
            mark.style.width = MarkWidth;
            mark.style.flexShrink = 0f;
            mark.style.color = StateColor(row.State);
            element.Add(mark);

            var text = new VisualElement();
            text.style.flexGrow = 1f;
            text.style.overflow = Overflow.Hidden;

            var name = new Label(row.DisplayName);
            name.style.unityFontStyleAndWeight = FontStyle.Bold;
            name.style.overflow = Overflow.Hidden;
            text.Add(name);

            var detail = new Label(BuildRowDetail(row));
            detail.style.color = DimColor;
            detail.style.fontSize = 10f;
            detail.style.overflow = Overflow.Hidden;
            text.Add(detail);

            element.Add(text);

            if (!selected)
            {
                element.RegisterCallback<MouseEnterEvent>(_ => element.style.backgroundColor = HoverColor);
                element.RegisterCallback<MouseLeaveEvent>(_ => element.style.backgroundColor = Color.clear);
            }

            var packageName = row.Name;
            element.RegisterCallback<ClickEvent>(_ => SelectPackage(packageName));
            return element;
        }

        private void SelectPackage(string packageName)
        {
            _selectedName = packageName ?? string.Empty;
            FangHubPageState.SetString(StateScope, SelectedKey, _selectedName);
            RebuildList();
            RebuildDetail();
        }

        private void RebuildDetail()
        {
            if (_detailContent == null)
            {
                return;
            }

            _detailContent.Clear();

            var row = FindRow(_selectedName);
            if (row == null)
            {
                _detailContent.Add(BuildMessage(_allRows.Count == 0
                    ? "还没有可查看的扩展包。"
                    : "在左侧列表里选一个扩展包，右侧显示它的详情与操作。"));
                return;
            }

            _detailContent.Add(BuildDetailHeader(row));

            var basic = BuildCollapsibleSection("基本信息", false);
            basic.Add(BuildFieldRow("显示名", row.DisplayName));
            basic.Add(BuildFieldRow("包标识", row.Name));
            basic.Add(BuildFieldRow("索引版本", string.IsNullOrEmpty(row.IndexVersion) ? "（索引未提供）" : row.IndexVersion));
            basic.Add(BuildFieldRow("本地版本", string.IsNullOrEmpty(row.InstalledVersion) ? "（未安装）" : row.InstalledVersion));
            basic.Add(BuildFieldRow("来源", row.Local == null ? "（未安装）" : ExtensionPackageCatalog.DescribeSource(row.Local.Source)));
            basic.Add(BuildFieldRow("最低引擎", row.Entry == null || string.IsNullOrEmpty(row.Entry.unity) ? "（未声明）" : row.Entry.unity));
            basic.Add(BuildFieldRow("仓库路径", row.Entry == null || string.IsNullOrEmpty(row.Entry.path) ? "（未声明）" : row.Entry.path));
            basic.Add(BuildFieldRow("tag", row.Entry == null || string.IsNullOrEmpty(row.Entry.tag) ? "（未声明）" : row.Entry.tag));
            _detailContent.Add(basic);

            _detailContent.Add(BuildSection("描述"));
            _detailContent.Add(BuildMessage(string.IsNullOrWhiteSpace(row.Description)
                ? "索引与本地 package.json 都没有写描述。"
                : row.Description));

            _detailContent.Add(BuildSection("操作"));
            _detailContent.Add(BuildActionRow(row));
        }

        private static VisualElement BuildDetailHeader(ExtensionPackageRow row)
        {
            var header = new VisualElement();

            var title = new Label(row.DisplayName);
            title.style.fontSize = 14f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(title);

            var id = new Label(row.Name);
            id.style.color = DimColor;
            header.Add(id);

            var state = new Label(DescribeState(row));
            state.style.whiteSpace = WhiteSpace.Normal;
            state.style.color = row.State == ExtensionPackageState.UpdateAvailable ? WarnColor : DimColor;
            header.Add(state);

            return header;
        }

        private VisualElement BuildActionRow(ExtensionPackageRow row)
        {
            var group = new VisualElement();

            var row1 = new VisualElement();
            row1.style.flexDirection = FlexDirection.Row;
            row1.style.alignItems = Align.Center;

            var action = ActionText(row.State);
            if (action.Length > 0)
            {
                var button = new Button(() => OnPackageAction(row)) { text = action };
                button.SetEnabled(!_installer.IsBusy && _coreInstalled);
                row1.Add(button);
            }
            else
            {
                var note = new Label("内嵌包：本仓库开发中，不提供安装 / 卸载。");
                note.style.color = DimColor;
                row1.Add(note);
            }

            var localPath = row.Local == null ? string.Empty : row.Local.PackageJsonPath;
            if (!string.IsNullOrEmpty(localPath))
            {
                row1.Add(new Button(() => Ping(localPath)) { text = "定位 package.json" });
            }

            group.Add(row1);

            if (!_coreInstalled)
            {
                var warning = new Label("核心包未装，安装与更新已禁用 —— 扩展包依赖核心包。");
                warning.style.color = WarnColor;
                warning.style.whiteSpace = WhiteSpace.Normal;
                group.Add(warning);
            }

            return group;
        }

        private void OnPackageAction(ExtensionPackageRow row)
        {
            switch (row.State)
            {
                case ExtensionPackageState.NotInstalled:
                case ExtensionPackageState.UpdateAvailable:
                    _installer.Install(row.Name, ExtensionPackageIndex.BuildInstallUrl(_index.repository, row.Entry));
                    break;
                case ExtensionPackageState.Installed:
                case ExtensionPackageState.LocalAhead:
                    _installer.Remove(row.Name);
                    break;
            }
        }

        private void OnCoreAction()
        {
            if (_index == null || _index.core == null)
            {
                SetStatus("索引还没拉到，无法更新核心包。");
                return;
            }

            _installer.Install(CorePackageName, ExtensionPackageIndex.BuildInstallUrl(_index.repository, _index.core));
        }

        private void RefreshIndex()
        {
            if (_fetch != null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(_indexUrl))
            {
                _indexUrl = DefaultIndexUrl;
                SetStatus("索引地址为空，已用默认地址。");
            }

            SetStatus("正在拉取扩展包列表…");

            if (_indexUrl.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    ApplyIndexText(File.ReadAllText(new Uri(_indexUrl).LocalPath));
                }
                catch (Exception exception)
                {
                    SetStatus("索引读取失败：" + exception.Message);
                }

                return;
            }

            _fetch = UnityWebRequest.Get(_indexUrl);
            _fetch.SendWebRequest();
            EditorApplication.update += TickFetch;
        }

        private void ResetIndexUrl()
        {
            _indexUrl = DefaultIndexUrl;
            EditorPrefs.SetString(IndexUrlKey, _indexUrl);

            if (_indexField != null)
            {
                _indexField.value = _indexUrl;
            }

            RefreshIndex();
        }

        private void TickFetch()
        {
            if (_fetch == null || !_fetch.isDone)
            {
                return;
            }

            EditorApplication.update -= TickFetch;

            var request = _fetch;
            _fetch = null;

            if (request.result != UnityWebRequest.Result.Success)
            {
                SetStatus("列表拉取失败：" + request.error + "（可点「刷新」重试）");
            }
            else
            {
                ApplyIndexText(request.downloadHandler.text);
            }

            request.Dispose();
        }

        private void ApplyIndexText(string text)
        {
            _index = ExtensionPackageIndex.Parse(text);
            SetStatus(_index == null
                ? "索引解析失败：不是合法索引文件。"
                : "共 " + _index.packages.Length + " 个扩展包。");
            Refresh();
        }

        private void OnInstallerChanged()
        {
            Refresh();

            if (_statusLabel != null && !string.IsNullOrEmpty(_installer.Status))
            {
                _statusLabel.text = _installer.Status;
            }
        }

        private void SetStatus(string message)
        {
            if (_statusLabel != null)
            {
                _statusLabel.text = message;
            }
        }

        private List<ExtensionPackageLocalInfo> CollectInstalled()
        {
            var installed = new List<ExtensionPackageLocalInfo>();

            if (_index == null || _index.packages == null)
            {
                return installed;
            }

            for (var i = 0; i < _index.packages.Length; i++)
            {
                var entry = _index.packages[i];
                if (entry == null)
                {
                    continue;
                }

                var local = ResolveLocal(entry.name);
                if (local != null)
                {
                    installed.Add(local);
                }
            }

            return installed;
        }

        private ExtensionPackageLocalInfo ResolveLocal(string name)
        {
            return _installer.TryGetInstalled(name, out var package) ? ToLocalInfo(package) : null;
        }

        private static ExtensionPackageLocalInfo ToLocalInfo(PackageInfo package)
        {
            if (package == null)
            {
                return null;
            }

            var info = new ExtensionPackageLocalInfo
            {
                Name = package.name,
                Version = package.version,
                Source = package.source,
                IsEmbedded = package.source == PackageSource.Embedded,
                Description = string.Empty,
                PackageJsonPath = "Packages/" + package.name + "/package.json"
            };

            if (ExtensionPackageCatalog.TryReadManifestDescription(package.resolvedPath, out var description))
            {
                info.Description = description;
            }

            return info;
        }

        private ExtensionPackageRow FindRow(string packageName)
        {
            if (string.IsNullOrEmpty(packageName))
            {
                return null;
            }

            for (var i = 0; i < _allRows.Count; i++)
            {
                if (_allRows[i].Name == packageName)
                {
                    return _allRows[i];
                }
            }

            return null;
        }

        private static string BuildRowDetail(ExtensionPackageRow row)
        {
            var installed = string.IsNullOrEmpty(row.InstalledVersion) ? "未装" : row.InstalledVersion;
            return row.Name + "  ·  索引 " + row.IndexVersion + " / 本地 " + installed;
        }

        private static string DescribeState(ExtensionPackageRow row)
        {
            switch (row.State)
            {
                case ExtensionPackageState.NotInstalled:
                    return "未安装";
                case ExtensionPackageState.UpdateAvailable:
                    return "可更新 " + row.InstalledVersion + " → " + row.IndexVersion;
                case ExtensionPackageState.LocalAhead:
                    return "本地领先索引（" + row.InstalledVersion + "）";
                case ExtensionPackageState.Embedded:
                    return "内嵌包（本仓库开发）";
                default:
                    return "已是最新（" + row.InstalledVersion + "）";
            }
        }

        private static string StateMark(ExtensionPackageState state)
        {
            switch (state)
            {
                case ExtensionPackageState.Installed:
                    return "✓";
                case ExtensionPackageState.UpdateAvailable:
                case ExtensionPackageState.LocalAhead:
                    return "↑";
                case ExtensionPackageState.Embedded:
                    return "▣";
                default:
                    return "○";
            }
        }

        private static Color StateColor(ExtensionPackageState state)
        {
            switch (state)
            {
                case ExtensionPackageState.Installed:
                    return Color.white;
                case ExtensionPackageState.UpdateAvailable:
                    return WarnColor;
                default:
                    return DimColor;
            }
        }

        private static string ActionText(ExtensionPackageState state)
        {
            switch (state)
            {
                case ExtensionPackageState.NotInstalled:
                    return "安装";
                case ExtensionPackageState.UpdateAvailable:
                    return "更新";
                case ExtensionPackageState.Installed:
                case ExtensionPackageState.LocalAhead:
                    return "卸载";
                default:
                    return string.Empty;
            }
        }

        private static ExtensionPackageFilter ParseFilter(string name)
        {
            for (var i = 0; i < FilterNames.Length; i++)
            {
                if (FilterNames[i] == name)
                {
                    return (ExtensionPackageFilter)i;
                }
            }

            return ExtensionPackageFilter.All;
        }

        private static VisualElement BuildFieldRow(string label, string value)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            var name = new Label(label);
            name.style.width = DetailLabelWidth;
            name.style.flexShrink = 0f;
            name.style.color = DimColor;
            row.Add(name);

            var text = new Label(string.IsNullOrEmpty(value) ? "（空）" : value);
            text.style.flexGrow = 1f;
            text.style.whiteSpace = WhiteSpace.Normal;
            text.style.color = DimColor;
            row.Add(text);

            return row;
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

        private static void Ping(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (asset != null)
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
                return;
            }

            if (File.Exists(assetPath))
            {
                EditorUtility.RevealInFinder(Path.GetFullPath(assetPath));
            }
        }
    }
}
