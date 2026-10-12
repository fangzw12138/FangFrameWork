#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Fang.Framework;
using Fang.Framework.Async;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Fang.Framework.CommandConsole.Async
{
    /// <summary>
    /// 指令服务：<see cref="AsyncService"/> + <see cref="ITickable"/>。
    /// 它被装进哪个 <see cref="AsyncScope"/>，上下文查找就以哪个 Scope 为起点；
    /// 驱动一律由 <c>AsyncScope.Tick</c> 来（本类不写任何 Unity 消息方法）。
    /// </summary>
    public sealed class DebugCommandService : AsyncService, ITickable
    {
        private const float TickingWindow = 1f;

        private static readonly List<DebugCommandService> LiveServices = new List<DebugCommandService>();

        private readonly List<string> _prefixes = new List<string>();

        private DebugProjectSo _project;
        private TcpDebugCommandServer _transport;
        private string _listenAddress = DebugProjectSo.DefaultListenAddress;
        private int _port = DebugProjectSo.DefaultPort;
        private bool _autoStart = true;
        private float _lastTickRealtime = float.NegativeInfinity;

        /// <summary>当前存活的指令服务（读时剔除已销毁的实例），供编辑器窗口使用。</summary>
        public static IReadOnlyList<DebugCommandService> ActiveServices
        {
            get
            {
                for (var i = LiveServices.Count - 1; i >= 0; i--)
                {
                    if (LiveServices[i] == null)
                    {
                        LiveServices.RemoveAt(i);
                    }
                }

                return LiveServices;
            }
        }

        /// <summary>指令注册表；服务初始化后可用。</summary>
        public DebugCommandRegistry Registry { get; private set; }

        /// <summary>当前生效的配置资产（可能为 null = 全默认）。</summary>
        public DebugProjectSo Project => _project;

        /// <summary>是否正在监听。</summary>
        public bool IsListening => _transport != null && _transport.IsListening;

        /// <summary>当前监听端口。</summary>
        public int Port => _port;

        /// <summary>当前监听地址。</summary>
        public string ListenAddress => _listenAddress;

        /// <summary>当前是否自动启动。</summary>
        public bool AutoStart => _autoStart;

        /// <summary>已接入的客户端数。</summary>
        public int ConnectedClients => _transport?.ClientCount ?? 0;

        /// <summary>被 tick 过的次数。</summary>
        public int TickCount { get; private set; }

        /// <summary>最近 1 秒内是否被 tick 过（用于验证「服务确实挂在树上」）。</summary>
        public bool IsTicking => Time.realtimeSinceStartup - _lastTickRealtime <= TickingWindow;

        /// <summary>
        /// 应用配置（可传 null = 全默认）。幂等：同样的参数重复调用不会重启端口。
        /// </summary>
        public void Configure(DebugProjectSo project)
        {
            var address = project != null ? project.ListenAddress : DebugProjectSo.DefaultListenAddress;
            var port = project != null ? project.Port : DebugProjectSo.DefaultPort;
            var autoStart = project == null || project.AutoStart;
            var prefixesChanged = !PrefixesEqual(project);

            _project = project;
            _listenAddress = address;
            _port = port;
            _autoStart = autoStart;

            ApplyPrefixes(project);

            if (prefixesChanged)
            {
                Rescan();
            }

            if (_autoStart)
            {
                StartTransport();
            }
            else
            {
                StopTransport();
            }
        }

        /// <summary>清空注册表并按当前扫描范围重新扫描已加载程序集。</summary>
        public int Rescan()
        {
            return Registry?.ScanLoadedAssemblies(_prefixes) ?? 0;
        }

        /// <summary>开始监听。已经在监听同一地址与端口时直接返回 true。</summary>
        public bool StartTransport()
        {
            if (Registry == null)
            {
                return false;
            }

            if (_transport != null &&
                _transport.IsListening &&
                _transport.Port == _port &&
                string.Equals(_transport.Address, _listenAddress, StringComparison.Ordinal))
            {
                return true;
            }

            StopTransport();

            var transport = new TcpDebugCommandServer(Registry);

            if (!transport.Start(_port, _listenAddress))
            {
                transport.Dispose();
                return false;
            }

            _transport = transport;
            return true;
        }

        /// <summary>停止监听。</summary>
        public void StopTransport()
        {
            if (_transport == null)
            {
                return;
            }

            _transport.Dispose();
            _transport = null;
        }

        /// <summary>进程内执行入口。</summary>
        public JObject Execute(string name, string[] args)
        {
            return Registry == null
                ? new JObject { ["ok"] = false, ["error"] = "指令注册表不可用（服务尚未初始化）。" }
                : Registry.Execute(name, args);
        }

        /// <summary>进程内执行入口（一行「空格分隔」的纯文本），返回单行 JSON。</summary>
        public string ExecuteText(string line)
        {
            return Registry == null
                ? new JObject { ["ok"] = false, ["error"] = "指令注册表不可用（服务尚未初始化）。" }.ToString(Formatting.None)
                : Registry.ExecuteText(line);
        }

        /// <summary>由 <c>AsyncScope.Tick</c> 驱动：记录心跳并排空传输队列（执行只发生在主线程）。</summary>
        public void OnTick(float deltaTime)
        {
            TickCount++;
            _lastTickRealtime = Time.realtimeSinceStartup;
            _transport?.Drain();
        }

        protected override UniTask OnInitAsync()
        {
            if (!LiveServices.Contains(this))
            {
                LiveServices.Add(this);
            }

            Registry = new DebugCommandRegistry();
            Registry.ContextFactory = CreateContext;

            Rescan();

            if (_autoStart)
            {
                StartTransport();
            }
            else
            {
                StopTransport();
            }

            return UniTask.CompletedTask;
        }

        protected override UniTask OnDisposeAsync()
        {
            LiveServices.Remove(this);
            StopTransport();

            Registry = null;
            _project = null;
            _prefixes.Clear();

            return UniTask.CompletedTask;
        }

        private DebugCommandContext CreateContext()
        {
            // 关键：把「查找起点」设为装这个服务的那个 AsyncScope。
            return new DebugCommandContext(Registry, () => Scope);
        }

        private bool PrefixesEqual(DebugProjectSo project)
        {
            var configured = project?.CommandAssemblyNamePrefixes;

            if (configured == null)
            {
                return _prefixes.Count == 0;
            }

            if (configured.Count != _prefixes.Count)
            {
                return false;
            }

            for (var i = 0; i < configured.Count; i++)
            {
                if (!string.Equals(configured[i], _prefixes[i], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private void ApplyPrefixes(DebugProjectSo project)
        {
            _prefixes.Clear();

            var configured = project?.CommandAssemblyNamePrefixes;

            if (configured == null)
            {
                return;
            }

            for (var i = 0; i < configured.Count; i++)
            {
                var prefix = configured[i];

                if (!string.IsNullOrWhiteSpace(prefix))
                {
                    _prefixes.Add(prefix.Trim());
                }
            }
        }
    }
}
#endif
