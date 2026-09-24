#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Fang.Framework.CommandConsole
{
    /// <summary>
    /// 调试指令服务：装配「注册表 + TCP 传输 + 游戏内控制台」，由核心包 Scope 托管。
    ///
    /// 生命周期：<see cref="OnInit"/> 用内置默认值起来（装机即用）；<see cref="Configure"/> 套用项目配置（幂等）；
    /// <see cref="OnTick"/> 在主线程 Drain 请求队列；<see cref="OnDispose"/> 停服务。
    ///
    /// 本类<b>不写 Unity 消息方法</b>（主线程靠 <c>Scope.Tick</c> 驱动，符合核心包约定）——
    /// 所以「装了但没人 tick」是本包最典型的静默失败，<c>ping</c> 指令与 FangHub 页都会把它显示出来。
    /// </summary>
    public sealed class DebugCommandService : Service, ITickable
    {
        private static readonly List<DebugCommandService> LiveServices = new List<DebugCommandService>();

        private DebugCommandRegistry _registry;
        private TcpDebugCommandServer _transport;
        private DebugCommandConsole _console;
        private DebugSettings _settings = DebugSettings.From(null);

        /// <summary>
        /// 当前活着的服务（编辑器侧控制台窗口要用它，否则拿不到 PlayMode 里的实例）。
        /// 已销毁的引用会在读取时被过滤掉，所以不需要额外的清理钩子。
        /// </summary>
        public static IReadOnlyList<DebugCommandService> ActiveServices
        {
            get
            {
                LiveServices.RemoveAll(service => service == null);
                return LiveServices;
            }
        }

        public DebugCommandRegistry Registry => _registry;

        /// <summary>当前生效配置（来自 <see cref="Configure"/> 的 SO，或内置默认值）。</summary>
        public DebugSettings Settings => _settings;

        /// <summary>当前套用的项目配置；没 Configure 过就是 null。</summary>
        public DebugProjectSo Project { get; private set; }

        public bool IsListening => _transport != null && _transport.IsListening;

        /// <summary>实际监听端口；没在监听时回落到配置值。</summary>
        public int Port => _transport != null && _transport.IsListening ? _transport.Port : _settings.Port;

        /// <summary>实际监听地址；没在监听时回落到配置值。</summary>
        public string ListenAddress =>
            _transport != null && _transport.IsListening ? _transport.Address : _settings.ListenAddress;

        /// <summary>游戏内控制台（配置关掉时可能为 null）。</summary>
        public DebugCommandConsole Console => _console;

        public int TickCount { get; private set; }

        public float LastTickRealtime { get; private set; }

        /// <summary>最近一秒内被 tick 过。</summary>
        public bool IsTicking => TickCount > 0 && Time.realtimeSinceStartup - LastTickRealtime < 1f;

        public override void OnInit()
        {
            if (!LiveServices.Contains(this))
            {
                LiveServices.Add(this);
            }

            _registry = new DebugCommandRegistry();

            // 上下文默认走本服务所在的 Scope；找不到服务返回 null 而不是抛。
            _registry.ContextFactory = () => new DebugCommandContext(_registry, () => Scope);

            _settings = DebugSettings.From(null);
            Rescan();
            ApplySettings();
        }

        public override void OnDispose()
        {
            LiveServices.Remove(this);
            StopTransport();
            DestroyConsole();

            _registry = null;
            Project = null;
        }

        /// <summary>套用项目配置（幂等：同样的配置重复调不会重开端口、也不会重扫程序集）。</summary>
        public void Configure(DebugProjectSo project)
        {
            if (_registry == null)
            {
                return;
            }

            var previousPrefixes = _settings.CommandAssemblyNamePrefixes;

            Project = project;
            _settings = DebugSettings.From(project);

            if (!SameList(previousPrefixes, _settings.CommandAssemblyNamePrefixes))
            {
                Rescan();
            }

            ApplySettings();
        }

        /// <summary>
        /// 按当前扫描范围重扫并登记。
        /// 会先 <c>Clear</c> —— 扫描范围是「当前真相」，不留下上一轮的残留；
        /// 代价是手工 <c>Register</c> 的指令也会被清掉，所以手工注册要放在 Configure 之后。
        /// </summary>
        public int Rescan()
        {
            if (_registry == null)
            {
                return 0;
            }

            _registry.Clear();
            return _registry.ScanLoadedAssemblies(_settings.CommandAssemblyNamePrefixes);
        }

        /// <summary>开始监听（幂等：参数没变且已在监听就直接返回 true）。</summary>
        public bool StartTransport()
        {
            if (_registry == null)
            {
                return false;
            }

            if (_transport != null
                && _transport.IsListening
                && _transport.Port == _settings.Port
                && string.Equals(_transport.Address, _settings.ListenAddress, StringComparison.Ordinal))
            {
                return true;
            }

            if (_transport == null)
            {
                _transport = new TcpDebugCommandServer(_registry);
            }

            return _transport.Start(_settings.Port, _settings.ListenAddress);
        }

        public void StopTransport()
        {
            _transport?.Dispose();
        }

        /// <summary>进程内执行入口（游戏内部 / 编辑器窗口 / 测试用）。</summary>
        public JObject Execute(string commandName, string[] args)
        {
            return _registry != null
                ? _registry.Execute(commandName, args)
                : DebugCommandRegistry.Error("指令服务未初始化");
        }

        /// <summary>执行一行文本（控制台手工输入用），返回序列化 JSON。</summary>
        public string ExecuteText(string commandLine)
        {
            return _registry != null
                ? _registry.ExecuteText(commandLine)
                : DebugCommandRegistry.Error("指令服务未初始化").ToString(Formatting.None);
        }

        public void OnTick(float deltaTime)
        {
            TickCount++;
            LastTickRealtime = Time.realtimeSinceStartup;

            _transport?.Drain();
        }

        private void ApplySettings()
        {
            if (_settings.AutoStart)
            {
                StartTransport();
            }
            else
            {
                StopTransport();
            }

            ApplyConsole();
        }

        private void ApplyConsole()
        {
            if (_settings.ConsoleEnabled)
            {
                if (_console == null)
                {
                    CreateConsole();
                }

                _console.Apply(_settings.ConsoleToggleKey, _settings.QuickButtons);
                _console.SetEnabled(true);
                return;
            }

            _console?.SetEnabled(false);
        }

        private void CreateConsole()
        {
            var host = new GameObject("DebugCommandConsole");
            host.transform.SetParent(transform, false);

            _console = host.AddComponent<DebugCommandConsole>();
            _console.Initialize(this);
        }

        private void DestroyConsole()
        {
            if (_console == null)
            {
                return;
            }

            var host = _console.gameObject;
            _console = null;

            // 编辑器里（非播放）Destroy 会报错，必须走 DestroyImmediate。
            if (Application.isPlaying)
            {
                Destroy(host);
            }
            else
            {
                DestroyImmediate(host);
            }
        }

        private static bool SameList(IReadOnlyList<string> left, IReadOnlyList<string> right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left == null || right == null || left.Count != right.Count)
            {
                return false;
            }

            for (var i = 0; i < left.Count; i++)
            {
                if (!string.Equals(left[i], right[i], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
#endif
