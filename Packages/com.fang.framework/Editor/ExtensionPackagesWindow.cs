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
            public Label Title;
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
        private ListView _list;
        private bool _coreInstalled;

        [MenuItem("Tools/Fang Framework/Extension Packages")]
        public static void Open()
        {
            var window = GetWindow<ExtensionPackagesWindow>("扩展包");
            window.minSize = new Vector2(560f, 360f);
        }

        private void CreateGUI()
        {
            _indexUrl = EditorPrefs.GetString(IndexUrlKey, DefaultIndexUrl);
            BuildTree();
            _installer.Changed += OnInstallerChanged;
            _installer.RefreshInstalledPackages();
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
            rootVisualElement.style.paddingLeft = 8f;
            rootVisualElement.style.paddingRight = 8f;
            rootVisualElement.style.paddingTop = 8f;
            rootVisualElement.style.paddingBottom = 8f;

            _indexField = new TextField("索引地址");
            _indexField.value = _indexUrl;
            _indexField.RegisterValueChangedCallback(evt =>
            {
                _indexUrl = evt.newValue;
                EditorPrefs.SetString(IndexUrlKey, _indexUrl);
            });

            var refreshButton = new Button(RefreshIndex);
            refreshButton.text = "刷新索引";

            var urlRow = new VisualElement();
            urlRow.style.flexDirection = FlexDirection.Row;
            urlRow.Add(_indexField);
            urlRow.Add(refreshButton);
            rootVisualElement.Add(urlRow);

            _coreLabel = new Label();
            _statusLabel = new Label();
            _statusLabel.style.whiteSpace = WhiteSpace.Normal;
            rootVisualElement.Add(_coreLabel);
            rootVisualElement.Add(_statusLabel);

            _list = new ListView();
            _list.makeItem = MakeRow;
            _list.bindItem = BindRow;
            _list.fixedItemHeight = 24f;
            _list.selectionType = SelectionType.None;
            _list.style.flexGrow = 1f;
            rootVisualElement.Add(_list);

            UpdateCoreLabel();
            UpdateStatusLabel();
        }

        private VisualElement MakeRow()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;

            var title = new Label();
            title.style.flexGrow = 1f;

            var meta = new Label();
            meta.style.width = 220f;

            var action = new Button();
            action.style.width = 80f;

            row.Add(title);
            row.Add(meta);
            row.Add(action);
            row.userData = new RowRefs { Title = title, Meta = meta, Action = action };
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

            refs.Title.text = row.DisplayName + "  " + row.Name;
            refs.Meta.text = "索引 " + row.IndexVersion + " / 本地 " + (row.InstalledVersion ?? "未装");
            refs.Action.text = ActionText(row.State);
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
                SetStatus("请先填写索引地址。");
                return;
            }

            SetStatus("正在拉取索引…");

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
                SetStatus("索引拉取失败：" + request.error);
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
                : "索引已更新：" + _index.packages.Length + " 个扩展包。");
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
        }

        private void OnInstallerChanged()
        {
            RebuildRows();
            UpdateStatusLabel();
        }

        private void UpdateStatusLabel()
        {
            if (_statusLabel != null)
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
                    ? "核心包：已装 " + core.version + "（" + core.source + "）"
                    : "核心包：未装 —— 扩展包依赖核心包，请先安装核心包。";
            }
        }
    }
}
