# Fang Framework · Event Bus

`com.fang.framework.eventbus` —— Fang Framework 的类型安全发布订阅扩展包。

零第三方依赖，零反射。`EventBus` 由 `Scope` 托管，生命周期跟着 Scope 走。

## 依赖

本包**引用核心包** `com.fang.framework`（asmdef 引用 `Fang.Framework`）。

`package.json` 的 `dependencies` 是空对象 —— UPM 不支持包内声明 git URL 依赖，因此依赖关系靠 asmdef 引用 + FangHub 扩展包页的前置检查 + 本文档声明。安装本包前请先确保核心包已装。

## 安装

**方式一：FangHub 的扩展包页**（推荐）

`Tools/Fang Framework/Fang Hub` → 侧栏点「扩展包」→ 点「刷新」→ 点「安装」。

**方式二：手写 `Packages/manifest.json`**

```json
{
  "dependencies": {
    "com.fang.framework": "https://github.com/fangzw12138/FangFrameWork.git?path=/Packages/com.fang.framework#com.fang.framework/v0.5.0",
    "com.fang.framework.eventbus": "https://github.com/fangzw12138/FangFrameWork.git?path=/Packages/com.fang.framework.eventbus#com.fang.framework.eventbus/v0.1.0"
  }
}
```

需要本机有 git CLI。

## 用法

```csharp
using Fang.Framework;
using Fang.Framework.EventBus;

public sealed class GameScope : Scope
{
    public EventBus Events { get; private set; }

    public override void OnInit()
    {
        base.OnInit();

        Events = AddService<EventBus>();
    }
}
```

```csharp
public sealed class AudioService : Service
{
    private EventBus _events;

    public override void OnInit()
    {
        _events = Scope.GetService<EventBus>();
        _events.Subscribe<DamageDealt>(OnDamageDealt);
    }

    public override void OnDispose()
    {
        _events.Unsubscribe<DamageDealt>(OnDamageDealt);
    }

    private void OnDamageDealt(DamageDealt payload)
    {
    }
}

public readonly struct DamageDealt
{
    public DamageDealt(int amount)
    {
        Amount = amount;
    }

    public int Amount { get; }
}
```

```csharp
events.Publish(new DamageDealt(10));
```

## 编辑器

本包带一个 FangHub 页（`Editor/EventBusPage.cs`，程序集 `Fang.Framework.EventBus.Editor`）：`Tools/Fang Framework/Fang Hub` → 侧栏「事件总线」。

- 列出运行中每个 `EventBus` 的物体路径，以及它的事件类型、订阅者数量与处理函数（`目标类型.方法名`），用来回答「谁订了 `DamageDealt`」。
- 订阅表是运行时状态，**进入 Play 模式后点「刷新」**才有内容；编辑模式下只显示提示。
- 页面只读：不订阅、不退订、不发事件。订阅表走编辑器侧反射读取 —— 不为了一个只读观察器给运行时开公开 API。
- 挂 FangHub 页的完整写法（asmdef 引用、属性字段、两种渲染接口、生命周期）见核心包 `Documentation~/FangHub.md`。

## API

| 成员 | 说明 |
| --- | --- |
| `Subscribe<T>(Action<T>)` | 订阅。同一委托重复订阅只算一次（幂等）；`null` 抛 `ArgumentNullException` |
| `Unsubscribe<T>(Action<T>)` | 退订。未订阅过时静默返回；`null` 静默返回 |
| `Publish<T>(T)` | 按订阅顺序同步回调全部订阅者；无订阅者时静默返回 |
| `OnDispose()` | 清空全部订阅（由 `Scope` 驱动） |

- 按 `typeof(T)` 精确匹配，**不做基类 / 接口分发**：订阅 `Action<Animal>` 收不到 `Publish<Dog>`。
- 主线程限定，与核心包一致。
- 不做：弱类型 `Publish(object)`、订阅句柄、优先级、事件过滤、异步分发。

设计理由见 `Documentation~/事件总线.md`。

## 许可证

MIT，见 `LICENSE.md`。
