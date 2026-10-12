using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Fang.Framework.Async;

namespace Fang.Framework.EventBus.Async
{
    /// <summary>
    /// 事件总线的**宿主**（<c>AsyncService</c> ⇒ 能被 <c>AsyncScope.AddService&lt;T&gt;()</c> 收）。
    ///
    /// 为什么要有它（而不是让业务直接 <c>new EventBus()</c>）：
    /// <list type="bullet">
    /// <item><b>生命周期跟着 Scope 走</b>：<c>AppScope</c> 建它、拆它时 <see cref="OnDisposeAsync"/>
    /// 把订阅表清空 ⇒ 业务侧不需要记得退订。</item>
    /// <item><b>作用域可控</b>：全局一份挂 <c>AppScope</c>；将来要一局一份就挂 <c>GameScope</c>。</item>
    /// <item><b>取用与其它能力一致</b>：<c>Scope.GetService&lt;EventBusService&gt;()</c>，零新机制。</item>
    /// </list>
    ///
    /// ★ 与 fang 那份的唯一差异：宿主是 <c>AsyncService</c> 而不是核心包的 <c>Service</c>
    /// （本工程 <c>Scope</c> 是 <c>AsyncScope</c>，核心包那份 <c>AddService&lt;EventBus&gt;()</c> 编译不过）。
    /// "Is Service"的语义由本适配层保证：**释放时清表**。
    /// </summary>
    public sealed class EventBusService : AsyncService
    {
        // ★ 纯 C# 实例**先建好**（不在 OnInitAsync 里建）：OnInitAsync 之前就可用，避开时序坑。
        private readonly EventBus _bus = new EventBus();

        /// <summary>底下的纯 C# 总线（要 <c>CountOf</c> / <c>Clear</c> 这类读数时用它）。</summary>
        public EventBus Bus => _bus;

        /// <summary>语法糖：业务侧只认 Service，不必先取 <see cref="Bus"/>。</summary>
        public void Subscribe<T>(Action<T> handler) => _bus.Subscribe(handler);

        /// <summary>语法糖（见 <see cref="Subscribe{T}"/>）。</summary>
        public void Unsubscribe<T>(Action<T> handler) => _bus.Unsubscribe(handler);

        /// <summary>语法糖（见 <see cref="Subscribe{T}"/>）。</summary>
        public void Publish<T>(T payload) => _bus.Publish(payload);

        /// <summary>某事件类型现有订阅数（调试读数用）。</summary>
        public int CountOf<T>() => _bus.CountOf<T>();

        /// <summary>全部类型合计的订阅数（调试读数用）。</summary>
        public int SubscriberCount => _bus.SubscriberCount;

        /// <summary>各事件类型的订阅数（调试指令 <c>eventbus_state</c> 用）。</summary>
        public List<KeyValuePair<Type, int>> DescribeSubscribers() => _bus.DescribeSubscribers();

        /// <summary>订阅表随 Scope 释放自动清空（AppScope 常驻 ⇒ 跨场景一份）。</summary>
        protected override UniTask OnDisposeAsync()
        {
            _bus.Clear();
            return UniTask.CompletedTask;
        }
    }
}
