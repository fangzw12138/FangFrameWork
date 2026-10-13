# Fang Framework · Async Command Console

`com.fang.framework.commandconsole.async` —— 挂在**异步 Scope** 上的调试指令层。

与**同步族**的 `com.fang.framework.commandconsole` 是两个独立的包，**不继承、不混用**：一棵 scope 树要么整棵同步族、要么整棵异步族。

指令是一个**静态方法**，形如：

```csharp
[DebugCommand("open_panel", "打开指定面板", "ui")]
public static object OpenPanel(DebugCommandContext ctx, DebugCommandArgs args)
{
    var ui = ctx.GetService<UIService>();          // 沿异步 scope 树查找
    ui.OpenAsync(args.Get(0, "panelId"));
    return new { opened = args.Get(0, "panelId") };
}
```

## 关键约定

| 项 | 约定 |
| --- | --- |
| 指令签名 | 必须**精确**是 `static object M(DebugCommandContext, DebugCommandArgs)`；返回 `object`（匿名类型/字典均可，序列化成 JSON） |
| 业务错误 | 抛 `DebugCommandException` → 返回 `{ok:false,error}`，**不**打 Error 日志 |
| 其它异常 | 打 `Debug.LogError` 后返回 `{ok:false,error}` |
| 取服务 | 一律 `ctx.GetService<T>()`（`where T : class`）；先沿 `Parent` 向上找，再 BFS 向下兜底；找不到返回 `null`，**不抛** |
| 扫描范围 | 默认只扫「引用了本包程序集」的程序集，因此使用方程序集必须引用 `Fang.Framework.CommandConsole.Async` |
| 分类 | 不写分类即回落 `DebugCommandAttribute.DefaultCategory`（`system`）；包内置三条（`ping` / `list_commands` / `command_log`）用 `DebugCommandDefaults.BuiltInCategory`（`builtin`） |
| 构建安全 | 本包全部代码在 `#if UNITY_EDITOR \|\| DEVELOPMENT_BUILD` 内，Release 构建自动剔除；引用方同样要包 |

## 依赖

| 依赖 | 说明 |
| --- | --- |
| `com.cysharp.unitask` `2.5.11` | 来自 OpenUPM 注册表 `package.openupm.com`；写在 `package.json` 的 `dependencies` 里 |
| `com.unity.nuget.newtonsoft-json` `3.2.2` | 指令结果与传输协议（NDJSON）的序列化；写在 `package.json` 的 `dependencies` 里 |
| `com.fang.framework` / `com.fang.framework.async` | 核心包与异步族，提供 `ConfigDataSo` / `ITickable` / `AsyncScope` / `AsyncService`。按 git URL + tag 分发、不在任何 registry 中，故**不写**在 `dependencies` 里，靠 asmdef 的 `references` 落地 |

## 依赖方向

```text
Fang.Framework.CommandConsole.Async
  └── Fang.Framework.Async   AsyncScope / AsyncService / AsyncLifecycleBehaviour / ITickable
        └── Fang.Framework   ConfigDataSo / ITickable
```

## 目录

| 目录 | 内容 |
| --- | --- |
| `Runtime/Command` | 特性、参数、异常、条目、注册表、扫描器、上下文、内置默认值 |
| `Runtime/Config` | `DebugProjectSo`（监听地址/端口/自动启动/扫描前缀，零写入口） |
| `Runtime/Host` | `DebugCommandService`（`AsyncService` + `ITickable`）与内置指令 |
| `Runtime/Transport` | `TcpDebugCommandServer`（后台线程收、主线程执行） |
| `Editor` | FangHub「指令控制台（异步）」页（只读清单）+ 指令控制台窗口 + `Docs/DebugCommands.md` 文档生成器 |
| `Tests/Editor` | EditMode 单测（扫描器 / 注册表 / 上下文 / FangHub 页发现） |

## 编辑器入口

| 入口 | 位置 | 能做什么 |
| --- | --- | --- |
| **指令控制台（异步）** | `Tools/Fang Framework/Fang Hub` → 侧栏 `Framework` 组 | 只读：指令清单、搜索、分类筛选、详情与调用方式、复制指令、导出文档、打开控制台 |
| 指令控制台 | 菜单 `Fang/调试/指令控制台` | 执行指令（活实例上）、看调用日志 |
| 导出指令文档 | 菜单 `Fang/调试/导出指令文档` | 生成 `Docs/DebugCommands.md` |

执行能力刻意留在独立窗口：FangHub 的 IMGUI 页在 PlayMode 下不渲染，而执行指令恰恰主要在 PlayMode 用。

## 传输协议

一行一个 JSON（NDJSON）。也接受纯文本行 `ping a b`（空格分隔，不做引号转义）。

```text
→ {"cmd":"ping","args":[]}
← {"ok":true,"data":{"pong":true,"listening":true,"ticking":true,"port":7777,"commands":4,"unityVersion":"6000.6.5f1"}}
```

## 安装

`Tools/Fang Framework/Fang Hub` → 侧栏「框架与扩展」→ 点「刷新」→ 点「安装」。

也可以按 git URL + tag 直接写进 `Packages/manifest.json`：

```jsonc
"com.fang.framework.commandconsole.async": "https://github.com/fangzw12138/FangFrameWork.git?path=/Packages/com.fang.framework.commandconsole.async#com.fang.framework.commandconsole.async/v0.1.1"
```

## 状态

`0.1.1`。协议、注册表与上下文查找已稳定；业务指令写在**使用方工程**的程序集里（该程序集必须引用 `Fang.Framework.CommandConsole.Async`）。

## 许可证

MIT，见 `LICENSE.md`。
