using System.Collections.Generic;
using System.Text.RegularExpressions;
using Fang.Framework;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Fang.Framework.CommandConsole.Editor.Tests
{
    /// <summary>
    /// <see cref="DebugCommandContext"/>：沿真实 Scope 解析链找服务、找不到返回 null、可继承覆写。
    /// </summary>
    public class DebugCommandContextTests
    {
        private readonly List<GameObject> _hosts = new List<GameObject>();

        private ProbeScope _parent;
        private ProbeScope _child;

        [SetUp]
        public void SetUp()
        {
            _parent = ProbeScope.Create("DebugContextRoot");
            _hosts.Add(_parent.gameObject);
            _child = _parent.CreateProbeChild();
        }

        [TearDown]
        public void TearDown()
        {
            _parent?.OnDispose();

            for (var i = _hosts.Count - 1; i >= 0; i--)
            {
                if (_hosts[i] != null)
                {
                    Object.DestroyImmediate(_hosts[i]);
                }
            }

            _hosts.Clear();
            _parent = null;
            _child = null;
        }

        [Test]
        public void GetService_FindsServiceInSameScope()
        {
            _child.AddProbeService<ProbeBetaService>();

            var context = new DebugCommandContext(() => _child);

            Assert.IsNotNull(context.GetService<ProbeBetaService>());
        }

        [Test]
        public void GetService_WalksUpToParentScope()
        {
            _parent.AddProbeService<ProbeAlphaService>();
            _child.AddProbeService<ProbeBetaService>();

            var context = new DebugCommandContext(() => _child);

            Assert.IsNotNull(context.GetService<ProbeAlphaService>(), "父 Scope 上的服务应能被解析到");
            Assert.IsNotNull(context.GetService<ProbeBetaService>());
        }

        [Test]
        public void GetService_Missing_ReturnsNullInsteadOfThrowing()
        {
            _parent.AddProbeService<ProbeAlphaService>();

            var context = new DebugCommandContext(() => _child);

            Assert.IsNull(context.GetService<ProbeGammaService>());
        }

        [Test]
        public void GetService_FallsBackToChildScope()
        {
            // 服务在子 Scope 里：向上找不到，向下兜底要能摸到。
            _child.AddProbeService<ProbeBetaService>();

            var context = new DebugCommandContext(() => _parent);

            Assert.IsNotNull(context.GetService<ProbeBetaService>(), "向上没有时应向下兜底命中子 Scope");
        }

        [Test]
        public void GetService_PrefersSelfAndParentsOverChildren()
        {
            // 同类型在自身与子树里都有：向上先命中，行为与早期版本一致。
            var parentService = _parent.AddProbeService<ProbeBetaService>();
            _child.AddProbeService<ProbeBetaService>();

            var context = new DebugCommandContext(() => _parent);

            Assert.AreSame(parentService, context.GetService<ProbeBetaService>(), "向上命中的那个优先");
        }

        [Test]
        public void GetService_MultipleMatchesInChildren_TakesFirstAndWarns()
        {
            var first = _child.AddProbeService<ProbeBetaService>();
            var second = _parent.CreateProbeChild();
            var secondService = second.AddProbeService<ProbeBetaService>();

            LogAssert.Expect(
                LogType.Warning,
                new Regex("向下兜底.*找到多个.*ProbeBetaService"));

            var context = new DebugCommandContext(() => _parent);

            Assert.AreSame(first, context.GetService<ProbeBetaService>(), "多命中取第一个（广度优先顺序）");
            Assert.AreNotSame(secondService, first);
        }

        [Test]
        public void TryGetService_ReportsPresence()
        {
            _parent.AddProbeService<ProbeAlphaService>();

            var context = new DebugCommandContext(() => _child);

            Assert.IsTrue(context.TryGetService<ProbeAlphaService>(out var found));
            Assert.IsNotNull(found);

            Assert.IsFalse(context.TryGetService<ProbeGammaService>(out var missing));
            Assert.IsNull(missing);
        }

        [Test]
        public void CurrentScope_ReturnsProvidedScope()
        {
            var context = new DebugCommandContext(() => _child);

            Assert.AreSame(_child, context.CurrentScope);
        }

        [Test]
        public void NullScopeProvider_ReturnsNullAndDoesNotThrow()
        {
            var context = new DebugCommandContext(null);

            Assert.IsNull(context.CurrentScope);
            Assert.IsNull(context.GetService<ProbeAlphaService>());
            Assert.IsFalse(context.TryGetService<ProbeAlphaService>(out _));
        }

        [Test]
        public void DerivedContext_OverrideIsUsed()
        {
            _parent.AddProbeService<ProbeAlphaService>();

            var context = new ProbeContext(() => _child);

            Assert.IsNotNull(context.GetService<ProbeAlphaService>());
            Assert.AreEqual(1, context.GetServiceCallCount, "子类覆写的 GetService 应被调用");
        }

        [Test]
        public void Registry_IsNullWhenOnlyScopeProviderGiven()
        {
            var context = new DebugCommandContext(() => _child);

            Assert.IsNull(context.Registry);
        }
    }
}
