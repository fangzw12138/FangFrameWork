using System;
using Fang.Framework.CommandConsole;
using UnityEngine;

namespace Fang.Framework.CommandConsole.Editor.Tests
{
    /// <summary>
    /// 扫描器探针指令。全部放在测试程序集里，测试只扫 <c>typeof(...).Assembly</c>，
    /// 所以不受包内其它程序集（内置指令等）影响。
    /// </summary>
    internal static class DebugCommandProbes
    {
        [DebugCommand("probe_ok", "正常指令 &lt;value&gt;", "probe")]
        public static object Ok(DebugCommandContext ctx, DebugCommandArgs args)
        {
            return new { value = args.GetInt(0, "value") };
        }

        [DebugCommand("probe_null", "返回 null 的指令", "probe")]
        public static object Null(DebugCommandContext ctx, DebugCommandArgs args)
        {
            return null;
        }

        [DebugCommand("probe_business_error", "业务失败", "probe")]
        public static object BusinessError(DebugCommandContext ctx, DebugCommandArgs args)
        {
            throw new DebugCommandException("预期失败: 材料不足");
        }

        [DebugCommand("probe_runtime_error", "运行时异常", "probe")]
        public static object RuntimeError(DebugCommandContext ctx, DebugCommandArgs args)
        {
            throw new InvalidOperationException("炸了");
        }

        [DebugCommand("probe_context", "回显上下文里的注册表是否有值", "probe")]
        public static object ContextProbe(DebugCommandContext ctx, DebugCommandArgs args)
        {
            return new { hasRegistry = ctx != null && ctx.Registry != null };
        }

        [DebugCommand("probe_no_description", "", "probe")]
        public static object NoDescription(DebugCommandContext ctx, DebugCommandArgs args)
        {
            return null;
        }
    }

    /// <summary>两条同名指令：验证「重名只留一条 + 警告」。</summary>
    internal static class DebugCommandProbesDuplicate
    {
        [DebugCommand("probe_duplicate", "第一条", "probe")]
        public static object First(DebugCommandContext ctx, DebugCommandArgs args)
        {
            return "first";
        }

        [DebugCommand("probe_duplicate", "第二条", "probe")]
        public static object Second(DebugCommandContext ctx, DebugCommandArgs args)
        {
            return "second";
        }
    }

    /// <summary>非 static 类：放「签名不符」与「空名」这两类必须被跳过的探针。</summary>
    internal sealed class DebugCommandProbeHost
    {
        [DebugCommand("probe_not_static", "非 static，必须被跳过", "probe")]
        public object NotStatic(DebugCommandContext ctx, DebugCommandArgs args)
        {
            return null;
        }

        [DebugCommand("probe_wrong_params", "参数不对，必须被跳过", "probe")]
        public static object WrongParams(string first)
        {
            return first;
        }

        [DebugCommand("   ", "空名，必须被跳过", "probe")]
        public static object EmptyName(DebugCommandContext ctx, DebugCommandArgs args)
        {
            return null;
        }
    }

    /// <summary>上下文 / 服务测试用的 Scope 替身（Runtime 程序集不是 Editor 平台，这里可以直接 AddComponent）。</summary>
    internal sealed class ProbeScope : Fang.Framework.Scope
    {
        /// <summary>建一个已经 OnInit 的 Scope（OnDispose 靠 IsInitialized 守卫，不 OnInit 就什么都释放不掉）。</summary>
        public static ProbeScope Create(string name)
        {
            var host = new GameObject(name);
            var scope = host.AddComponent<ProbeScope>();
            scope.OnInit();
            return scope;
        }

        public T AddProbeService<T>() where T : Fang.Framework.Service
        {
            return AddService<T>();
        }

        public ProbeScope CreateProbeChild()
        {
            var child = CreateChildScope<ProbeScope>();
            child.OnInit();
            return child;
        }
    }

    internal sealed class ProbeAlphaService : Fang.Framework.Service
    {
    }

    internal sealed class ProbeBetaService : Fang.Framework.Service
    {
    }

    internal sealed class ProbeGammaService : Fang.Framework.Service
    {
    }

    /// <summary>继承 <see cref="DebugCommandContext"/> 的探针：验证「换上下文不用改包」。</summary>
    internal sealed class ProbeContext : DebugCommandContext
    {
        public ProbeContext(Func<Fang.Framework.Scope> scopeProvider) : base(scopeProvider)
        {
        }

        public int GetServiceCallCount { get; private set; }

        public override T GetService<T>()
        {
            GetServiceCallCount++;
            return base.GetService<T>();
        }
    }
}
