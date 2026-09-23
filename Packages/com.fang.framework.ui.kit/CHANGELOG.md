# Changelog

## [0.2.0] - 2026-09-23

### Added

- **控件**：`KitButton`（按钮的结构契约：底图 / 按钮本体 / 图标 / 文字**只读**，两个动效槽位**可写**）+ 包内 6 个控件预制体：`ButtonIconText` / `ButtonIcon` / `ButtonText` / `TextTitle` / `TextBody` / `TextCaption`。每个相关节点自带 `TokenMatch`。
- **组合 token**：一个 token 管一整个控件的多个属性，靠 `KitButton` 上的引用找到子元素。
  - `TextTokenSo`（字体 / 字号 / 样式 / 行距 / 颜色，**逐项开关**）
  - `IconTokenSo`（Sprite + 颜色）
  - `ButtonTokenSo`（底图 Sprite + 5 态 `ColorBlock` + **嵌套**文字 token / 图标 token + 两个动效槽位）
- **动效**：`UiMotionSo`（`AnimationClip` + 循环）。**不是 token**——不参与匹配、不进 token 库、不出现在新建 token 的类型下拉里。
- **占位图**：`Sprites/ButtonBackground.png`（9 宫格圆角底，border 20）、`Sprites/IconDot.png`、`Sprites/IconDiamond.png`。
- **示例**（`Samples~/Demo`）：`Demo.unity`（3 按钮 + 3 文本）、`UIKitDemoProject.asset`、6 个 Prefab Variant、10 条 token、`README.md`。导入方式：`Package Manager → UI Kit → Samples → Import`。
- **文档**：`Documentation~/动效.md`、`Documentation~/定制与升级.md`。
- **测试**：新增 `IconTokenSoTests`、`ButtonTokenSoTests`、`PackageContentTests`（包内预制体的条目 id / 槽位接线 / 示例 token 覆盖 / 示例 GUID 无断链）；`TextTokenSoTests` 扩充组合语义。ui.kit 的 EditMode 测试从 66 项涨到 **104 项，全绿**。

### Changed

- `package.json`：`version` → `0.2.0`；新增 `samples` 条目（`Samples~/Demo`）。
- 写值语义统一为「**空 = 不管**」：引用类字段（`TMP_FontAsset` / `Sprite` / `TokenSo` / `UiMotionSo`）为空时 `Apply` **跳过**，不把 target 写成 null。否则「全体应用」会把用户手配的动效清掉。值类字段（`Color` / `float` / `FontStyles` / `ColorBlock`）用各自的 `bool` 开关表示「不写」。
- `TokenMatchValidator` 的「没填 MatchId」提醒新增第二种含义：组合 token **嵌套**的子 token 也留空 MatchId（「只被引用，不参与匹配」），与「还没启用」区分开，见 `Documentation~/控件库.md`。
- 动效播放机制结论（实测）写进 `Documentation~/动效.md`。

### Fixed

修正 0.1.0 条目与代码不符的三处：

- 不存在 `TokenIds` 常量表与 `TokenKind`——匹配 id 是**手填字符串**，没有枚举、没有常量表。
- token 实现是 5 个**原子**类（`FontAssetTokenSo` / `FontSizeTokenSo` / `FontStyleTokenSo` / `LineSpacingTokenSo` / `ColorTokenSo`），不是一个 `FontTokenSo` 带四个开关。
- 文档是 `Documentation~/匹配规范.md`，不是 `索引规范.md`。

## [0.1.0] - 2026-09-23

### Added

- 包骨架：`package.json`（`dependencies` 为空对象）、`README.md` / `CHANGELOG.md` / `LICENSE.md`。
- 程序集：`Fang.Framework.UI.Kit`（全平台，`autoReferenced: true`，引用 `Fang.Framework` / `Fang.Framework.UI` / `DOTween` / `Unity.TextMeshPro`）、`Fang.Framework.UI.Kit.Editor`（Editor，`autoReferenced: false`）。
- 内容资产目录约定：`Prefabs/` `Sprites/` `Materials/` `Shaders/` `Particles/` `Textures/`。
- 文档：`Documentation~/控件库.md`（机制与边界）、`Documentation~/匹配规范.md`（匹配 id 的推荐命名：手填字符串，无枚举、无常量表）。
- 匹配组件：`TokenMatch`（通用组件，条目列表 = `id` + `target`）与 `TokenMatchEntry`；`ApplyFrom(UIKitProjectSo)` 逐条按 id 查 token 并写值。
- token：`TokenSo` 基类（`MatchId` / `TargetType` / `Apply(Component)` / `Accepts(Component)`）+ 5 个原子实现，**一个 token 只写一个属性**：`FontAssetTokenSo`（字体）、`FontSizeTokenSo`（字号）、`FontStyleTokenSo`（样式）、`LineSpacingTokenSo`（行距）、`ColorTokenSo`（颜色）。
- 项目 SO：`UIKitProjectSo`（`Prefabs` = 全体应用的扫描范围 + `Tokens` = token 库 + `GetTokens(id)`）。
- 校验：`TokenMatchValidator`（内部纯逻辑）——空条目 / 没填 id / 没指定 target / 缺 token / target 类型不符，以及「token 没填 MatchId」的提醒（不算问题）。
- 编辑器：`UIKitNewProjectWizard`、`UIKitProjectScaffolder` + `UIKitScaffoldPaths` + `UIKitProjectCreateOptions` / `UIKitProjectScaffoldResult`、`UIKitValidationWindow`、`UIKitNewTokenWizard`（类型下拉来自 `TypeCache.GetTypesDerivedFrom<TokenSo>()`）、`UIKitNewPrefabWizard`、`UIKitApplier` + `UIKitApplyPlan`、`UIKitApplyPreviewWindow`、`UIKitPage`（FangHub「控件库」页）。
- 测试：`Tests/Editor`（`Fang.Framework.UI.Kit.Editor.Tests`），66 项 EditMode 测试全绿。
