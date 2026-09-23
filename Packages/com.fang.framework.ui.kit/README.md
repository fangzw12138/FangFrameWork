# Fang Framework · UI Kit

`com.fang.framework.ui.kit` —— Fang Framework 的开箱即用 UI 控件库：控件 + 动效 + 粒子，**只支持 URP**。

包内只读、随包升级；定制走工程里的 Prefab Variant（改 icon、局部结构），主题由「匹配组件 + token SO + 项目 SO」在编辑器期批量写入。

## 依赖

| 依赖 | 落地形式 | 说明 |
| --- | --- | --- |
| `com.fang.framework` | asmdef 引用 `Fang.Framework` | 核心包，必须 |
| `com.fang.framework.ui` | asmdef 引用 `Fang.Framework.UI` | 面板基类与 `UIService`，必须 |
| DOTween | asmdef 引用 `DOTween` | 动效，**硬依赖** |

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
    "com.fang.framework.ui.kit": "https://github.com/fangzw12138/FangFrameWork.git?path=/Packages/com.fang.framework.ui.kit#com.fang.framework.ui.kit/v0.1.0"
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
│   └── 匹配规范.md                      匹配 id（手填字符串）的推荐命名
├── Runtime/
│   ├── Fang.Framework.UI.Kit.asmdef    全平台，autoReferenced
│   └── AssemblyInfo.cs
├── Editor/
│   └── Fang.Framework.UI.Kit.Editor.asmdef
├── Prefabs/                             包内控件预制体（只读）
├── Sprites/
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
├── 匹配组件（TokenMatch）             ├── token SO（字体 / 字号 / 样式 / 行距 / 颜色，独立资产）
└── 匹配规范文档                       └── 项目 SO（UIKitProjectSo ← 驱动者）
```

`UIKitProjectSo` 管两件事：`Prefabs`（全体应用的扫描范围）与 `Tokens`（token 库）。每条 token 自己带 `MatchId`（**手填字符串**，如 `Text/Title`），`GetTokens(id)` 按它过滤。运行时 `match.ApplyFrom(project)` 逐条查 token 并 `token.Apply(target)`；编辑器期批量写回预制体走 `UIKitApplier`（预览 → 确认 → 落盘）。完整闭环、规则动作类型与边界见 `Documentation~/控件库.md`，匹配 id 见 `Documentation~/匹配规范.md`。

## 用法

1. 建 token：`Tools/Fang Framework/Fang Hub → 控件库 → token 库 → ＋`（选类型 → 逐项填字段 → **手填匹配 id**），或 `Create > Fang Framework/UI Kit > 字号 Token / 颜色 Token` 再手填 `MatchId`（`Text/Title`、`Color/Primary`…，见 `Documentation~/匹配规范.md`）。
2. 建项目配置：`控件库项目 ▾ → 新建控件库项目配置 SO…`，把要管的 prefab 与 token 登记进去。
3. 在预制体节点上挂 `TokenMatch`，每条填一个匹配 id 与 `target` 组件（id 可手填，也可点「选择…」按项目 SO 挑）。
4. 应用：`控件库 → 全体应用`（预览后落盘），或运行时 `TokenMatch.ApplyFrom(project)` 逐条查 token 写值。

```csharp
var match = GetComponent<TokenMatch>();
match.ApplyFrom(project);      // 逐条：GetTokens(id) → token.Apply(target)
```

## 状态

已落地：包骨架、匹配组件、5 类 token、项目配置 SO、校验、FangHub「控件库」页（组件库 / token 库、两个引导向导、统计、校验、全体应用与预览落盘）、`TokenMatch` 条目 id 选择器。

未落地：v1 控件与预制体、动效、UI 粒子。

## 许可证

MIT，见 `LICENSE.md`。
