using System;
using System.Collections.Generic;

namespace Fang.Framework.EventBus.Async
{
    /// <summary>
    /// **类型安全的事件总线**（纯 C#，好测、不依赖引擎）。
    ///
    /// 设计照抄 fang 框架那份（<c>com.fang.framework.eventbus</c> 的《事件总线.md》），
    /// 只有一处差异：宿主不是核心包的 <c>Service</c>，而是本工程的异步族
    /// <see cref="EventBusService"/>（<c>AsyncService</c>）—— 因为本工程 <c>Scope</c> 是 <c>AsyncScope</c>。
    /// 其余取舍（幂等去重 / 快照遍历 / 主线程同步直调 / 不吞异常 / 明确不做）逐条保留。
    ///
    /// 为什么类型安全而不是 <c>string</c> + <c>object</c>：字符串 key 拼错编译器不查、
    /// 载荷要运行时强制转换、key 与类型之间没有约束 —— 三条都是编译期就能避免的。
    /// 代价（认下）：**没有基类 / 接口分发** —— <c>Publish&lt;Dog&gt;</c> 不会触发 <c>Subscribe&lt;Animal&gt;</c>。
    /// 需要分组广播时，领域侧自己定义一个装字段的结构体即可。
    /// </summary>
    public sealed class EventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers = new Dictionary<Type, List<Delegate>>();

        /// <summary>订阅一个事件类型。</summary>
        /// <param name="handler">处理函数；<c>null</c> ⇒ <see cref="ArgumentNullException"/>。</param>
        /// <remarks>
        /// **幂等去重**：同一个委托（同一方法 + 同一目标实例）订两次只回调一次。
        /// 去重挡的是最常见的事故 —— 某个服务在 <c>OnInit</c> 与某条"刷新"路径上各订一次，
        /// 于是同一个音效播两遍（表现为"偶发双份"，极难定位）。
        /// 代价：无法故意让同一个委托被调两次；真要那样，包一层 lambda。
        /// </remarks>
        public void Subscribe<T>(Action<T> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            var type = typeof(T);

            if (!_handlers.TryGetValue(type, out var list))
            {
                list = new List<Delegate>();
                _handlers.Add(type, list);
            }

            if (!list.Contains(handler))
            {
                list.Add(handler);
            }
        }

        /// <summary>退订（容错：没订过 / <c>null</c> 都直接返回；该类型空表则移除整条，避免长跑攒垃圾）。</summary>
        public void Unsubscribe<T>(Action<T> handler)
        {
            if (handler == null)
            {
                return;
            }

            if (!_handlers.TryGetValue(typeof(T), out var list))
            {
                return;
            }

            list.Remove(handler);

            if (list.Count == 0)
            {
                _handlers.Remove(typeof(T));
            }
        }

        /// <summary>
        /// 广播一条事件。无订阅者 = 直接返回。
        /// </summary>
        /// <remarks>
        /// ★ **遍历快照**：不这么做时，订阅者在回调里 <c>Subscribe</c>/<c>Unsubscribe</c> 会改到正在遍历的
        /// <c>List</c>，结果是抛异常或漏 / 重复回调 —— 而"回调里退订自己"是常见写法。
        /// 快照的语义：**一次 <c>Publish</c> 的接收者集合在发布开始时确定**（回调中订的从下次生效，
        /// 回调中退的本次仍会被调一次）。代价是每次一次数组分配：事件是"一次性命令"，频率远低于每帧轮询。
        ///
        /// 接收者抛异常会**直接冒泡**给发送方（不吞、不延迟分发）—— 让"谁出的错"可见。
        /// </remarks>
        public void Publish<T>(T payload)
        {
            if (!_handlers.TryGetValue(typeof(T), out var list))
            {
                return;
            }

            var snapshot = list.ToArray();

            for (var i = 0; i < snapshot.Length; i++)
            {
                ((Action<T>)snapshot[i]).Invoke(payload);
            }
        }

        /// <summary>某个事件类型现在有几个订阅者（调试读数用）。</summary>
        public int CountOf<T>()
        {
            return _handlers.TryGetValue(typeof(T), out var list) ? list.Count : 0;
        }

        /// <summary>全部类型合计的订阅数（调试读数用）。</summary>
        public int SubscriberCount
        {
            get
            {
                var total = 0;

                foreach (var pair in _handlers)
                {
                    total += pair.Value.Count;
                }

                return total;
            }
        }

        /// <summary>各事件类型的订阅数（调试指令 <c>eventbus_state</c> 用；按类型名排序，便于复读比对）。</summary>
        public List<KeyValuePair<Type, int>> DescribeSubscribers()
        {
            var rows = new List<KeyValuePair<Type, int>>(_handlers.Count);

            foreach (var pair in _handlers)
            {
                rows.Add(new KeyValuePair<Type, int>(pair.Key, pair.Value.Count));
            }

            rows.Sort((left, right) => string.CompareOrdinal(left.Key.FullName, right.Key.FullName));
            return rows;
        }

        /// <summary>清空订阅表（<see cref="EventBusService"/> 释放时调；业务侧一般不需要）。</summary>
        public void Clear()
        {
            _handlers.Clear();
        }
    }
}
