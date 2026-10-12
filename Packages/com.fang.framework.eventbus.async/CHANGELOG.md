# Changelog

## [0.1.0] - 2026-10-12

### Added

- **Runtime**（程序集 `Fang.Framework.EventBus.Async`，引用 `Fang.Framework` / `Fang.Framework.Async` / `UniTask`）：
  - `EventBus`：纯 C# 类型安全总线 —— `Subscribe<T>`（幂等去重）/ `Unsubscribe<T>`（容错）/ `Publish<T>`（遍历快照）/ `CountOf<T>` / `SubscriberCount` / `DescribeSubscribers()` / `Clear()`。
  - `EventBusService`：`AsyncService` 宿主（能被 `AsyncScope.AddService<T>()` 收）；暴露 `Bus` + `Subscribe` / `Unsubscribe` / `Publish` 语法糖 + 只读读数；`OnDisposeAsync` 清表。
- `README.md` / `CHANGELOG.md` / `LICENSE.md`。

### Notes

- 与**同步族**的 `com.fang.framework.eventbus`（`EventBus : Service`）只差宿主基类：本包是 `AsyncService`，因为异步族用的是 `AsyncScope`（`AddService<T>() where T : AsyncService`）。两者不混用。

