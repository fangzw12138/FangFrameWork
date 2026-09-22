# Changelog

## [Unreleased]

### Added

- **FangHub 页「事件总线」**：`Editor/EventBusPage.cs` + 程序集 `Fang.Framework.EventBus.Editor`（asmdef 引用 `Fang.Framework` / `Fang.Framework.Editor` / `Fang.Framework.EventBus`，`includePlatforms: ["Editor"]`）。列出运行中每个 `EventBus` 的物体路径、事件类型、订阅者数量与处理函数；只读，订阅表走编辑器侧反射读取。
- README 补「编辑器」章节；安装方式改为 FangHub 的扩展包页。

## [0.1.0] - 2026-09-21

### Added

- `EventBus : Service`：类型安全发布订阅。
- `Subscribe<T>(Action<T>)` / `Unsubscribe<T>(Action<T>)` / `Publish<T>(T)` / `OnDispose()`。
- 程序集 `Fang.Framework.EventBus`（引用 `Fang.Framework`），EditMode 测试程序集 `Fang.Framework.EventBus.Tests`。
- `Documentation~/事件总线.md`。
