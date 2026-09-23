# Changelog

## [0.1.0] - 2026-09-23

### Added

- 包骨架：`package.json`（`dependencies` 为空对象）、`README.md` / `CHANGELOG.md` / `LICENSE.md`。
- 程序集：`Fang.Framework.UI.Kit`（全平台，`autoReferenced: true`，引用 `Fang.Framework` / `Fang.Framework.UI` / `DOTween` / `Unity.TextMeshPro`）、`Fang.Framework.UI.Kit.Editor`（Editor，`autoReferenced: false`）。
- 内容资产目录约定：`Prefabs/` `Sprites/` `Materials/` `Shaders/` `Particles/` `Textures/`。
- 文档：`Documentation~/控件库.md`（机制与边界）、`Documentation~/索引规范.md`（索引 id 表）。
- 索引：`TokenIds`（16 条已发布 id 的字符串常量表：`Text/*` 5 条、`Color/*` 9 条、`Icon/*` 2 条）+ `TokenKind`。
- 匹配组件：`TokenMatch`（通用组件，条目列表 = `id` + `target`）与 `TokenMatchEntry`；`ApplyFrom(UIKitProjectSo)` 逐条按 id 查 token 并写值。
- token：`TokenSo` 基类（`Kind` / `TargetType` / `Apply(Component)` / `Accepts`）+ 两类实现 `FontTokenSo`（字体 / 字号 / 样式 / 行距，逐项可开关）、`ColorTokenSo`（颜色）。
- 项目 SO：`UIKitProjectSo`（prefab 列表 + token 表 + `GetToken(id)`）；token 资产自己的 `Id` 就是索引 id，表里不再写一遍。
- 校验：`TokenMatchValidator`（内部纯逻辑）——空条目 / 没填 id / id 不在索引规范 / 没指定 target / 缺 token / token 类型与组不符 / target 类型不符 / token 表重复 id。
- 编辑器：`UIKitNewProjectWizard`（引导窗口：每项一句说明、固定底栏、内容可滚动）、`UIKitProjectScaffolder` + `UIKitScaffoldPaths` + `UIKitProjectCreateOptions` / `UIKitProjectScaffoldResult`、`UIKitValidationWindow`（校验结果弹窗）。
- 测试：`Tests/Editor`（`Fang.Framework.UI.Kit.Editor.Tests`），66 项 EditMode 测试全绿。
