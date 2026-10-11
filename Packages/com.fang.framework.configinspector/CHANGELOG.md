# Changelog

## [0.1.1] - 2026-10-11

### Fixed

- `ConfigDataSoFieldDrawer`：Unity 6.5 起 `Object.GetInstanceID()` 被弃用为错误（CS0619），导致该包整体编译失败（Cannot compile scripts）。展开链改用 64 位稳定实例 ID（`GetStableId`，Unity 6000.5+ 走 `EntityId.ToULong(obj.GetEntityId())`），6000.5 以前的 Unity 行为不变。

## [0.1.0] - 2026-09-24

### Added

- `ConfigDataSoFieldDrawer`（程序集 `Fang.Framework.ConfigInspector.Editor`）：给声明类型为 `ConfigDataSo` 或其子类的引用字段加折叠箭头，展开后内联显示被引用配置资产的内容，可直接编辑。IMGUI 与 UI Toolkit 两条路径都实现，都带循环引用与最大深度（2 层）守卫。
- FangHub 页「配置展开」（`Editor/ConfigInspectorPage.cs`，`framework-config-inspector`）：说明生效范围、用法与限制。
- `Documentation~/配置展开.md`：设计理由（含「为什么不做运行期开关」）。
