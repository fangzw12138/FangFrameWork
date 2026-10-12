#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using Fang.Framework;
using UnityEngine;

namespace Fang.Framework.CommandConsole.Async
{
    /// <summary>
    /// 指令层的工程级配置。对外**零写入口**，只有只读属性；非法值一律回落到默认值。
    /// </summary>
    [CreateAssetMenu(fileName = "DebugProject", menuName = "Fang/调试/指令项目配置", order = 0)]
    public sealed class DebugProjectSo : ConfigDataSo
    {
        /// <summary>默认监听地址。</summary>
        public const string DefaultListenAddress = "127.0.0.1";

        /// <summary>默认监听端口。</summary>
        public const int DefaultPort = 7777;

        [SerializeField]
        [Tooltip("TCP 监听地址；留空则回落 127.0.0.1。")]
        private string _listenAddress = DefaultListenAddress;

        [SerializeField]
        [Tooltip("TCP 监听端口；越界（小于 1 或大于 65535）则回落 7777。")]
        private int _port = DefaultPort;

        [SerializeField]
        [Tooltip("进入 Play 时是否自动开始监听。")]
        private bool _autoStart = true;

        [SerializeField]
        [Tooltip("非空时只扫描「程序集名以此前缀开头」的程序集；留空则只扫描引用了本包的程序集。")]
        private List<string> _commandAssemblyNamePrefixes = new List<string>();

        /// <summary>监听地址（空 → 127.0.0.1）。</summary>
        public string ListenAddress => string.IsNullOrWhiteSpace(_listenAddress) ? DefaultListenAddress : _listenAddress.Trim();

        /// <summary>监听端口（越界 → 7777）。</summary>
        public int Port => _port < 1 || _port > 65535 ? DefaultPort : _port;

        /// <summary>是否自动开始监听。</summary>
        public bool AutoStart => _autoStart;

        /// <summary>扫描范围前缀；为空表示「只扫引用了本包的程序集」。</summary>
        public IReadOnlyList<string> CommandAssemblyNamePrefixes => _commandAssemblyNamePrefixes;
    }
}
#endif
