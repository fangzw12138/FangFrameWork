#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using Fang.Framework.Async;
using UnityEngine;

namespace Fang.Framework.CommandConsole.Async.Tests
{
    /// <summary>用于验证「按接口取服务」。</summary>
    internal interface ITestService
    {
        string Id { get; }
    }

    /// <summary>
    /// 测试用异步 Scope：暴露受保护的 <c>AddService</c> / <c>CreateChildScope</c>，
    /// 并集中回收创建的 GameObject。
    /// </summary>
    internal sealed class TestScope : AsyncScope
    {
        private static readonly List<GameObject> Hosts = new List<GameObject>();

        public static TestScope Create()
        {
            var host = new GameObject(nameof(TestScope));
            Hosts.Add(host);

            return host.AddComponent<TestScope>();
        }

        public static void DestroyAll()
        {
            for (var i = Hosts.Count - 1; i >= 0; i--)
            {
                if (Hosts[i] != null)
                {
                    Object.DestroyImmediate(Hosts[i]);
                }
            }

            Hosts.Clear();
        }

        /// <summary>装一个服务（生命周期同步完成，不需要 PlayerLoop）。</summary>
        public TService Add<TService>() where TService : MonoBehaviour, IAsyncLifecycle
        {
            return AddService<TService>();
        }

        /// <summary>建一个子 Scope。</summary>
        public TChild CreateChild<TChild>() where TChild : AsyncScope
        {
            return CreateChildScope<TChild>();
        }
    }

    /// <summary>什么都不做的服务替身（<c>OnInitAsync</c> 用基类默认实现，同步完成）。</summary>
    internal sealed class TestService : AsyncService, ITestService
    {
        public string Id => nameof(TestService);
    }

    /// <summary>另一个服务类型，用来验证「取不到就返回 null」。</summary>
    internal sealed class TestOtherService : AsyncService
    {
    }
}
#endif
