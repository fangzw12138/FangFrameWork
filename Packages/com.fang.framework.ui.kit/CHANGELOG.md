# Changelog

## [0.3.2] - 2026-10-01

### Fixed

- **进度条 / HUD 数值条的填充「图形不对」**：填充用的是 `Image` 的 `Filled` 横向 + `fillAmount`。`Filled` **不认 9 宫格 `border`** —— 它只发一个 quad，再按 `fillAmount` 把贴图连 UV 一起缩放：圆角被横向压扁（不再是圆弧），填充的右端还是硬切，与底图（`Sliced`、圆角正常）对不上，一眼就能看出来。现在填充改成 **`Sliced` + 锚点拉宽**：`ProgressBar` / `HudBar` 的 `Fill` 是 `Sliced` 的 9 宫格图，`anchorMin.x = 0`、`anchorMax.x = 比例`（默认仍是 50%），`KitProgress.SetValue` 写 `Fill.rectTransform.anchorMax.x`、`Normalized` 从锚点读回 —— 与 Unity 自己的 `Slider` 填充同款（`Slider` 的填充一直是这么做的，所以它没这个问题）。
- `KitProgress` 保留 `Filled` 分支（`_fill.type == Filled` 时仍写 `fillAmount`）：别人已经做好的 `Filled` 预制体不会被这次改动弄坏。
- 回归测试 3 项：包内线性填充不得出现「`Filled` + `Horizontal` / `Vertical`」（同时断言 `SkillButton` 的径向冷却遮罩仍是 `Filled`）、`KitProgress` 对 `Sliced` 填充写锚点并夹在 0..1、对 `Filled` 填充仍写 `fillAmount`。

### Changed

- 包版本 `0.3.1` → `0.3.2`。
- 文档：`控件库.md`（`ProgressBar` 结构、`KitProgress` 契约行、比例说明）、`匹配规范.md` / `README.md` / 示例 `README.md` 的比例表述、`定制与升级.md` 新增「线性的填充别用 `Filled` 横向」。

验证：refresh 0 error / 0 warning；ui.kit EditMode **110 项全绿**（107 + 3）。

## [0.3.1] - 2026-10-01

### Fixed

- **`Switch` 的文字被压成竖排**：`LabelSlot` 的 `LayoutElement` 没填 `preferredWidth`（是 -1）。`HorizontalLayoutGroup` 的 `childControlWidth` 是按**子节点的 preferred 宽度**驱动子节点宽度的，而槽位节点自己不是 `ILayoutElement` 的实现者（只有 `LayoutElement`），所以宽度算出来是 0 —— 槽位里的文字被压成一行一个字（实测「S / w / i / t / c / h」）。现在 `LabelSlot` 给了 `preferredWidth 64 / minWidth 24`，标签文字设为 `NoWrap`，与包内按钮的 `LabelSlot`（`pref 72x16`）一致；文案更长时按需覆盖这两个值（在 Variant 上覆盖即可）。

### Changed

- 包版本 `0.3.0` → `0.3.1`。

## [0.3.0] - 2026-10-01

### Added

**9 个新控件**（包内预制体 + 运行时组件 + 组合 token），按 PC 基线（参考 1920×1080，1 UI 单位 = 1 像素）：

| 控件 | 预制体 | 结构 | 匹配 id |
| --- | --- | --- | --- |
| 输入框 | `InputField` | 底 + `TMP_InputField` + 占位 + 可选前置图标 | `Field/Input`（组合） |
| 下拉 | `Dropdown` | 底 + `TMP_Dropdown`（标题 / 箭头 / 列表模板，模板里含作原型的 Toggle 项） | `Field/Dropdown`（组合） |
| 开关 | `Switch` | `Toggle` + 轨道 + 滑块 + 文字（横向布局 + ContentSizeFitter） | `Switch/Default`（组合） |
| 滑条 | `Slider` | `Slider` + 底 + 填充 + 手柄 | `Slider/Default`（组合） |
| 进度条 | `ProgressBar` | 底 + 填充（Filled）+ 数值文字 | `Progress/Background` `Progress/Fill` `Progress/Value` |
| HUD 数值条 | `HudBar` | 图标 + 条 + 数值文字 | `Hud/Background` `Hud/Fill` `Hud/Icon` `Hud/Value` |
| Toast | `Toast` | 9 宫格底 + `CanvasGroup` + 图标 + 消息 | `Toast/Background` `Toast/Icon` `Toast/Message` |
| Tooltip | `Tooltip` | 9 宫格底 + 横向布局 + ContentSizeFitter（自适应） | `Tooltip/Background` `Tooltip/Text` |
| 技能按钮 | `SkillButton` | 底 + `Button` + 图标 + 冷却遮罩 + 冷却秒 + 等级 | `Button/Primary`（复用）+ `Skill/Cooldown` `Skill/CooldownText` `Skill/Level` |

