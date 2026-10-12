# Changelog

## [0.1.0] - 2026-10-12

### Added

- **Runtime**（程序集 `Fang.Framework.CommandConsole.Async`，引用 `Fang.Framework` / `Fang.Framework.Async` / `UniTask`；`dependencies` 另需 `com.unity.nuget.newtonsoft-json`）：
  - `DebugCommandAttribute`：把一个签名精确为 `static object M(DebugCommandContext, DebugCommandArgs)` 的静态方法登记为指令。
  - `DebugCommandArgs` / `DebugCommandException` / `DebugCommandEntry` / `DebugCommandCallRecord`。
  - `DebugCommandScanner`：按「引用了本包的程序集」或指定前缀扫描已加载程序集，签名不合规给出警告。
  - `DebugCommandRegistry`：注册 / 查找 / 执行 / 调用日志（环形缓冲），协议入口支持 NDJSON 与纯文本行。
  - `DebugCommandContext`：沿 `AsyncScope.Parent` 向上查找、BFS 向下兜底，歧义时仅一条警告。
  - `DebugProjectSo`：监听地址 / 端口 / 自动启动 / 扫描前缀，零写入口。
  - `DebugCommandService`：`AsyncService` + `ITickable`，由 `AsyncScope.Tick` 驱动。
  - `TcpDebugCommandServer`：后台线程接收、主线程 `Drain` 执行并回写。
  - 内置指令 `ping` / `list_commands` / `command_log`。
- **Editor**（程序集 `Fang.Framework.CommandConsole.Async.Editor`）：指令控制台窗口 + `Docs/DebugCommands.md` 文档生成器。
- **Tests/Editor**（程序集 `Fang.Framework.CommandConsole.Async.Tests`）：EditMode 单测（扫描器 / 注册表 / 上下文）。
- `README.md` / `CHANGELOG.md` / `LICENSE.md`。

### Notes

- 全部运行时代码包在 `#if UNITY_EDITOR || DEVELOPMENT_BUILD` 内，Release 构建自动剔除；引用方同样要包。
- 与**同步族**的 `com.fang.framework.commandconsole`（程序集 `Fang.Framework.CommandConsole`）互相独立、不继承、不混用：一棵 scope 树要么整棵同步族、要么整棵异步族。
- `package.json` 的 `dependencies` **只声明** registry 能回答的两项（`com.cysharp.unitask` / `com.unity.nuget.newtonsoft-json`），**不声明核心包与异步族** —— 它们按 git URL + tag 分发、不在任何 registry 里，写进去会解析失败。对它们的依赖靠 asmdef 的 `references` 落地。
