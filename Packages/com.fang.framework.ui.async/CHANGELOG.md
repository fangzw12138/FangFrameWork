# Changelog

## [0.1.0] - 2026-10-11

### Added

- **Runtime**（程序集 `Fang.Framework.UI.Async`，引用 `Fang.Framework` / `Fang.Framework.Async` / `UniTask` / `UnityEngine.UI`）：
  - 配置（5 个只读 SO）：`UILayerConfigDataSo` / `UIPanelConfigDataSo` / `UIPrefabPanelConfigDataSo` / `UIVisualTreePanelConfigDataSo` / `UIProjectConfigDataSo`，菜单在 `Create > Fang Framework > UI (Async)`。
  - 领域：`IUIPanel` / `UIPanelData<TConfig>` / `UIPanelController<TConfig, TData>`（UGUI）/ `UIVisualTreePanelController<TConfig, TData>`（UI Toolkit）。
  - 内部：`UILayerResolver` / `UICanvasPool` / `UIPanelFactory` / `UIPanelStack` / `UIVisualTreeBinder` / `CanvasScalerSettings` / `CanvasRenderSettings`。面板栈为纯逻辑，可脱离 Unity 单测。
  - `UIService : AsyncService`：面板创建与复用、面板栈、层排序、输入阻塞事件，由 `AsyncScope.AddService<UIService>()` 托管。
- **Editor**（程序集 `Fang.Framework.UI.Async.Editor`）：FangHub 页「Async UI」—— 面板清单 / 定位 / 搜索、新增（UGUI 与 UITK 两轨）、创建预制体、删除（预览 + 确认）、校验；脚手架 `UIPanelScaffolder` / `UIPanelScaffoldPaths` / `UIPanelScaffoldInfo` / `UIPanelScaffoldResult` / `UIPanelTemplateSource` / `UIPanelTrack` / `UIPanelPrefabQueue` / `UIPanelCreateOptions` / `UIProjectCreateOptions`。
  - 脚手架**绝不覆盖已有文件**：控制器 / Data 脚本、预制体若已存在则跳过并记入结果，只回填配置引用（`Documentation~/异步UI.md` §12）。
- **Tests**：`Fang.Framework.UI.Async.Tests`（EditMode）+ `Fang.Framework.UI.Async.TestDoubles`（非 Editor 的测试替身程序集）。
- `Documentation~/异步UI.md`。

### Notes

- 异步族与核心同步族**不混用**：`AddService<T>` / `GetService<T>` 的约束都是 `IAsyncLifecycle`，核心 `Service` 装不进去。
- 本包与同步 UI 包 `com.fang.framework.ui` 互不依赖，可同时安装。
