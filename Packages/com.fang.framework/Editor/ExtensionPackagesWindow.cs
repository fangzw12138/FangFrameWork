using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

namespace Fang.Framework.Editor
{
    public sealed class ExtensionPackagesWindow : EditorWindow
    {
        private const string IndexUrlKey = "Fang.Framework.ExtensionPackages.IndexUrl";
        private const string CorePackageName = "com.fang.framework";
        private const string DefaultIndexUrl = "https://raw.githubusercontent.com/fangzw12138/FangFrameWork/main/packages.json";

        private const float NameColumnWidth = 160f;
        private const float MetaColumnWidth = 200f;
        private const float ActionColumnWidth = 84f;
        private const float RowHeight = 26f;

        private static readonly Color DimColor = new Color(0.65f, 0.65f, 0.65f);
        private static readonly Color LineColor = new Color(0.28f, 0.28f, 0.28f);

        private sealed class RowModel
        {
            public ExtensionPackageEntry Entry;
            public string Name;
            public string DisplayName;
            public string IndexVersion;
            public string InstalledVersion;
            public ExtensionPackageState State;
        }

        private sealed class RowRefs
        {
            public Label Name;
            public Label Id;
            public Label Meta;
            public Button Action;
            public Action Handler;
        }

        private readonly ExtensionPackageInstaller _installer = new ExtensionPackageInstaller();
        private readonly List<RowModel> _rows = new List<RowModel>();
        private string _indexUrl;
        private ExtensionPackageIndexDocument _index;
        private UnityWebRequest _fetch;
        private TextField _indexField;
        private Label _coreLabel;
        private Label _statusLabel;
        private VisualElement _headerRow;
        private ListView _list;
        private Label _emptyLabel;
        private bool _coreInstalled;

        [MenuItem("Tools/Fang Framework/Extension Packages")]
        public static void Open()
        {
            var window = GetWindow<ExtensionPackagesWindow>("扩展包");
            window.minSize = new Vector2(620f, 380f);
        }

        private void CreateGUI()
        {
            _indexUrl = EditorPrefs.GetString(IndexUrlKey, DefaultIndexUrl);
            BuildTree();
            _installer.Changed += OnInstallerChanged;
            _installer.RefreshInstalledPackages();

            if (_index == null)
            {
                RefreshIndex();
            }
        }

        private void OnDisable()
        {
            _installer.Changed -= OnInstallerChanged;
            EditorApplication.update -= TickFetch;

            if (_fetch != null)
            {
                _fetch.Dispose();
                _fetch = null;
            }
        }

        private void BuildTree()
        {
            rootVisualElement.style.paddingLeft = 10f;
            rootVisualElement.style.paddingRight = 10f;
            rootVisualElement.style.paddingTop = 8f;
            rootVisualElement.style.paddingBottom = 8f;

            var toolbar = new VisualElement();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.alignItems = Align.Center;

            var refreshButton = new Button(RefreshIndex);
            refreshButton.text = "刷新";

            _statusLabel = new Label();
            _statusLabel.style.marginLeft = 8f;
            _statusLabel.style.whiteSpace = WhiteSpace.Normal;
            _statusLabel.style.color = DimColor;

            toolbar.Add(refreshButton);
            toolbar.Add(_statusLabel);
            rootVisualElement.Add(toolbar);

            _coreLabel = new Label();
            _coreLabel.style.marginTop = 4f;
            _coreLabel.style.color = DimColor;
            rootVisualElement.Add(_coreLabel);

            _headerRow = BuildHeaderRow();
            rootVisualElement.Add(_headerRow);

            _list = new ListView();
            _list.makeItem = MakeRow;
            _list.bindItem = BindRow;
            _list.fixedItemHeight = RowHeight;
            _list.selectionType = SelectionType.None;
            _list.showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly;
            _list.style.flexGrow = 1f;
            rootVisualElement.Add(_list);

            _emptyLabel = new Label();
            _emptyLabel.style.whiteSpace = WhiteSpace.Normal;
            _emptyLabel.style.marginTop = 10f;
            _emptyLabel.style.color = DimColor;
            rootVisualElement.Add(_emptyLabel);

            rootVisualElement.Add(BuildAdvancedSection());

            UpdateCoreLabel();
            UpdateListVisibility();
        }

        private VisualElement BuildAdvancedSection()
        {
            var advanced = new Foldout();
            advanced.text = "索引设置（高级）";
            advanced.value = false;
            advanced.style.marginTop = 10f;

            _indexField = new TextField("索引地址");
            _indexField.value = _indexUrl;
            _indexField.style.flexGrow = 1f;
            _indexField.RegisterValueChangedCallback(evt =>
            {
                _indexUrl = evt.newValue;
                EditorPrefs.SetString(IndexUrlKey, _indexUrl);
            });

            var resetButton = new Button(() =>
            {
                _indexField.value = DefaultIndexUrl;
                RefreshIndex();
            });
            resetButton.text = "恢复默认";
            resetButton.style.marginLeft = 6f;
            resetButton.style.marginBottom = 2f;

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.Add(_indexField);
            row.Add(resetButton);

            var hint = new Label();
            hint.style.whiteSpace = WhiteSpace.Normal;
            hint.style.color = DimColor;
            hint.style.fontSize = 11f;
            hint.text = "索引是仓库根目录的 packages.json，窗口靠它知道有哪些扩展包、各自在哪个 tag。"
                + "只有离线验证才需要改成 file:// 本地索引。";

            advanced.Add(row);
            advanced.Add(hint);
            return advanced;
        }

