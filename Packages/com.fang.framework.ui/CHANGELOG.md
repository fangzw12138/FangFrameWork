# Changelog

## [0.1.0] - 2026-09-22

### Added

- `UIService : Service, ITickable`：面板创建与复用、面板栈、层排序、跟随面板位置更新、输入阻塞事件。零反射，由 `Scope.AddService<UIService>()` 托管。
- 双轨面板基类：`UIPanelController<TConfig, TData>`（UGUI）与 `UIVisualTreePanelController<TConfig, TData>`（UI Toolkit，含 `UIDocument` 建树与 USS 挂载）。
- 跟随面板：`UIFollowPanelController<TConfig, TData>` + `UIPrefabFollowPanelController`（UGUI 屏幕坐标）/ `UIVisualTreeFollowPanelController`（`RuntimePanelUtils.ScreenToPanel` 换算）。
- 配置资产：`UILayerConfigDataSo` / `UIPanelConfigDataSo` / `UIPrefabPanelConfigDataSo` / `UIVisualTreePanelConfigDataSo` / `UIProjectConfigDataSo`，全部只读。
- 数据与句柄：`UIPanelData<TConfig>` / `IUIPanel` / `IUIFollowPanel`。
- 内部纯逻辑：`UIPanelStack` / `UILayerResolver` / `UIPanelFactory` / `UICanvasPool` / `UIVisualTreeBinder`。
- FangHub 页「UI」：面板清单浏览/定位/搜索、新增（UGUI 与 UITK 两轨）、创建预制体、删除（预览 + 确认）、校验。
- 脚手架：`UIPanelScaffolder` / `UIPanelScaffoldPaths` / `UIPanelScaffoldInfo` / `UIPanelScaffoldResult` / `UIPanelTemplateSource` / `UIPanelTrack`。
- 程序集：`Fang.Framework.UI`（全平台）、`Fang.Framework.UI.Editor`（Editor）、`Fang.Framework.UI.Tests`（EditMode 测试）、`Fang.Framework.UI.Editor.Tests`（EditMode 测试）、`Fang.Framework.UI.TestDoubles`（非 Editor 的测试替身程序集，绕开「编辑器程序集不能 AddComponent」的限制）。
- `Documentation~/UI.md`。
