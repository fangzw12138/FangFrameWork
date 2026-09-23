# Fang Framework · UI Kit

`com.fang.framework.ui.kit` —— Fang Framework 的开箱即用 UI 控件库：控件 + 动效 + 粒子，**只支持 URP**。

包内只读、随包升级；定制走工程里的 Prefab Variant（改 icon、局部结构），主题由「匹配组件 + token SO + 项目 SO」在编辑器期批量写入。

**「只读」只对消费者成立** —— 把包放在工程 `Packages/` 下的**内嵌包**随便改（见 `Documentation~/定制与升级.md`）。

想先看到效果：`Package Manager → UI Kit → Samples → Import` 导入 **Controls Demo**（3 种按钮 + 3 种文本 + 演示场景），见下面「示例」。

## 示例（Samples）

`Package Manager → UI Kit → Samples → Controls Demo → Import`。

实测导入路径（Unity 6000.3.2f1）：

```text
Assets/Samples/UI Kit/<包 version>/Controls Demo/
├── Demo.unity                  3 按钮 + 3 文本，打开就能看
├── UIKitDemoProject.asset      项目 SO：登记 6 个 Variant + 8 条 token
├── Prefabs/                    6 个 Prefab Variant（引用包内基预制体）
├── Tokens/                     10 条 token（含 2 条被组合 token 嵌套的）
└── README.md                   怎么用 / 怎么迁
```

路径**带包版本段** ⇒ 升级后重新导入会落到新版本目录，**不会覆盖你改过的旧副本**。迁法见 `Documentation~/定制与升级.md`。

## 依赖

| 依赖 | 落地形式 | 说明 |
| --- | --- | --- |
| `com.fang.framework` | asmdef 引用 `Fang.Framework` | 核心包，必须 |
| `com.fang.framework.ui` | asmdef 引用 `Fang.Framework.UI` | 面板基类与 `UIService`，必须 |
| DOTween | asmdef 引用 `DOTween` | 动效，**预留依赖** |

**DOTween 目前不是运行必需**：asmdef 里引用了它，但 v1 的动效资产是 `AnimationClip`、播放走内置的 `PlayableGraph`（见 `Documentation~/动效.md`），所以**没装 DOTween 本包也照样编译、控件照样能用**（已实测）。等动效落地用上补间时它才真正必需；`package.json` 的 `dependencies` 里没有它，是因为 UPM 不支持包内声明 git URL / Asset Store 依赖。

DOTween **不在引擎 registry、也不在 OpenUPM**，只能从 Asset Store 或官网 zip 装到 `Assets/Plugins/Demigiant/`（本包按 `DOTween_1_3_030` 验证）。装上即用：该 `.unityPackage` 自带 `Assets/Resources/DOTweenSettings.asset`，`DG.DOTweenEditor.EditorUtils.DOTweenSetupRequired()` 返回 `false`，不需要再跑 Utility Panel 的 Setup。

`package.json` 的 `dependencies` 是空对象 —— UPM 不支持包内声明 git URL 依赖，第三方硬依赖（DOTween 不在 registry）也写不进 `dependencies`，所以依赖关系靠 asmdef 引用 + 核心包「框架与扩展」页的前置检查 + 本文档声明。

### DOTween 的引用方式（实测，非推测）

DOTween 官方包**不带 asmdef**：核心是预编译 `DOTween.dll`（程序集名 `DOTween`），而 `Modules/*.cs`（`DOTweenModuleUI` 等）落在预定义程序集 `Assembly-CSharp-firstpass` 里。**asmdef 无法引用预定义程序集**，所以：

- 本包只引用 `DOTween` 核心 DLL（asmdef `references` 里写 `"DOTween"`）；
- UI 补间一律走核心 API（`DOTween.To(getter, setter, ...)`、`ShortcutExtensions` 的 `RectTransform` 快捷方法），**不依赖** `DOTweenModuleUI` 的扩展方法。

## 安装

**方式一：FangHub 的「框架与扩展」页**（推荐）

`Tools/Fang Framework/Fang Hub` → 侧栏点「框架与扩展」→ 点「刷新」→ 点「安装」。

**方式二：手写 `Packages/manifest.json`**

```json
{
  "dependencies": {
    "com.fang.framework": "https://github.com/fangzw12138/FangFrameWork.git?path=/Packages/com.fang.framework#com.fang.framework/v0.5.1",
    "com.fang.framework.ui": "https://github.com/fangzw12138/FangFrameWork.git?path=/Packages/com.fang.framework.ui#com.fang.framework.ui/v0.1.0",
    "com.fang.framework.ui.kit": "https://github.com/fangzw12138/FangFrameWork.git?path=/Packages/com.fang.framework.ui.kit#com.fang.framework.ui.kit/v0.2.1"
  }
}
```

需要本机有 git CLI。渲染管线只支持 URP，BiRP / HDRP 工程装上会粉。

## 包结构

