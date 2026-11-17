# Changelog

## [0.1.5] - 2026-11-17

### Changed（Breaking）

- `UIService.OpenAsync` 两个重载都必须传入 `AsyncScope ownerScope`，不再默认使用 `UIService.Scope`；传入 `null` 抛 `ArgumentNullException`。
- 新建面板在 `CreateData` / `InitAsync` 前注入 `ownerScope`；已关闭的缓存面板再次打开时重新注入新的 `ownerScope`，`OnOpenAsync` 读到的是新 scope。
- 已打开的面板再次打开时保持首次 owner scope，不重新注入；传入不同 scope 时记 `LogWarning` 并复用原面板。

### Added

- `UIService` 内部维护面板 owner scope，面板销毁清理时同步移除。

## [0.1.4] - 2026-11-16

### Added

- `IUIPanelBackHandler`：面板实现该接口即完全接管 Back 请求，由面板自己决定何时关闭或弹确认框；外部不再兜底关闭。
- `UIPanelConfigDataSo.CloseOnBack`：面板未实现 `IUIPanelBackHandler` 时，由配置决定顶层面板是否响应 Back 关闭；默认 `false`，必须显式开启。
- `UIService.TopPanel`：只读暴露当前栈顶面板，并沿用面板栈的已销毁面板清理逻辑。
- `UIService.GetPanelConfig(string)`：只读查询已注册的面板配置；未知或空 id 返回 `null`。

## [0.1.3] - 2026-11-16

### Added

- `UIService.RestoreFocus()`：重新 `Focus()` 当前栈顶、且最后一次真正获得过焦点的面板；栈空或焦点面板已不是栈顶时 no-op。供输入层在鼠标点击背景丢失 `EventSystem` 选中后、收到键盘/手柄 UI 输入时调用。
- `UIPanelStack.PanelFocused`（internal）：`PushAsync` / `RestoreTopAsync` 在调用 `panel.Focus()` 之后发出，用作焦点归属通知。

## [0.1.2] - 2026-11-16

### Fixed

- `UICanvasPool.Get` 新建 Canvas 改为 inactive-first：先创建并停用宿主，配置完 `Canvas` / `sortingOrder` / 渲染模式 / `CanvasScaler` / `GraphicRaycaster` 后再激活。修复 Unity 6000.6.5f1 下运行时新建的 Overlay Canvas 排序注册不生效、被低 `sortingOrder` 画布遮挡的问题；与 Unity 版本无关，无 `#if`。

## [0.1.1] - 2026-11-15

### Fixed

- `UIService.OpenAsync` 在面板创建后、初始化前注入所属 `AsyncScope`：面板控制器内的 `Scope` 不再为 null，`OnOpenAsync` 与 `OnCloseAsync` 中可直接 `Scope.GetService<T>()`。UGUI 与 UI Toolkit 面板均覆盖。

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