- **运行时组件**：`KitInputField` / `KitDropdown` / `KitSwitch` / `KitSlider` / `KitProgress` / `KitSkillButton`（**继承 `KitButton`**）/ `KitToast` / `KitToastHost`（队列与回收）/ `KitTooltip`。它们只给**结构契约**（只读引用）与**最小接口**（`SetValue` / `SetState` / `SetCooldown` / `SetLevel` / `Show` / `Hide`），不接游戏逻辑。
  `KitSkillButton` 继承 `KitButton` 的理由与「三种按钮共用一个类」同理：底 / 图标 / 文字 / 5 态完全同构，继承后 `Button/Primary` 这类组合 token 直接可用（`TargetType.IsInstanceOfType` 成立），只多三个槽位。为此 `KitButton` 去掉了 `sealed`（源码兼容）。
- **组合 token 4 个**：`FieldTokenSo` / `DropdownTokenSo` / `SwitchTokenSo` / `SliderTokenSo`，与 `ButtonTokenSo` 同构：写底图 Sprite、5 态 `ColorBlock`、并**嵌套**原子化的 `TextTokenSo` / `IconTokenSo`。值类字段带 `bool` 开关，引用类字段「空 = 不管」。
- **不新增原子 token 类**：`IconTokenSo` 本来就是「Image ← Sprite + 颜色」，所以进度条的底 / 填充、Toast / Tooltip 的底、技能按钮的冷却遮罩这些 Image 槽位一律复用它，靠**语义 id** 区分（`Documentation~/控件库.md` §四 的既有原则：差异靠不同的 token 资产表达，不靠不同的类）。**比例（`fillAmount`）是运行期数据不是主题**，因此没有 `FillTokenSo` 之类，走运行时接口。
- 开关的开 / 关配色（轨道与滑块各两色）存在 `KitSwitch` 上、由 `SwitchTokenSo` 整组写入 —— 和 `ButtonTokenSo` 把 `ColorBlock` 写进按钮一样。Token 里用**一个** `_useColors` 开关表示「整组写 / 不碰」，因为这四个颜色是一个整体。
- 示例扩到 **15 个 Variant + 27 条 token**（含 6 条按钮嵌套 + 13 条新控件嵌套的子 token），`Demo.unity` 加三列（`Fields` / `Bars` / `Popups`）。
- 文档：`匹配规范.md` 补 8 个新 id 组；`控件库.md` 补新组合 token 与「Image 槽位复用 `IconTokenSo`」；`定制与升级.md` 补新控件尺寸基线；`README.md` / 示例 `README.md` 同步。

### Changed

- 包版本 `0.2.2` → `0.3.0`（新增控件属 minor）。
- `KitButton` 去掉 `sealed`，以便 `KitSkillButton` 继承。

### Fixed

- **`UIKitApplier` 的 `MatchIndex` 语义不匹配（真 bug，会静默跳过）**：`Build` 里记的是 `GetComponentsInChildren<TokenMatch>()` 的**全预制体序号**，`Apply` 里却用 `node.GetComponents<TokenMatch>()[MatchIndex]`（**单节点**列表）去取 —— 只要一个预制体里的 `TokenMatch` 总数 ≥ 2（哪怕一个节点一个），**非首个节点**的行就会报「TokenMatch 数量对不上」并跳过。现在 `Build` 按**同一节点内**的序号编号，与 `Apply` 对齐；于是「每个预制体只留一个 `TokenMatch`、容器做成空壳」这条规避不再需要（`ProgressBar` / `HudBar` / `Toast` 本身就是多节点 `TokenMatch`）。
- **同级重名兄弟会写错节点**：`BuildNodePath` 只用节点名拼路径、`ResolveNode` 用 `Transform.Find` 解析，两个同名兄弟永远命中第一个（写错对象且不报错）。现在**只有同级重名时**路径段才附兄弟序号（如 `Content/Item#2/Icon`），不重名时路径保持原样可读。
- 回归测试 3 项：跨节点多个 `TokenMatch` 全部落盘、同一节点两个 `TokenMatch` 各写各的、同名兄弟各写各的。

验证：refresh 0 error / 0 warning；ui.kit EditMode **107 项全绿**；示例「全体应用」26 条落盘、0 跳过、0 报错。

## [0.2.2] - 2026-09-30

### Fixed

- **2022.3 / 团结引擎 1.6 上编译不过**：控件库页搜索框的 `searchField.textEdition.placeholder` 是 Unity 6 才有的 UI Toolkit 成员（2022.3 的 `ITextEdition` 没有 `placeholder`），加 `#if UNITY_6000_0_OR_NEWER` 守卫。2022.3 上搜索框没有占位提示，其余行为不变。

### Changed

- 包版本 `0.2.1` → `0.2.2`。

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