        private static VisualElement BuildHeaderRow()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = 10f;
            row.style.paddingBottom = 3f;
            row.style.borderBottomWidth = 1f;
            row.style.borderBottomColor = LineColor;

            var name = new Label("包");
            name.style.width = NameColumnWidth;
            name.style.flexShrink = 0f;
            name.style.color = DimColor;

            var id = new Label("包标识");
            id.style.flexGrow = 1f;
            id.style.color = DimColor;

            var meta = new Label("索引 / 本地");
            meta.style.width = MetaColumnWidth;
            meta.style.flexShrink = 0f;
            meta.style.color = DimColor;

            var action = new Label("操作");
            action.style.width = ActionColumnWidth;
            action.style.flexShrink = 0f;
            action.style.color = DimColor;

            row.Add(name);
            row.Add(id);
            row.Add(meta);
            row.Add(action);
            return row;
        }

        private VisualElement MakeRow()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            var name = new Label();
            name.style.width = NameColumnWidth;
            name.style.flexShrink = 0f;
            name.style.unityFontStyleAndWeight = FontStyle.Bold;

            var id = new Label();
            id.style.flexGrow = 1f;
            id.style.fontSize = 11f;
            id.style.color = DimColor;

            var meta = new Label();
            meta.style.width = MetaColumnWidth;
            meta.style.flexShrink = 0f;

            var action = new Button();
            action.style.width = ActionColumnWidth;
            action.style.flexShrink = 0f;

            row.Add(name);
            row.Add(id);
            row.Add(meta);
            row.Add(action);
            row.userData = new RowRefs { Name = name, Id = id, Meta = meta, Action = action };
            return row;
        }

        private void BindRow(VisualElement element, int index)
        {
            if (index < 0 || index >= _rows.Count)
            {
                return;
            }

            var refs = (RowRefs)element.userData;
            var row = _rows[index];

            refs.Name.text = row.DisplayName;
            refs.Id.text = row.Name;
            refs.Meta.text = "索引 " + row.IndexVersion + " / 本地 " + (row.InstalledVersion ?? "未装")
                + (row.State == ExtensionPackageState.Embedded ? "（内嵌）" : string.Empty);

            refs.Action.text = ActionText(row.State);
            refs.Action.style.visibility = row.State == ExtensionPackageState.Embedded
                ? Visibility.Hidden
                : Visibility.Visible;
            refs.Action.SetEnabled(
                !_installer.IsBusy
                && _coreInstalled
                && row.State != ExtensionPackageState.Embedded);

            if (refs.Handler != null)
            {
                refs.Action.clicked -= refs.Handler;
            }

            refs.Handler = () => OnActionClicked(row);
            refs.Action.clicked += refs.Handler;
        }

        private void OnActionClicked(RowModel row)
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
            RebuildRows();
        }

        private void RebuildRows()
        {
            _rows.Clear();

            if (_index != null)
            {
                for (var i = 0; i < _index.packages.Length; i++)
                {
                    var entry = _index.packages[i];
                    _installer.TryGetInstalled(entry.name, out var installed);

                    _rows.Add(new RowModel
                    {
                        Entry = entry,
                        Name = entry.name,
                        DisplayName = string.IsNullOrEmpty(entry.displayName) ? entry.name : entry.displayName,
                        IndexVersion = entry.version,
                        InstalledVersion = installed?.version,
                        State = ExtensionPackageIndex.ResolveState(
                            entry.version,
                            installed?.version,
                            installed != null && installed.source == PackageSource.Embedded)
                    });
                }
            }

            if (_list != null)
            {
                _list.itemsSource = _rows;
                _list.Rebuild();
            }

            UpdateCoreLabel();
            UpdateListVisibility();
        }

        private void UpdateListVisibility()
        {
            var hasRows = _rows.Count > 0;

            if (_headerRow != null)
            {
                _headerRow.style.display = hasRows ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_list != null)
            {
                _list.style.display = hasRows ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_emptyLabel != null)
            {
                _emptyLabel.style.display = hasRows ? DisplayStyle.None : DisplayStyle.Flex;

                if (!hasRows)
                {
                    _emptyLabel.text = _index == null
                        ? "还没有扩展包列表。点「刷新」重试。"
                        : "索引里没有扩展包。";
                }
            }
        }

        private void OnInstallerChanged()
        {
            RebuildRows();
            UpdateStatusLabel();
        }

        private void UpdateStatusLabel()
        {
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

        private void UpdateCoreLabel()
        {
            _installer.TryGetInstalled(CorePackageName, out var core);
            _coreInstalled = core != null;

            if (_coreLabel != null)
            {
                _coreLabel.text = _coreInstalled
                    ? "核心包 " + core.version + "（" + core.source + "）"
                    : "核心包：读取中…";
            }
        }
    }
}
