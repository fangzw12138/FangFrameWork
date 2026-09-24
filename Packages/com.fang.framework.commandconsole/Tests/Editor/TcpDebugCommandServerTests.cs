using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Fang.Framework.CommandConsole.Editor.Tests
{
    /// <summary>
    /// TCP 传输：起真服务 + 真 TcpClient 走一次往返（不依赖任何游戏逻辑）。
    /// 用端口 0 让系统分配端口，避免和别的测试 / 别的程序抢固定端口。
    /// </summary>
    public class TcpDebugCommandServerTests
    {
        private DebugCommandRegistry _registry;
        private TcpDebugCommandServer _server;
        private TcpClient _client;
        private StreamWriter _writer;
        private StreamReader _reader;

        [SetUp]
        public void SetUp()
        {
            _registry = new DebugCommandRegistry();
            _registry.Register(
                "probe_ok",
                "正常指令 <value>",
                "probe",
                typeof(DebugCommandProbes).GetMethod(nameof(DebugCommandProbes.Ok)));

            _server = new TcpDebugCommandServer(_registry);
        }

        [TearDown]
        public void TearDown()
        {
            CloseClient();
            _server?.Dispose();
            _server = null;
            _registry = null;
        }

        private void CloseClient()
        {
            try
            {
                _writer?.Dispose();
            }
            catch (Exception)
            {
                // 已经关了。
            }

            try
            {
                _reader?.Dispose();
            }
            catch (Exception)
            {
                // 已经关了。
            }

            try
            {
                _client?.Close();
            }
            catch (Exception)
            {
                // 已经关了。
            }

            _writer = null;
            _reader = null;
            _client = null;
        }

        private void Connect()
        {
            _client = new TcpClient();
            _client.Connect(IPAddress.Loopback, _server.Port);

            var stream = _client.GetStream();
            _writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };
            _reader = new StreamReader(stream, Encoding.UTF8);
        }

        /// <summary>发一行，然后在主线程 Drain 直到收到响应（读在后台线程，不阻塞主线程）。</summary>
        private IEnumerator Send(string line, List<string> responses)
        {
            var read = Task.Run(() => _reader.ReadLine());
            _writer.WriteLine(line);

            var deadline = Time.realtimeSinceStartup + 5f;
            while (!read.IsCompleted && Time.realtimeSinceStartup < deadline)
            {
                _server.Drain();
                yield return null;
            }

            Assert.IsTrue(read.IsCompleted, "5 秒内没收到响应: " + line);
            responses.Add(read.Result);
        }

        private static List<string> NewResponses()
        {
            return new List<string>();
        }

        [Test]
        public void Start_WithPortZero_AssignsRealPort()
        {
            Assert.IsTrue(_server.Start(0));
            Assert.IsTrue(_server.IsListening);
            Assert.Greater(_server.Port, 0);
            Assert.AreEqual("127.0.0.1", _server.Address);
        }

        [Test]
        public void Start_InvalidAddress_ReturnsFalseAndLogsError()
        {
            LogAssert.Expect(LogType.Error, new Regex("监听地址必须是 IP"));

            Assert.IsFalse(_server.Start(0, "localhost"));
            Assert.IsFalse(_server.IsListening);
        }

        [Test]
        public void Start_WhenPortAlreadyTaken_ReturnsFalseAndLogsError()
        {
            Assert.IsTrue(_server.Start(0));

            var second = new TcpDebugCommandServer(_registry);

            LogAssert.Expect(LogType.Error, new Regex("TCP 监听失败"));

            Assert.IsFalse(second.Start(_server.Port), "同一端口第二次监听应当失败而不是抛异常");
            Assert.IsFalse(second.IsListening);

            second.Dispose();
        }

        [Test]
        public void Start_Twice_RestartsWithoutLosingTheService()
        {
            Assert.IsTrue(_server.Start(0));
            var firstPort = _server.Port;

            Assert.IsTrue(_server.Start(0), "重复 Start 应当停掉旧的再重开");
            Assert.IsTrue(_server.IsListening);
            Assert.Greater(_server.Port, 0);
            Assert.Greater(firstPort, 0);
        }

        [Test]
        public void Drain_WithoutRequests_ReturnsZero()
        {
            Assert.IsTrue(_server.Start(0));

            Assert.AreEqual(0, _server.Drain());
        }

        [Test]
        public void Dispose_StopsListening()
        {
            Assert.IsTrue(_server.Start(0));

            _server.Dispose();

            Assert.IsFalse(_server.IsListening);
        }

        [UnityTest]
        public IEnumerator RoundTrip_OkCommand_EchoesIdAndData()
        {
            Assert.IsTrue(_server.Start(0));
            Connect();

            var responses = NewResponses();
            yield return Send("{\"id\":7,\"cmd\":\"probe_ok\",\"args\":[\"5\"]}", responses);

            StringAssert.Contains("\"ok\":true", responses[0]);
            StringAssert.Contains("\"value\":5", responses[0]);
            StringAssert.Contains("\"id\":7", responses[0]);
        }

        [UnityTest]
        public IEnumerator RoundTrip_UnknownCommand_ReturnsError()
        {
            Assert.IsTrue(_server.Start(0));
            Connect();

            var responses = NewResponses();
            yield return Send("{\"id\":1,\"cmd\":\"no_such_cmd\"}", responses);

            StringAssert.Contains("\"ok\":false", responses[0]);
            StringAssert.Contains("未知指令", responses[0]);
        }

        [UnityTest]
        public IEnumerator RoundTrip_InvalidJson_ReturnsErrorWithHint()
        {
            Assert.IsTrue(_server.Start(0));
            Connect();

            var responses = NewResponses();
            yield return Send("{oops", responses);

            StringAssert.Contains("\"ok\":false", responses[0]);
            StringAssert.Contains("不是合法 JSON", responses[0]);
        }

        [UnityTest]
        public IEnumerator RoundTrip_ArgsNotArray_ReturnsError()
        {
            Assert.IsTrue(_server.Start(0));
            Connect();

            var responses = NewResponses();
            yield return Send("{\"id\":2,\"cmd\":\"probe_ok\",\"args\":\"5\"}", responses);

            StringAssert.Contains("\"ok\":false", responses[0]);
            StringAssert.Contains("args 必须是数组", responses[0]);
            StringAssert.Contains("\"id\":2", responses[0]);
        }

        [UnityTest]
        public IEnumerator RoundTrip_MultipleRequests_OnOneConnection()
        {
            Assert.IsTrue(_server.Start(0));
            Connect();

            var responses = NewResponses();
            yield return Send("{\"id\":1,\"cmd\":\"probe_ok\",\"args\":[\"1\"]}", responses);
            yield return Send("{\"id\":2,\"cmd\":\"probe_ok\",\"args\":[\"2\"]}", responses);
            yield return Send("{\"id\":3,\"cmd\":\"no_such_cmd\"}", responses);

            Assert.AreEqual(3, responses.Count);
            StringAssert.Contains("\"id\":1", responses[0]);
            StringAssert.Contains("\"value\":1", responses[0]);
            StringAssert.Contains("\"id\":2", responses[1]);
            StringAssert.Contains("\"value\":2", responses[1]);
            StringAssert.Contains("\"ok\":false", responses[2]);
        }

        [UnityTest]
        public IEnumerator RoundTrip_TwoClients_AreBothServed()
        {
            Assert.IsTrue(_server.Start(0));
            Connect();

            var responses = NewResponses();
            yield return Send("{\"id\":1,\"cmd\":\"probe_ok\",\"args\":[\"9\"]}", responses);

            // 第二个客户端：连接后第一条仍应被处理（多客户端安全）。
            var secondClient = new TcpClient();
            secondClient.Connect(IPAddress.Loopback, _server.Port);
            var secondStream = secondClient.GetStream();
            var secondWriter = new StreamWriter(secondStream, new UTF8Encoding(false)) { AutoFlush = true };
            var secondReader = new StreamReader(secondStream, Encoding.UTF8);

            try
            {
                var read = Task.Run(() => secondReader.ReadLine());
                secondWriter.WriteLine("{\"id\":\"second\",\"cmd\":\"probe_ok\",\"args\":[\"11\"]}");

                var deadline = Time.realtimeSinceStartup + 5f;
                while (!read.IsCompleted && Time.realtimeSinceStartup < deadline)
                {
                    _server.Drain();
                    yield return null;
                }

                Assert.IsTrue(read.IsCompleted, "第二个客户端没收到响应");
                StringAssert.Contains("\"value\":11", read.Result);
                StringAssert.Contains("\"id\":\"second\"", read.Result);
            }
            finally
            {
                secondWriter.Dispose();
                secondReader.Dispose();
                secondClient.Close();
            }
        }
    }
}
