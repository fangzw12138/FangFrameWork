using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Fang.Framework.Editor
{
    public sealed class ExtensionPackageInstaller
    {
        private readonly Dictionary<string, PackageInfo> _installed = new Dictionary<string, PackageInfo>();
        private Request _pending;
        private Action _onSuccess;
        private Action<string> _onFailure;

        public bool IsBusy { get; private set; }

        public string Status { get; private set; } = string.Empty;

        public event Action Changed;

        public void RefreshInstalledPackages(string completedMessage = null)
        {
            if (IsBusy)
            {
                return;
            }

            var request = Client.List(true, true);
            BeginPoll(request, () =>
            {
                ApplyInstalled(request);
                Status = completedMessage ?? string.Empty;
            });
        }

        public bool TryGetInstalled(string name, out PackageInfo info)
        {
            if (string.IsNullOrEmpty(name))
            {
                info = null;
                return false;
            }

            return _installed.TryGetValue(name, out info);
        }

        public void Install(string packageName, string url)
        {
            if (IsBusy)
            {
                return;
            }

            if (string.IsNullOrEmpty(url))
            {
                SetStatus("安装失败：" + packageName + "：索引里缺少 path/tag。");
                return;
            }

            SetStatus("正在检查依赖：" + UnitaskDependency.PackageName);
            var request = Client.List(true, true);
            BeginPoll(request, () => InstallAfterDependencyCheck(packageName, url, request));
        }

        private void InstallAfterDependencyCheck(string packageName, string url, ListRequest request)
        {
            ApplyInstalled(request);

            if (_installed.ContainsKey(UnitaskDependency.PackageName))
            {
                InstallPackage(packageName, url);
                return;
            }

            bool changed;
            string error;
            if (!UnitaskDependency.TryEnsureRegistry(out changed, out error))
            {
                SetStatus("安装已中止：" + error);
                return;
            }

            if (!changed)
            {
                InstallUnitask(packageName, url);
                return;
            }

            SetStatus("已写入 OpenUPM 注册表，正在解析…");
            var resolve = Client.List(false, true);
            BeginPoll(resolve, () =>
            {
                UnitaskDependency.Commit();
                InstallUnitask(packageName, url);
            }, message =>
            {
                string restoreError;
                UnitaskDependency.TryRestore(out restoreError);
                SetStatus(string.IsNullOrEmpty(restoreError)
                    ? "安装已中止：OpenUPM 注册表未生效（" + message + "），manifest.json 已还原。"
                    : "安装已中止：OpenUPM 注册表未生效（" + message + "），且" + restoreError);
            });
        }

        private void InstallUnitask(string packageName, string url)
        {
            SetStatus("正在安装依赖：" + UnitaskDependency.Identifier);
            var request = Client.Add(UnitaskDependency.Identifier);
            BeginPoll(request, () => InstallPackage(packageName, url), message =>
            {
                SetStatus("安装失败：" + UnitaskDependency.PackageName + "：" + message
                    + "（可参考「快速开始 → 首次安装」手动装依赖）");
            });
        }

        private void InstallPackage(string packageName, string url)
        {
            SetStatus("正在安装：" + packageName);
            var request = Client.Add(url);
            BeginPoll(request, () =>
            {
                AssetDatabase.Refresh();
                RefreshInstalledPackages("已安装 " + packageName + "。");
            });
        }

        private void ApplyInstalled(ListRequest request)
        {
            _installed.Clear();
            foreach (var package in request.Result)
            {
                _installed[package.name] = package;
            }
        }

        public void Remove(string packageName)
        {
            if (IsBusy)
            {
                return;
            }

            SetStatus("正在卸载：" + packageName);
            BeginPoll(Client.Remove(packageName), () =>
            {
                AssetDatabase.Refresh();
                RefreshInstalledPackages("已卸载 " + packageName + "。");
            });
        }

        private void BeginPoll(Request request, Action onSuccess, Action<string> onFailure = null)
        {
            IsBusy = true;
            _pending = request;
            _onSuccess = onSuccess;
            _onFailure = onFailure ?? (message => SetStatus("操作失败：" + message));
            EditorApplication.update += Tick;
            RaiseChanged();
        }

        private void Tick()
        {
            if (_pending == null)
            {
                EditorApplication.update -= Tick;
                return;
            }

            if (!_pending.IsCompleted)
            {
                return;
            }

            EditorApplication.update -= Tick;

            var request = _pending;
            var onSuccess = _onSuccess;
            var onFailure = _onFailure;
            _pending = null;
            _onSuccess = null;
            _onFailure = null;
            IsBusy = false;

            if (request.Status == StatusCode.Success)
            {
                onSuccess?.Invoke();
            }
            else
            {
                var message = request.Error != null ? request.Error.message : "未知错误";
                onFailure?.Invoke(message);
            }

            RaiseChanged();
        }

        private void SetStatus(string message)
        {
            Status = message;
            RaiseChanged();
        }

        private void RaiseChanged()
        {
            Changed?.Invoke();
        }
    }
}