```text
Packages/com.fang.framework.ui.kit/
├── package.json
├── README.md / CHANGELOG.md / LICENSE.md
├── Documentation~/                     目录名带 ~ 后缀，不参与 Unity 导入
│   ├── 控件库.md                        机制与边界
│   ├── 匹配规范.md                      匹配 id（手填字符串）的推荐命名
│   ├── 动效.md                          动效架构与播放机制（含实测结论）
│   └── 定制与升级.md                    「包内只读」的机制、Variant vs 复制、升级迁移
├── Runtime/
│   ├── Config/                         token 与动效资产
│   ├── Domain/                         KitButton / TokenMatch
│   ├── Internal/                       校验
│   ├── Fang.Framework.UI.Kit.asmdef    全平台，autoReferenced
│   └── AssemblyInfo.cs
├── Editor/
│   └── Fang.Framework.UI.Kit.Editor.asmdef
├── Prefabs/                             包内控件预制体（只读；路径不改名、不移动）
├── Sprites/                             占位图：圆角底 / 圆点 / 菱形
├── Samples~/Demo/                       示例（Package Manager → Samples → Import）
├── Materials/
├── Shaders/
├── Particles/
└── Textures/
```

内容资产目录（`Prefabs/` `Sprites/` `Materials/` `Shaders/` `Particles/` `Textures/`）只放**包内只读内容**：控件预制体、图标、材质、Shader、粒子。这条约定只写在本包文档里，核心包 `目录规范.md` 不动。

## 机制

包内（只读、随包升级）与工程里（你自己建、随便改）分工：

```text
包内                                工程里
├── 控件预制体（节点带匹配组件）       ├── Prefab Variant（引用包内控件，改 icon / 局部）
├── 匹配组件（TokenMatch）             ├── token SO（原子：一个属性一个 token；组合：一个控件一条）
└── 匹配规范文档                       └── 项目 SO（UIKitProjectSo ← 驱动者）
```

`UIKitProjectSo` 管两件事：`Prefabs`（全体应用的扫描范围）与 `Tokens`（token 库）。每条 token 自己带 `MatchId`（**手填字符串**，如 `Text/Title`），`GetTokens(id)` 按它过滤。运行时 `match.ApplyFrom(project)` 逐条查 token 并 `token.Apply(target)`；编辑器期批量写回预制体走 `UIKitApplier`（预览 → 确认 → 落盘）。

token 分两类：

- **原子**：一个 token 只写一个属性（`FontSizeTokenSo` 写字号、`ColorTokenSo` 写颜色…）。
- **组合**：一个 token 管一整个控件（`ButtonTokenSo` 管按钮的底图 / 5 态 / 文字 / 图标 / 动效，靠**嵌套**原子化的 `TextTokenSo` + `IconTokenSo`，并通过 `KitButton` 上的**引用**找到按钮里的文字与图标）。

**同一个槽位只放一种**，别混（详见 `Documentation~/控件库.md` §四、匹配 id 见 `Documentation~/匹配规范.md`）。

## 用法

1. 控件：做包内预制体的 **Prefab Variant**（`ButtonIconText` / `ButtonIcon` / `ButtonText` / `TextTitle` / `TextBody` / `TextCaption`），或直接用 `Samples~/Demo` 里导入的那 6 个。
2. 建 token：`Tools/Fang Framework/Fang Hub → 控件库 → token 库 → ＋`（选类型 → 逐项填字段 → **手填匹配 id**），或 `Create > Fang Framework/UI Kit > …` 再手填 `MatchId`（`Text/Title`、`Button/Primary`…，见 `Documentation~/匹配规范.md`）。按钮用**组合** token（`Button/*`），文本 / 图标用**原子** token（`Text/*`、`Icon/*`）。
3. 建项目配置：`控件库项目 ▾ → 新建控件库项目配置 SO…`，把要管的 prefab 与 token 登记进去。
4. 在预制体节点上挂 `TokenMatch`，每条填一个匹配 id 与 `target` 组件（id 可手填，也可点「选择…」按项目 SO 挑）。按钮的槽位 `target` 填那个 **`KitButton`** 组件。
5. 应用：`控件库 → 全体应用`（预览后落盘），或运行时 `TokenMatch.ApplyFrom(project)` 逐条查 token 写值。

```csharp
var match = GetComponent<TokenMatch>();
match.ApplyFrom(project);      // 逐条：GetTokens(id) → token.Apply(target)
```

## 状态

**已落地**：

- 骨架与机制：匹配组件、项目配置 SO、校验、FangHub「控件库」页（组件库 / token 库、引导向导、统计、校验、全体应用与预览落盘）、`TokenMatch` 条目 id 选择器。
- **v1 控件**：3 种按钮（图标 / 文本 / 图标+文本）+ 3 种文本（标题 / 正文 / 说明），6 个包内预制体 + 占位图。
- **token**：5 个原子类 + 3 个组合类（`TextTokenSo` / `IconTokenSo` / `ButtonTokenSo`），组合可嵌套一层。
- **动效架构**：`UiMotionSo` 资产 + `KitButton` 两个槽位（播放机制已实测，见 `Documentation~/动效.md`）。
- **示例**：`Samples~/Demo`。

**未落地**：动效的具体实现与播放器（只定了资产与槽位）、UI 粒子、Shader / 材质、运行时换肤入口、DTCG / Figma 导入。

## 动效

动效**资产**是 `UiMotionSo`（`AnimationClip` + 循环），**槽位**在 `KitButton` 上（`OnClick` / `OnLocked`），由组合 token `ButtonTokenSo` 在编辑器期写进组件字段；**播放**在运行时。

v1 **只定架构、不实现播放**。已实测的播放机制（裸 `Animator` + `PlayableGraph` 可播、`AnimationClip.SampleAnimation` 运行时可播）以及「动效按节点路径绑定 ⇒ 一条 token 只服务一种结构」等要点见 `Documentation~/动效.md`。

## 许可证

MIT，见 `LICENSE.md`。
