# Changelog

## [0.1.0] - 2026-09-24

### Added

**包骨架**

- `package.json`（`dependencies`: `com.unity.nuget.newtonsoft-json` 3.2.2）、`README.md` / `CHANGELOG.md` / `LICENSE.md`、`Documentation~/调试.md`。
- 程序集：`Fang.Framework.CommandConsole`（全平台，`autoReferenced: true`，引用 `Fang.Framework`）、`Fang.Framework.CommandConsole.Editor`（Editor，`autoReferenced: false`）、`Fang.Framework.CommandConsole.Editor.Tests`（EditMode）。
- 命名空间是 `Fang.Framework.CommandConsole`（**不能**叫 `Fang.Framework.Debug`：那会让 `Debug.Log(...)` 在自身及兄弟命名空间里编译失败，`CS0234`，已实测），理由见 `Documentation~/调试.md` §六。

**命令核心（纯逻辑）**

- `DebugCommandAttribute`（name / description / category，分类是自由字符串）、`DebugCommandArgs`（`Get` / `GetInt` / `GetFloat` / `GetBool` / `GetVector3Int`，错误文案统一带「第 n 个参数 <名>」与「实际值」）、`DebugCommandException`（业务侧「预期内失败」）、`DebugCommandEntry`。
- `DebugCommandScanner`：程序集 → 条目。校验签名（**连实例方法一起枚举**，所以「贴在非 static 方法上」会被抓到并警告，而不是静默失效）；空名跳过、重名后者覆盖、描述为空保留但警告；`ReflectionTypeLoadException` 走 `ex.Types` 容错。
- `DebugCommandRegistry`：登记 + 执行（`{ok,data}` / `{ok,error}`）+ 200 条环形调用日志（不打 Console）+ `ExecuteText`（控制台手工输入的一行文本）。
- `DebugCommandDefaults`：全部内置默认值集中一处（刻意不加编译守卫，因为 `DebugProjectSo` 在 Release 里也要能解析回落值）。
- 默认扫描范围 = **只扫引用了本包的程序集**（本机实测 3291 ms → 0 ms，扫到的指令完全一致），理由见 `Documentation~/调试.md` §三。

**上下文**

- `DebugCommandContext`：`Func<Scope>` 提供者 + `virtual GetService<T>()` 先沿核心包 Scope 解析链**向上**找、找不到再从当前 Scope 起**向下兜底**（子树广度优先；多命中取第一个并 `LogWarning`）、**找不到返回 null 不抛** + `TryGetService`；带 `Registry`（`list_commands` / `command_log` 要用）。只用 `Scope.Services` / `Parent` / `Children` 三个公开成员，零反射、核心包零改动。可继承、可换工厂，不用改包。

**传输**

- `DebugCommandRequest` / `DebugCommandProtocol`：唯一的 JSON 解析/格式化处。解析失败也会尽力把 `id` 从原文捞出来回填；`args` 非数组会明确报错而不是静默当空。
- `TcpDebugCommandServer`：后台线程只入队，主线程 `Drain()` 执行并写回（多客户端安全、慢客户端不卡主线程）；端口被占用返回 false + LogError 不抛；`Start` 幂等（会先停旧的再按新参数重开）；`port = 0` 让系统分配。

**宿主**

- `DebugCommandService`（`Service, ITickable`）：`OnInit` 用内置默认值起来（装机即用）、`Configure(project)` 幂等套用配置（改扫描范围才重扫，改端口才重开监听）、`OnTick` Drain、`OnDispose` 停服务并拆控制台。不写 Unity 消息方法。
- `DebugBuiltInCommands`：`ping`（报 listening / ticking / port / 指令数）、`list_commands`（可按名称/分类/描述过滤）、`command_log`。分类固定 `builtin`（与「忘标分类归 `system`」刻意分开），随扫描自动出现。
- `DebugCommandConsole`：游戏内 IMGUI 控制台（开关走 `OnGUI` 事件，不占用 `Update`；`IsTogglePressed()` 是 virtual，换 Input System 只需覆写）。内置 3 个快捷按钮 + SO 配置的 + 运行时 `RegisterQuickButton`；输出区公开可读。
- `DebugCommandService.ActiveServices`：编辑器侧拿 PlayMode 里实例的桥（读取时过滤已销毁引用，所以不依赖清理钩子）。

**配置**

- `DebugProjectSo`（`ConfigDataSo`）：运行参数 + 扫描范围 + 分类顺序 + 文档输出。字段都种了默认值，清空即回落。
- `DebugQuickButton`（`[Serializable]`，Label + CommandLine）。
- `DebugSettings`：把 SO（或 null）解析成生效配置，**回落规则只在这一处**。

**编辑器侧**

