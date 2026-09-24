#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace Fang.Framework.CommandConsole
{
    /// <summary>
    /// TCP 指令服务端（纯 C#）：后台线程监听，行分隔 JSON 协议，见 <see cref="DebugCommandProtocol"/>。
    ///
    /// 线程模型：Accept / Read 在后台线程，<b>只入队</b>；<see cref="Drain"/> 由主线程调用，
    /// 逐条执行并写回。这样指令内部可以放心碰 UnityEngine API，而慢客户端不会卡住主线程
    /// （两处参考实现里「后台读线程阻塞等主线程」的写法会把一客户端变成一线程一阻塞，已弃用）。
    ///
    /// 端口被占用等失败：<see cref="Start"/> 返回 false + LogError，<b>不抛</b>。
    /// </summary>
    public sealed class TcpDebugCommandServer : IDisposable
    {
        private readonly struct Incoming
        {
            public Incoming(TcpClient client, string line)
            {
                Client = client;
                Line = line;
            }

            public TcpClient Client { get; }

            public string Line { get; }
        }

        private readonly DebugCommandRegistry _registry;
        private readonly ConcurrentQueue<Incoming> _incoming = new ConcurrentQueue<Incoming>();
        private readonly object _clientsLock = new object();
        private readonly List<TcpClient> _clients = new List<TcpClient>();

        private TcpListener _listener;
        private Thread _acceptThread;
        private volatile bool _running;

        public TcpDebugCommandServer(DebugCommandRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public bool IsListening { get; private set; }

        /// <summary>实际绑定的端口（<see cref="Start"/> 传 0 时是系统分配的端口）。</summary>
        public int Port { get; private set; }

        /// <summary>实际绑定的地址。</summary>
        public string Address { get; private set; }

        /// <summary>
        /// 启动监听。已经在监听时先停掉再按新参数重开（所以是幂等的，重复调不会泄漏线程）。
        /// </summary>
        /// <param name="port">监听端口；传 0 让系统分配（测试用）。</param>
        /// <param name="address">监听地址，默认 <see cref="DebugCommandDefaults.ListenAddress"/>；必须是 IP。</param>
        public bool Start(int port, string address = null)
        {
            Stop();

            var bindAddress = string.IsNullOrWhiteSpace(address)
                ? DebugCommandDefaults.ListenAddress
                : address.Trim();

            if (!IPAddress.TryParse(bindAddress, out var ip))
            {
                Debug.LogError($"[DebugCommand] 监听地址必须是 IP（例如 127.0.0.1），实际是 '{bindAddress}'。");
                return false;
            }

            try
            {
                var listener = new TcpListener(ip, port);
                listener.Start();

                _listener = listener;

                var endpoint = (IPEndPoint)listener.LocalEndpoint;
                Port = endpoint.Port;
                Address = endpoint.Address.ToString();

                _running = true;
                _acceptThread = new Thread(AcceptLoop)
                {
                    IsBackground = true,
                    Name = "DebugCommandAccept",
                };
                _acceptThread.Start();

                IsListening = true;
                Debug.Log($"[DebugCommand] TCP 指令服务已启动: {Address}:{Port}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[DebugCommand] TCP 监听失败（端口 {port} 被占用？）: {e.Message}");
                IsListening = false;
                _running = false;
                SafeStopListener();
                return false;
            }
        }

        /// <summary>
        /// 主线程调用：消费请求队列，逐条执行并写回。必须在 Unity 主线程
        /// （指令内可能触碰 UnityEngine API）。返回本次处理的请求数。
        /// </summary>
        public int Drain()
        {
            var processed = 0;

            while (_incoming.TryDequeue(out var item))
            {
                TryWrite(item.Client, ProcessLine(item.Line));
                processed++;
            }

            return processed;
        }

        public void Dispose()
        {
            Stop();
        }

        private void Stop()
        {
            var wasListening = IsListening;

            _running = false;
            IsListening = false;

            SafeStopListener();
            _acceptThread = null;

            lock (_clientsLock)
            {
                for (var i = 0; i < _clients.Count; i++)
                {
                    try
                    {
                        _clients[i].Close();
                    }
                    catch (Exception)
                    {
                        // 对端已经断开：正常情况。
                    }
                }

                _clients.Clear();
            }

            // 停机时队列里的请求不再执行（它们没有意义了，回写也写不回去）。
            while (_incoming.TryDequeue(out _))
            {
            }

            if (wasListening)
            {
                Debug.Log("[DebugCommand] TCP 指令服务已停止。");
            }
        }

        private void SafeStopListener()
        {
            try
            {
                _listener?.Stop();
            }
            catch (Exception)
            {
                // Stop 抛错无所谓，监听已经不可用。
            }

            _listener = null;
        }

        // ──────────────────────────────────────────────
        // 后台线程
        // ──────────────────────────────────────────────

        private void AcceptLoop()
        {
            var listener = _listener;

            while (_running && listener != null)
            {
                TcpClient client;

                try
                {
                    client = listener.AcceptTcpClient();
                }
                catch (Exception)
                {
                    break; // 监听已停止
                }

                if (!_running)
                {
                    CloseQuietly(client);
                    break;
                }

                lock (_clientsLock)
                {
                    _clients.Add(client);
                }

                var reader = new Thread(() => ReadLoop(client))
                {
                    IsBackground = true,
                    Name = "DebugCommandRead",
                };
                reader.Start();
            }
        }

        private void ReadLoop(TcpClient client)
        {
            try
            {
                using (var reader = new StreamReader(client.GetStream(), DebugCommandProtocol.WireEncoding))
                {
                    while (_running)
                    {
                        var line = reader.ReadLine();
                        if (line == null)
                        {
                            break; // 对端关闭
                        }

                        if (string.IsNullOrWhiteSpace(line))
                        {
                            continue;
                        }

                        _incoming.Enqueue(new Incoming(client, line));
                    }
                }
            }
            catch (Exception)
            {
                // 对端断开 / IO 异常：正常清理。
            }
            finally
            {
                RemoveClient(client);
            }
        }

        private void RemoveClient(TcpClient client)
        {
            lock (_clientsLock)
            {
                _clients.Remove(client);
            }

            CloseQuietly(client);
        }

        private void TryWrite(TcpClient client, string response)
        {
            try
            {
                var bytes = DebugCommandProtocol.WireEncoding.GetBytes(response + "\n");
                var stream = client.GetStream();
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush();
            }
            catch (Exception)
            {
                RemoveClient(client);
            }
        }

        private static void CloseQuietly(TcpClient client)
        {
            try
            {
                client?.Close();
            }
            catch (Exception)
            {
                // 已经关过了。
            }
        }

        // ──────────────────────────────────────────────
        // 协议（主线程执行）
        // ──────────────────────────────────────────────

        private string ProcessLine(string line)
        {
            var request = DebugCommandRequest.Parse(line);

            if (!request.Ok)
            {
                return DebugCommandProtocol.FormatError(request.Error, request.Id);
            }

            var response = _registry.Execute(request.Command, request.Args);
            return DebugCommandProtocol.FormatResponse(response, request.Id);
        }
    }
}
#endif
