# UI Kit 示例

`com.fang.framework.ui.kit` 的示例：3 种按钮 + 3 种文本，一套 token、一个演示场景。**导入后可以直接改**——这里的东西都是你的工程资产，不是包内资产。

机制说明看包内 `Documentation~/控件库.md`，匹配 id 看 `Documentation~/匹配规范.md`，动效看 `Documentation~/动效.md`。

## 怎么用

1. 打开 `Demo.unity`，能看到 3 个按钮（主按钮 / 图标按钮 / 文字按钮）和 3 段文本。
2. 选中 `UIKitDemoProject.asset`，这就是「项目 SO」：`Tokens` 是 token 库，`Prefabs` 是「全体应用」的扫描范围（登记的是本示例里的 6 个 Variant）。
3. `Tools/Fang Framework/Fang Hub → 控件库` → 选这个项目 → 「校验」应为**零问题** → 「全体应用」预览确认后落盘。
4. 改 `Tokens/` 里的任意 token（颜色 / 字号），再「全体应用」一次，`Prefabs/` 里的 Variant 就跟着变，场景里的实例也一起变。

## 目录

```text
Demo/
├── Demo.unity                  演示场景（Canvas + 6 个 Variant 实例 + EventSystem）
├── UIKitDemoProject.asset      项目 SO：登记 6 个 Variant + 8 条 token
├── Prefabs/                    6 个 Prefab Variant（引用包内基预制体，随便改）
│   ├── ButtonIconText / ButtonIcon / ButtonText
│   └── TextTitle / TextBody / TextCaption
└── Tokens/
    ├── ButtonPrimary.asset     Button/Primary    ← 组合 token（嵌文字 + 图标 token）
    ├── ButtonSecondary.asset   Button/Secondary  ← 组合 token（嵌图标 token）
    ├── ButtonGhost.asset       Button/Ghost      ← 组合 token（嵌文字 token）
    ├── TitleText.asset         Text/Title
    ├── BodyText.asset          Text/Body
    ├── CaptionText.asset       Text/Caption
    ├── ButtonLabelText.asset   （匹配 id 留空，被组合 token 嵌套）
    ├── ButtonIconToken.asset   （匹配 id 留空，被组合 token 嵌套）
    ├── PrimaryIcon.asset       Icon/Primary
    └── SecondaryIcon.asset     Icon/Secondary
```

## 两条链路，别混

| | 谁管 | 例子 |
| --- | --- | --- |
| **按钮整体**（底图 / 5 态颜色 / 文字 / 图标 / 动效） | **组合 token**（`ButtonTokenSo`），靠 `KitButton` 上的引用找到文字和图标 | `Button/Primary` 一条就管完整个按钮 |
| **独立元素**（一段文本、一个图标） | **原子 token**（`TextTokenSo` / `IconTokenSo`） | `Text/Title`、`Icon/Primary` |

**同一个槽位只挂一条组合 token**，别再往同一个 id 上叠原子 token，否则同一个属性会被写两遍。

`ButtonLabelText` / `ButtonIconToken` 的匹配 id 是**故意留空**的：它们只被组合 token 嵌套引用，不参与匹配，所以也不登记进 token 库。留空在这里表示「只被引用」，不是「还没启用」。

`PrimaryIcon` / `SecondaryIcon`（`Icon/Primary` / `Icon/Secondary`）是**独立图标元素**用的原子 token。本示例的按钮图标由组合 token 的嵌套图标 token 管，所以这两条在示例里没有匹配方——它们是留给「不在按钮里的图标」的，id 已按规范冻结。

## 图标怎么改

按钮图标的**形状**走 **Variant 覆盖**（`Prefabs/ButtonIconText` 把 `IconSlot/Icon` 的 Sprite 覆盖成了 `IconDiamond`），**颜色**走 token。这正是推荐做法：形状是每个控件的个性，颜色是主题。

## 中文文字

示例文案里有中文（「主按钮」「标题」…）。TMP 的默认字体（LiberationSans SDF）没有中文字形，需要工程里配一个中文 **fallback 字体**，否则会显示方块。配置方式见核心包文档；本仓库开发工程用 Codely 的 TMPChineseFont 流程装了 Noto Sans SC fallback。

## 为什么不能改包内

包内资产在 `Library/PackageCache/`（走 Package Manager 安装时）或包目录里，**升级会被覆盖**。所以：

- 改外观 → 改这里的 **Variant**（改 icon、加节点、调尺寸都行），或者改 **token**。
- 包内的 `Prefabs/` 只是「基预制体」，它提供节点结构和 `KitButton` / `TokenMatch` 的接线，不要直接改。
- 内嵌包（把包放在工程 `Packages/` 下、随工程一起提交）不受此限制，可以随便改；只读只对「从 git / Package Manager 装的消费者」成立。

## 升级怎么迁

包升级后，这份示例**不会自动更新**（Package Manager 的 Samples 导入是「复制一份到 `Assets/`」，已经导入的副本不会被覆盖）。

想拿新版本示例：先把这份 `Demo` 改名或删掉，再重新 `Package Manager → UI Kit → Samples → Import`，然后把你自己的改动（token 值、Variant 覆盖）搬回去。

**你自己做的 Variant 不会因为包升级而断链**，前提是包内基预制体的**路径没变**。这也是本包的约定：已发布的包内 prefab 路径不改名、不移动。