- `DebugPage`：FangHub「指令控制台」页（`framework-command-console`，Category `Framework`，Order 2）。菜单栏只放整体操作（调试项目 ▾ / 校验 / 导出文档 / 刷新 / 导入示例 + 状态标签）；标题行「当前的项目：X」+「定位」（跳到 SO 资产）；左列 = 列表头（标题 + 搜索）+ 分类筛选（全部 / 内置指令 / 各业务分类，**默认「全部」不含内置指令**）+ 摘要（共 N 条（内置 M 条）· K 个业务分类）；右详情 = 基本信息 Foldout 默认折叠 / 说明 / 怎么调 + 「复制指令」（复制 `指令名 参数名…`，参数名取自描述里的 `<...>`、不带尖括号）。页面不再内嵌 SO Inspector —— 要改配置点「定位」去 Project 窗口。
- `DebugCommandConsoleWindow`：`Tools/Fang Framework/指令/打开控制台`。进程内执行 + 历史下拉 + 输出区 + 调用日志 + 状态行（监听 / tick）。
- `DebugCommandDocBuilder`（纯逻辑，条目 + 配置 → `{路径: 内容}`）+ `DebugCommandDocGenerator`：`Tools/Fang Framework/指令/导出指令清单`。分类顺序读配置、没列出的分类排在后面（不丢）；表格转义 `|` 与换行；分类名非法字符换下划线、撞名加后缀；拒绝写工程目录外的路径。
- `DebugCommandValidator`（internal 纯逻辑）+ `DebugValidationWindow`：把**扫描器会打的警告**转成问题清单（同一套逻辑，校验里看到的就是运行时 Console 里看到的），另加配置项越界检查（看 SO 原始值）。
- `DebugNewProjectWizard`：引导窗口（每项一句说明、固定底栏、内容可滚动）。
- `DebugScaffoldPaths` / `DebugProjectLocator`：路径与命名校验、工程内配置查找。

**测试**

- `Tests/Editor`：167 项 EditMode 测试全绿。覆盖参数解析、扫描（签名/空名/重名/范围/排序）、注册表（执行/错误通道/调用日志/环形溢出/截断）、上下文（父子链/缺失返回 null/覆写/**向下兜底命中子 Scope/向上优先于向下/多命中取第一个并警告**）、协议（合法/非法 JSON/缺 cmd/args 非数组/id 回填）、TCP（真起服务 + 真 TcpClient 往返、错误响应、多请求单连接、双客户端、端口占用、地址非法、幂等重启）、服务（默认启动/Configure 幂等与生效/tick/内置指令/控制台）、配置（默认值/回落/去重）、文档生成（分类顺序/空域/拆文件/转义/路径规范化/确定性）、校验器。

**示例**

- `Samples~/Demo`：贴近真实的示例——三层 Scope（App / Game / Scene）+ 三个带可变状态的服务（会话 / 玩家 / 生成）+ 一个刻意不注册的服务 + 五个分类（`builtin` / `session` / `player` / `world` / `demo`）的指令 + 项目 SO + 场景 + 示例自己的 `README.md`。SO 刻意用非默认值（端口 7788、快捷键 F1、3 个快捷按钮、分类顺序），用来验证 `Configure` 真的生效。**调试指令服务装在 App 层（系统层）**，玩家 / 生成服务在 Scene 层 —— `player_*` / `world_*` 正是靠「向下兜底」命中的。示例脚本整段包在 `#if UNITY_EDITOR || DEVELOPMENT_BUILD` 里（包内类型在 Release 被剥空，示例不包会让消费者的 Release 构建编译失败）。
- FangHub 页菜单栏加「导入示例」（`Sample.FindByPackage` + `Import`；已导入先弹确认，导入后定位 `Demo.unity`）。

**验证**

- refresh 0 error / 0 warning。
- 沙盒手测（`Assets/Sandbox/`，验完已删）：TCP 往返含错误响应、游戏内控制台开关与输出、编辑器窗口在 PlayMode 下执行并记调用日志、文档按 SO 的顺序与路径生成、校验器报出签名不符、Console 零错误。
- 示例端到端（导入 `Assets/Samples/Command Console/0.1.0/Demo` 之后，PlayMode + 真实 `TcpClient` 连 `127.0.0.1:7788`）：23 条断言全过 —— `ping` / `list_commands` / 读改状态 / 参数越界 / 主动抛错 / 找不到服务返回 `false` / 中文参数 / `command_log` 最新 5 条 / 未知指令 / 非法 JSON / `world_spawn` 真的在场景里建出 3 个 Cube / `world_clear` 清空 / 控制台快捷键 F1 与 6 个快捷按钮确实来自 SO。导入后的场景零 missing script（Samples 导入保留 `.meta` 的 GUID）。
- 文档导出（与菜单同一条路径）：`Assets/CommandConsoleDocs/` 落盘总索引 + 6 个分类文件，分类顺序 = SO 里的 `builtin → session → player → world → demo`（未列出的 `probe` 排最后）。
