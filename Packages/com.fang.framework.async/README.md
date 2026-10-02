# Fang Framework · Async

`com.fang.framework.async` —— 基于 UniTask 的异步 Scope / Service / Controller 族。

与核心包的 `Scope` / `Service` / `Controller` 是**两个独立的族，不继承、不混用**：一棵 Scope 树要么整棵同步族，要么整棵异步族。

## 依赖

| 依赖 | 说明 |
| --- | --- |
| `com.cysharp.unitask` `2.5.11` | 来自 OpenUPM 注册表 `package.openupm.com`；**写在 `package.json` 的 `dependencies` 里**（registry 能回答），FangHub 安装本包前会自动补上注册表 |
| `com.fang.framework` `>= 0.5.2` | 核心包，只读复用 `Data<TConfig>` / `ConfigDataSo` / `ITickable`。**不写在 `package.json` 的 `dependencies` 里** —— 核心包按 git URL + tag 分发、不在任何 registry 里，UPM 解析不了（核心 `扩展包分发.md` 事实 2）。这条依赖靠 asmdef 的 `references`（编译期真实生效）与 FangHub「框架与扩展」页的前置检查（核心未装时禁用安装按钮）落地 |

**核心包本身仍然零第三方依赖**：上面两条都是**本包**的依赖，核心包不因本包存在而新增任何依赖。

## 安装

`Tools/Fang Framework/Fang Hub` → 侧栏「扩展包」→ 点「刷新」→ 点「安装」。

## 状态

`0.1.0` 开发中。用法见 `Documentation~/异步快速开始.md`。

## 许可证

MIT，见 `LICENSE.md`。
