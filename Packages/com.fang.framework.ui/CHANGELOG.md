# Changelog

## [0.1.3] - 2026-10-11

### Fixed

- **脚手架会静默覆盖手写文件**：`UIPanelScaffolder` 写 UXML / USS / Data / 控制器脚本改为先判存在（`TryWriteText`），已存在就跳过并把路径记入 `UIPanelScaffoldResult`；`CreatePrefab` 遇到已存在的预制体不再覆盖，只把引用回填到配置。原先那道「面板配置已存在」的守卫挡不住**配置资产被删、手写脚本还在**的路径，此时 `File.WriteAllText` 会把手写控制器静默冲掉。
- `CreatePrefab` 回填后不再无条件报成功：落盘检查失败会给出「配置回填未落盘，稍后重试」，由 `UIPanelPrefabQueue` 继续重试（最多 20 次）。

### Changed

- 包版本 `0.1.2` → `0.1.3`。

## [0.1.2] - 2026-10-03

### Fixed

- FangHub 页停靠 / 拖出成独立窗口时的 NRE：释放 `SerializedObject` 前先摘掉 Inspector 元素，重新挂载后在新 panel 上重建。

### Changed

- 包版本 `0.1.1` → `0.1.2`。

## [0.1.1] - 2026-09-30

### Fixed

- **2022.3 / 团结引擎 1.6 上编译不过**：UI 页搜索框的 `searchField.textEdition.placeholder` 是 Unity 6 才有的 UI Toolkit 成员（2022.3 的 `ITextEdition` 没有 `placeholder`），加 `#if UNITY_6000_0_OR_NEWER` 守卫。2022.3 上搜索框没有占位提示，其余行为不变。

### Changed

- 包版本 `0.1.0` → `0.1.1`。

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
