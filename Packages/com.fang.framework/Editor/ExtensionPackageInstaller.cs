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

        public void RefreshInstalledPackages()
        {
            if (IsBusy)
            {
                return;
            }

            var request = Client.List(true, true);
            BeginPoll(request, () =>
            {
                _installed.Clear();
                foreach (var package in request.Result)
                {
                    _installed[package.name] = package;
                }

                Status = "已读取本地包状态。";
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

            SetStatus("正在安装：" + packageName);
            BeginPoll(Client.Add(url), () =>
            {
                AssetDatabase.Refresh();
                RefreshInstalledPackages();
            });
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
                RefreshInstalledPackages();
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
