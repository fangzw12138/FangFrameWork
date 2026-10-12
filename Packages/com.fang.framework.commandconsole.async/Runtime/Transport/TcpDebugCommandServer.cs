#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Fang.Framework.CommandConsole.Async
{
    /// <summary>
    /// TCP 传输层：**后台线程只收**（把 <c>(clientId, line)</c> 入队），
    /// **主线程 <see cref="Drain"/>** 出队执行并回写。协议是一行一个 JSON（NDJSON），也接受纯文本行。
    /// </summary>
    public sealed class TcpDebugCommandServer : IDisposable
    {
        private const string LogTag = DebugCommandRegistry.LogTag;

        private readonly DebugCommandRegistry _registry;
        private readonly ConcurrentQueue<PendingRequest> _pending = new ConcurrentQueue<PendingRequest>();
        private readonly ConcurrentDictionary<int, ClientConnection> _clients = new ConcurrentDictionary<int, ClientConnection>();

        private TcpListener _listener;
        private Thread _acceptThread;
        private volatile bool _running;
        private int _nextClientId;

        public TcpDebugCommandServer(DebugCommandRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        /// <summary>是否正在监听。</summary>
        public bool IsListening => _running && _listener != null;

        /// <summary>当前（或最近一次）监听端口。</summary>
        public int Port { get; private set; }

        /// <summary>当前（或最近一次）监听地址。</summary>
        public string Address { get; private set; }

        /// <summary>已接入的客户端数。</summary>
        public int ClientCount => _clients.Count;

        /// <summary>启动监听。失败（端口占用等）返回 false 并打一条带端口与原因的 Error 日志。</summary>
        public bool Start(int port, string address)
        {
            if (_running)
            {
                return true;
            }

            if (port < 1 || port > 65535)
            {
                Debug.LogError($"{LogTag} 指令服务启动失败：端口 {port} 越界（允许 1~65535）。");
                return false;
            }

            var host = string.IsNullOrWhiteSpace(address) ? "127.0.0.1" : address.Trim();

            try
            {
                var endpoint = ResolveAddress(host);
                var listener = new TcpListener(endpoint, port);
                listener.Start();

                _listener = listener;
                Port = port;
                Address = host;
                _running = true;

                _acceptThread = new Thread(AcceptLoop)
                {
                    IsBackground = true,
                    Name = $"Fang.DebugCommand.Accept:{port}"
                };
                _acceptThread.Start();

                Debug.Log($"{LogTag} 指令服务已监听 {host}:{port}");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"{LogTag} 指令服务启动失败：无法监听 {host}:{port} —— {exception.Message}");
                Stop();
                return false;
            }
        }

        /// <summary>停止监听、断开全部客户端并清空待处理队列。</summary>
        public void Stop()
        {
            _running = false;

            var listener = _listener;
            _listener = null;

            if (listener != null)
            {
                try
                {
                    listener.Stop();
                }
                catch (Exception)
                {
                    // 已经在关闭中。
                }
            }

            foreach (var pair in _clients)
            {
                pair.Value.Close();
            }

            _clients.Clear();

            while (_pending.TryDequeue(out _))
            {
                // 丢弃未执行的请求。
            }

            var acceptThread = _acceptThread;
            _acceptThread = null;

            if (acceptThread != null && acceptThread != Thread.CurrentThread && acceptThread.IsAlive)
            {
                try
                {
                    acceptThread.Join(500);
                }
                catch (ThreadStateException)
                {
                    // 线程已退出。
                }
            }
        }

        /// <summary>主线程排空队列：执行指令并把结果写回对应连接。写失败即断开该连接。</summary>
        public void Drain()
        {
            while (_pending.TryDequeue(out var request))
            {
                if (!_clients.TryGetValue(request.ClientId, out var connection))
                {
                    continue;
                }

                string response;

                try
                {
                    response = _registry.ExecuteRequest(request.Line);
                }
                catch (Exception exception)
                {
                    response = Failure($"指令服务内部错误：{exception.Message}");
                    Debug.LogError($"{LogTag} 指令服务执行请求失败：{exception}");
                }

                if (!connection.TryWriteLine(response))
                {
                    DropClient(request.ClientId);
                }
            }
        }

        public void Dispose()
        {
            Stop();
        }

        private void AcceptLoop()
        {
            var listener = _listener;

            if (listener == null)
            {
                return;
            }

            while (_running)
            {
                TcpClient client;

                try
                {
                    client = listener.AcceptTcpClient();
                }
                catch (SocketException)
                {
                    // Stop() 打断 AcceptTcpClient 的正常路径。
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (InvalidOperationException)
                {
                    break;
                }
                catch (Exception exception)
                {
                    if (_running)
                    {
                        Debug.LogWarning($"{LogTag} 指令服务接受连接失败：{exception.Message}");
                    }

                    break;
                }

                if (!_running)
                {
                    SafeClose(client);
                    break;
                }

                RegisterClient(client);
            }
        }

        private void RegisterClient(TcpClient client)
        {
            var id = Interlocked.Increment(ref _nextClientId);
            var connection = new ClientConnection(id, client);

            if (!_clients.TryAdd(id, connection))
            {
                connection.Close();
                return;
            }

            var thread = new Thread(() => ReadLoop(connection))
            {
                IsBackground = true,
                Name = $"Fang.DebugCommand.Client:{id}"
            };

            thread.Start();
        }

        private void ReadLoop(ClientConnection connection)
        {
            try
            {
                using (var reader = new StreamReader(connection.Stream, new UTF8Encoding(false)))
                {
                    string line;

                    while (_running && (line = reader.ReadLine()) != null)
                    {
                        _pending.Enqueue(new PendingRequest(connection.Id, line));
                    }
                }
            }
            catch (IOException)
            {
                // 连接被对端或本端关闭。
            }
            catch (ObjectDisposedException)
            {
                // Stop() 关闭了底层流。
            }
            catch (Exception exception)
            {
                if (_running)
                {
                    Debug.LogWarning($"{LogTag} 指令服务读取客户端 #{connection.Id} 失败：{exception.Message}");
                }
            }
            finally
            {
                DropClient(connection.Id);
            }
        }

        private void DropClient(int clientId)
        {
            if (_clients.TryRemove(clientId, out var connection))
            {
                connection.Close();
            }
        }

        private static IPAddress ResolveAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return IPAddress.Loopback;
            }

            var trimmed = address.Trim();

            if (IPAddress.TryParse(trimmed, out var parsed))
            {
                return parsed;
            }

            if (string.Equals(trimmed, "localhost", StringComparison.OrdinalIgnoreCase))
            {
                return IPAddress.Loopback;
            }

            if (string.Equals(trimmed, "any", StringComparison.OrdinalIgnoreCase))
            {
                return IPAddress.Any;
            }

            var resolved = Dns.GetHostAddresses(trimmed);

            if (resolved == null || resolved.Length == 0)
            {
                throw new ArgumentException($"无法解析监听地址 '{address}'。", nameof(address));
            }

            return resolved[0];
        }

        private static string Failure(string error)
        {
            return new JObject
            {
                ["ok"] = false,
                ["error"] = error
            }.ToString(Formatting.None);
        }

        private static void SafeClose(TcpClient client)
        {
            try
            {
                client?.Close();
            }
            catch (Exception)
            {
                // 忽略。
            }
        }

        private readonly struct PendingRequest
        {
            public PendingRequest(int clientId, string line)
            {
                ClientId = clientId;
                Line = line;
            }

            public int ClientId { get; }

            public string Line { get; }
        }

        private sealed class ClientConnection
        {
            private readonly object _gate = new object();

            private bool _closed;

            public ClientConnection(int id, TcpClient client)
            {
                Id = id;
                Client = client;
                Stream = client.GetStream();
                Writer = new StreamWriter(Stream, new UTF8Encoding(false));
            }

            public int Id { get; }

            public TcpClient Client { get; }

            public NetworkStream Stream { get; }

            public StreamWriter Writer { get; }

            /// <summary>写一行并 flush（行分隔符是 <c>\n</c>）。失败返回 false。</summary>
            public bool TryWriteLine(string line)
            {
                lock (_gate)
                {
                    if (_closed)
                    {
                        return false;
                    }

                    try
                    {
                        Writer.Write(line);
                        Writer.Write('\n');
                        Writer.Flush();
                        return true;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
            }

            /// <summary>幂等关闭。</summary>
            public void Close()
            {
                lock (_gate)
                {
                    if (_closed)
                    {
                        return;
                    }

                    _closed = true;
                }

                try
                {
                    Writer.Dispose();
                }
                catch (Exception)
                {
                    // 对端已断开或流已被读线程释放。
                }

                try
                {
                    Client.Close();
                }
                catch (Exception)
                {
                    // 忽略。
                }
            }
        }
    }
}
#endif
