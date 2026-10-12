# Fang Framework · Async Event Bus

`com.fang.framework.eventbus.async` —— 挂在**异步 Scope** 上、类型安全的**事件总线**。

它解决的是「**一次性领域事件**」的广播：发送方不认识接收方，消费方数量会长。

```csharp
// 发布方（改完数据、判定完）
bus.Publish(new AchievementUnlocked(id, name, medalReward, ...));

// 消费方（各自订阅，互不相识）
bus.Subscribe<AchievementUnlocked>(OnUnlocked);
```

## 关键约定

| 项 | 约定 |
| --- | --- |
| 分发判据 | 按**载荷类型** `typeof(T)` 分发：`Subscribe<DamageDealt>` 的参数类型就是 `Action<DamageDealt>`，拼错类型名**编译不过** |
| 订阅幂等 | 同一个委托（同一方法 + 同一目标实例）订两次 → **只回调一次** |
| 广播快照 | **一次 `Publish` 的接收者集合在发布开始时确定**：回调里订的从下次生效，回调里退的本次仍被调一次 |
| 线程 | **主线程限定**；`Publish` 是**同步直调** |
| 异常 | 接收者抛异常**直接冒泡**给发送方（不吞、不延迟分发） |
| 取服务 | `Scope.GetService<EventBusService>()`；App 级全局一份 = 挂 `AppScope` |

## 为什么宿主是 `AsyncService`（与 `com.fang.framework.eventbus` 的唯一差异）

同步族那份 `EventBus : Service` 挂在**同步族生命周期**上（`Scope.AddService<T>() where T : Service`），
而异步族用的是 `AsyncScope`（`AddService<T>() where T : AsyncService`）
⇒ 异步族里直接用同步族那份**编译不过**。所以本包只做一件事：把同一份 `EventBus` 装进 `AsyncService`，
"Is Service"的语义（生命周期跟着 Scope、释放时清表）由适配层保证。

差异只有这一条；其余设计取舍（幂等去重 / 快照遍历 / 主线程 / 不吞异常）逐条照抄同步族那份，
理由见其 `Documentation~/事件总线.md`。**异步族用本包，同步族用 `com.fang.framework.eventbus`，两者不混用。**

## 为什么订阅幂等去重

最常见的总线事故是**重复订阅导致重复回调**：某个服务 `OnInit` 里订一次，又在某条「刷新 / 重连」
路径上再订一次 ⇒ 同一个音效播两遍。这类 bug 表现为「偶发双份」，极难定位。
去重判据是**委托相等**，所以两个内容相同但实例不同的 lambda 仍各算一份（闭包捕获不同，本来就该各算一份）。

代价（认下）：**无法故意让同一个委托被调两次**。

## 为什么 `Publish` 遍历快照

不加快照时，订阅者在回调里 `Subscribe` / `Unsubscribe` 会直接改正在遍历的 `List<Delegate>`
⇒ `InvalidOperationException`，或**漏掉 / 重复回调**。「回调里退订自己」是常见写法，必踩。
代价：每次 `Publish` 一次数组分配 —— 事件是"一次性命令"，频率远低于每帧轮询，可以接受。

## 明确不做

- 弱类型 `Publish(object)` / 字符串 key。
- `IDisposable` 订阅句柄。
- 订阅优先级、事件过滤、一次性订阅、异步 / 延迟分发。
- 跨线程分发、事件队列、每帧批量派发。
- 静态单例 `EventBus.Instance`。
- 基类 / 接口分发（`Publish<Dog>` 不触发 `Subscribe<Animal>`）。

## 依赖

| 依赖 | 说明 |
| --- | --- |
| `com.cysharp.unitask` `2.5.11` | 来自 OpenUPM 注册表 `package.openupm.com`；写在 `package.json` 的 `dependencies` 里 |
| `com.fang.framework` / `com.fang.framework.async` | 提供 `AsyncService` / `AsyncScope`。按 git URL + tag 分发、不在任何 registry 中，故**不写**在 `dependencies` 里，靠 asmdef 的 `references` 落地 |

```text
Fang.Framework.EventBus.Async
  └─ Fang.Framework.Async   AsyncService / AsyncScope
        └─ Fang.Framework
```

## 安装

`Tools/Fang Framework/Fang Hub` → 侧栏「扩展包」→ 点「刷新」→ 点「安装」。

也可以按 git URL + tag 直接写进 `Packages/manifest.json`：

```jsonc
"com.fang.framework.eventbus.async": "https://github.com/fangzw12138/FangFrameWork.git?path=/Packages/com.fang.framework.eventbus.async#com.fang.framework.eventbus.async/v0.1.0"
```

## 状态

`0.1.0`。

## 许可证

MIT，见 `LICENSE.md`。
