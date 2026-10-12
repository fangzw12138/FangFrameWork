#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using Fang.Framework.Async;
using UnityEngine;

namespace Fang.Framework.CommandConsole.Async
{
    /// <summary>
    /// 指令上下文：给出注册表，并在**异步 Scope 树**上查找服务。
    /// 查找顺序是先沿 <see cref="AsyncScope.Parent"/> 向上，再以起点为根 BFS 向下兜底；
    /// 全程只读 <c>Services</c>/<c>Parent</c>/<c>Children</c>，零反射。
    /// </summary>
    public sealed class DebugCommandContext
    {
        private readonly DebugCommandRegistry _registry;
        private readonly Func<AsyncScope> _scopeProvider;

        internal DebugCommandContext(DebugCommandRegistry registry, Func<AsyncScope> scopeProvider)
        {
            _registry = registry;
            _scopeProvider = scopeProvider;
        }

        /// <summary>本次执行使用的注册表。</summary>
        public DebugCommandRegistry Registry => _registry;

        /// <summary>查找起点（通常是装指令服务的那个 Scope）；没有可用 Scope 时为 null。</summary>
        public AsyncScope CurrentScope => _scopeProvider?.Invoke();

        /// <summary>沿异步 Scope 树查找服务；找不到返回 null（不抛）。</summary>
        public T GetService<T>() where T : class
        {
            TryGetService<T>(out var service);
            return service;
        }

        /// <summary>
        /// 沿异步 Scope 树查找服务。
        /// 先向上逐层（每层按 <c>Services</c> 顺序匹配）；仍没有则从起点 BFS 向下，
        /// 命中第一个作为结果；若又遇到第二个命中，只打**一条**警告后立即返回。
        /// </summary>
        public bool TryGetService<T>(out T service) where T : class
        {
            service = null;

            var scope = CurrentScope;

            if (scope == null)
            {
                return false;
            }

            if (TryFindUpward(scope, out service))
            {
                return true;
            }

            return TryFindDownward(scope, out service);
        }

        private static bool TryFindUpward<T>(AsyncScope scope, out T service) where T : class
        {
            for (var current = scope; current != null; current = current.Parent)
            {
                if (TryMatch(current, out service))
                {
                    return true;
                }
            }

            service = null;
            return false;
        }

        private static bool TryFindDownward<T>(AsyncScope origin, out T service) where T : class
        {
            service = null;

            AsyncScope firstScope = null;
            var queue = new Queue<AsyncScope>();
            queue.Enqueue(origin);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (TryMatch(current, out T match))
                {
                    if (firstScope == null)
                    {
                        service = match;
                        firstScope = current;
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"{DebugCommandRegistry.LogTag} 服务 '{typeof(T).Name}' 在作用域树里有多个实例：起点 '{origin.Name}'，" +
                            $"命中 '{firstScope.Name}' 与 '{current.Name}'，已取第一个。");

                        // 只打一条警告，立即返回第一个命中。
                        return true;
                    }
                }

                var children = current.Children;

                for (var i = 0; i < children.Count; i++)
                {
                    queue.Enqueue(children[i]);
                }
            }

            return firstScope != null;
        }

        private static bool TryMatch<T>(AsyncScope scope, out T service) where T : class
        {
            var services = scope.Services;

            for (var i = 0; i < services.Count; i++)
            {
                if (services[i] is T match)
                {
                    service = match;
                    return true;
                }
            }

            service = null;
            return false;
        }
    }
}
#endif
