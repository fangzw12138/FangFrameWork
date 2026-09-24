#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using Fang.Framework;

namespace Fang.Framework.CommandConsole
{
    /// <summary>
    /// 指令执行上下文：指令内部经此定位游戏服务。
    ///
    /// 查找方向是<b>两段</b>：先沿核心包 <see cref="Scope"/> 的解析链（自身 → <see cref="Scope.Parent"/> …）
    /// <b>向上</b>找；向上找不到，再从当前 Scope 起<b>广度优先向下兜底</b>（自身 → <see cref="Scope.Children"/> …）。
    /// 所以「指令服务装在浅层（例如系统层）、它要用的服务在更深层（例如场景层）」也能摸到 ——
    /// 这正是把指令服务装在系统层时的常见形态。
    ///
    /// 向下兜底只在<b>当前 Scope 的子树</b>里找；场景里另外独立摆放的根 Scope 不在树里，走不到。
    /// 同一类型在子树里命中多个时取<b>第一个</b>并打一条 <c>LogWarning</c>（层级顺序决定拿哪个，静默取第一个会误导）。
    ///
    /// <b>找不到返回 null、不抛异常</b> —— 核心包 <c>Scope.GetService&lt;T&gt;()</c> 找不到会抛，
    /// 而「还没进游戏就该返回 error」是指令的常见场景，所以这里自己走链（只用 <see cref="Scope.Services"/> /
    /// <see cref="Scope.Parent"/> / <see cref="Scope.Children"/> 三个公开成员，零反射）。
    ///
    /// 想要业务快捷属性（例如 <c>CurrentBattle</c>）就继承本类，并把
    /// <see cref="DebugCommandRegistry.ContextFactory"/> 换成自己的工厂 —— 不用改包。
    /// </summary>
    public class DebugCommandContext
    {
        private readonly Func<Scope> _scopeProvider;

        public DebugCommandContext(DebugCommandRegistry registry, Func<Scope> scopeProvider)
        {
            Registry = registry;
            _scopeProvider = scopeProvider;
        }

        /// <summary>只要上下文、不要注册表时用这个（<see cref="Registry"/> 为 null）。</summary>
        public DebugCommandContext(Func<Scope> scopeProvider)
            : this(null, scopeProvider)
        {
        }

        /// <summary>发起本次执行的注册表（<c>list_commands</c> / <c>command_log</c> 这类元指令要用）。</summary>
        public DebugCommandRegistry Registry { get; }

        /// <summary>当前 Scope（提供者没给就返回 null）。每次访问都会重新问一次提供者。</summary>
        public Scope CurrentScope => _scopeProvider?.Invoke();

        /// <summary>先沿 Scope 链向上、再向下兜底找服务；找不到返回 null（不抛）。</summary>
        public virtual T GetService<T>() where T : Service
        {
            TryGetService(out T service);
            return service;
        }

        /// <summary>先沿 Scope 链向上、再向下兜底找服务；找不到返回 false。</summary>
        public bool TryGetService<T>(out T service) where T : Service
        {
            service = null;

            var scope = CurrentScope;
            if (scope == null)
            {
                return false;
            }

            // 1) 沿 Parent 向上（自身 → 父 → …）：主路径。
            for (var current = scope; current != null; current = current.Parent)
            {
                if (TryFindInScope(current, out service))
                {
                    return true;
                }
            }

            // 2) 向上没有，再从自身起广度优先向下兜底。
            return TryFindInSubtree(scope, out service);
        }

        private static bool TryFindInScope<T>(Scope scope, out T service) where T : Service
        {
            service = null;

            var services = scope.Services;
            for (var i = 0; i < services.Count; i++)
            {
                if (services[i] is T match)
                {
                    service = match;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 从 <paramref name="root"/> 起广度优先（自身 → Children → …）找服务。
        /// 命中多个时取第一个并打一条警告 —— 同一类型有多个实例时，指令拿到哪个由层级顺序决定，
        /// 静默取第一个会让人误以为拿到的是自己想的那个。
        /// </summary>
        private static bool TryFindInSubtree<T>(Scope root, out T service) where T : Service
        {
            service = null;

            var queue = new Queue<Scope>();
            queue.Enqueue(root);

            string firstScopeName = null;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (TryFindInScope(current, out T match))
                {
                    if (service == null)
                    {
                        service = match;
                        firstScopeName = current.Name;
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning(
                            "[DebugCommand] 向下兜底在 Scope '" + root.Name + "' 的子树里找到多个 "
                            + typeof(T).Name + "（首个在 '" + firstScopeName + "'，又见 '" + current.Name
                            + "'），取第一个。");
                        return true;
                    }
                }

                var children = current.Children;
                for (var i = 0; i < children.Count; i++)
                {
                    queue.Enqueue(children[i]);
                }
            }

            return service != null;
        }
    }
}
#endif
