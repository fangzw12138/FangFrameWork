# Changelog

## [0.1.0] - 2026-09-24

### Added

- `ConfigDataSoFieldDrawer`（程序集 `Fang.Framework.ConfigInspector.Editor`）：给声明类型为 `ConfigDataSo` 或其子类的引用字段加折叠箭头，展开后内联显示被引用配置资产的内容，可直接编辑。IMGUI 与 UI Toolkit 两条路径都实现，都带循环引用与最大深度（2 层）守卫。
- FangHub 页「配置展开」（`Editor/ConfigInspectorPage.cs`，`framework-config-inspector`）：说明生效范围、用法与限制。
- `Documentation~/配置展开.md`：设计理由（含「为什么不做运行期开关」）。
