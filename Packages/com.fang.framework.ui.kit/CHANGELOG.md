# Changelog

## [0.2.1] - 2026-09-23

### Changed

**按 PC 基线重调字号与尺寸**（0.2.0 那套是移动端量级）。参考分辨率仍是 1920×1080 + `matchWidthOrHeight 0.5`，屏幕为 1080p 时 **1 UI 单位 = 1 像素**，所以下面都是像素值。

| 项 | 0.2.0 | 0.2.1 |
| --- | --- | --- |
| 标题 | 56 | **28** |
| 正文 | 32 | **16** |
| 说明小字 | 22 | **12** |
| 按钮文字 | 22 | **16** |
| 按钮高 | 44 | **32** |
| 图标槽 | 24×24 | **16×16** |
| 标签槽 | 110×24 | **72×16** |
| 按钮内边距 / 间距 | 14 / 8 | **10 / 4** |

- 包内预制体默认值：`TextTitle` 48→24、`TextBody` 30→16、`TextCaption` 20→12、按钮文字 26→16；按钮 `sizeDelta` 160×48→120×32；文本预制体 `sizeDelta` 400×64/40/28→300×32/22/16。
- 示例 token：`TitleText` 56→28、`BodyText` 32→16、`CaptionText` 22→12、`ButtonLabelText` 22→16。
- `Sprites/ButtonBackground.png`：圆角 12→6，9 宫格 `border` 20→10。**必须与上面一起改**：`Image` 在 `Sliced` 下的 `preferredHeight` 等于 `border` 四边之和（原为 40），按钮根的 `ContentSizeFitter` 取最大值 —— border 太大时按钮**压不到 32 高**（实测卡在 40，改字号和 padding 都无效）。改后四边和为 20。
- 示例场景：两列位置收紧（`Buttons` −140→−80、`Texts` −440→−260），列间距 20→12；相机底色 `F0F2F7`→`FAFBFC`。
- 示例 token `ButtonSecondary` 的 5 态颜色调深（normal `E8EDF5`→`E1E8F0`）：原来的浅底在 `F0F2F7` 页面上只差 8 级，几乎看不见。

### Added

- `Documentation~/控件库.md` §七 补「9 宫格底图的 `border` 决定控件最小尺寸」；`Documentation~/定制与升级.md` 补「换底图时注意 9 宫格的 border」与「尺寸基线」。

### Fixed

- 示例的 6 个 Prefab Variant 重新生成：清掉 0.2.0 落盘时被 `ContentSizeFitter` 写进根 `RectTransform` 的尺寸 override（Variant 里存的是 160×48，与基预制体的 120×32 不一致），同时保住「文字 / 图标形状走 Variant 覆盖」的演示。

验证：refresh 0 error / 0 warning；ui.kit EditMode 104 项全绿；把 Game View 设成 **1920×1080**（scaleFactor = 1）实测 —— 按钮 112×32 / 36×32 / 92×32，正文 16px、标题 28px、说明 12px，主按钮 `4A8FD9`、次按钮 `E1E8F0`、文字按钮无底（`2E4A66`）。

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
