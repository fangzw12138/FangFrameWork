# Fang Framework · Command Console

`com.fang.framework.commandconsole` —— Fang Framework 的**指令层**：一条静态方法 + 一个特性就是一个调试指令，外部经 TCP 调用，游戏内与编辑器各有一个控制台。

```csharp
[DebugCommand("hero_set_hp", "设置英雄血量 <value>", "hero")]
public static object SetHp(DebugCommandContext ctx, DebugCommandArgs args)
{
    var hero = ctx.GetService<HeroService>();
    if (hero == null) throw new DebugCommandException("当前没有游戏会话");
    hero.SetHp(args.GetInt(0, "value"));
    return new { hp = hero.Hp };
}
```

外部用行分隔 JSON 调用（AI / 测试脚本 / 任何 TCP 客户端）：

```text
→ {"id":1,"cmd":"hero_set_hp","args":["42"]}
← {"id":1,"ok":true,"data":{"hp":42}}
← {"id":1,"ok":false,"error":"当前没有游戏会话"}
```

## 分层

```text
Command/    纯逻辑：特性 / 参数 / 扫描器 / 注册表 / 上下文 / 默认值   ← 可单测，不碰场景
Transport/  纯逻辑 + IO：协议解析/格式化、TCP 服务端（后台入队 + 主线程 Drain）
Host/       Unity 宿主：DebugCommandService（Service, ITickable）+ 内置指令 + 游戏内控制台
Editor/     编辑器侧：指令控制台窗口、文档生成、校验、FangHub「指令控制台」页
```

- **扫描器与注册表分离**：运行时（`DebugCommandService`）与编辑器文档生成器调**同一个** `DebugCommandScanner`，两侧清单永远一致。
- **执行在主线程**：后台线程只把收到的行入队，主线程 `Drain()` 逐条执行并写回（指令内可以放心碰 UnityEngine API）。
- **主线程由 `Scope.Tick` 驱动**：本包不写 Unity 消息方法，符合核心包「生命周期由使用方显式驱动」的约定。**装了没人 tick ⇒ TCP 不响应**，所以 FangHub 页与编辑器控制台都会显示「是否在 tick / 是否在监听」。

## 依赖

| 依赖 | 落地形式 | 说明 |
| --- | --- | --- |
| `com.fang.framework` | asmdef 引用 `Fang.Framework` | 核心包（`Scope` / `Service` / `ITickable` / `ConfigDataSo`），必须 |
| `com.unity.nuget.newtonsoft-json` | `package.json` 的 `dependencies` | 3.2.2。TCP 协议解析与「任意返回值 → JSON」都要它（`JsonUtility` 序列化不了匿名对象） |

核心包是 git URL 安装，写不进 `dependencies`，所以依赖关系靠 asmdef 引用 + 核心包「框架与扩展」页的前置检查 + 本文档声明（与既有扩展包一致）。

## 安装

**方式一：FangHub 的「框架与扩展」页**（推荐）

`Tools/Fang Framework/Fang Hub` → 侧栏点「框架与扩展」→ 点「刷新」→ 点「安装」。

**方式二：手写 `Packages/manifest.json`**

```json
{
  "dependencies": {
    "com.fang.framework": "https://github.com/fangzw12138/FangFrameWork.git?path=/Packages/com.fang.framework#com.fang.framework/v0.5.1",
    "com.fang.framework.commandconsole": "https://github.com/fangzw12138/FangFrameWork.git?path=/Packages/com.fang.framework.commandconsole#com.fang.framework.commandconsole/v0.1.0"
  }
}
```

需要本机有 git CLI。

## 包结构

```text
Packages/com.fang.framework.commandconsole/
├── package.json
├── README.md / CHANGELOG.md / LICENSE.md
├── Documentation~/                     目录名带 ~ 后缀，不参与 Unity 导入
│   └── 调试.md                          设计理由：分层、SO 管什么、怎么加传输 / 换上下文
├── Runtime/
│   ├── Config/                         DebugProjectSo / DebugQuickButton / DebugSettings（不加编译守卫）
│   ├── Command/                        特性 / 参数 / 异常 / 条目 / 扫描器 / 注册表 / 上下文 / 默认值
│   ├── Transport/                      协议 + TCP 服务端
│   ├── Host/                           服务 + 内置指令 + 游戏内控制台
│   ├── Fang.Framework.CommandConsole.asmdef     全平台，autoReferenced
│   └── AssemblyInfo.cs
├── Editor/                             控制台窗口 / 文档生成 / 校验 / 向导 / FangHub 页
└── Tests/Editor/                       EditMode 测试
```

命名空间是 `Fang.Framework.CommandConsole`。**不要**把命名空间改成 `Fang.Framework.Debug`：只要有任何一个命名空间叫它，`Debug.Log(...)` 在该命名空间**以及它的兄弟命名空间**里都会编译失败（`CS0234`，已实测）——简单名 `Debug` 会先解析到那个命名空间成员、而不是 `UnityEngine.Debug`。程序集名不受影响（命名空间查找不看程序集名）。

## 用法

### 1. 加一条指令

写一个 `static` 方法，参数必须是 `(DebugCommandContext, DebugCommandArgs)`，返回 `object`（`null` 也行），贴上 `[DebugCommand(name, description, category)]`。方法可以是 `public` 也可以是 `private`，类可以是 `static` 也可以是普通类 —— 扫描器只看方法签名。

签名不符的方法会被**跳过并打 `LogWarning`**（带上来源），不会等到调用时才炸。

### 2. 起服务

`DebugCommandService` 是核心包的 `Service`，挂在某个 `Scope` 上：

```csharp
var service = scope.AddService<DebugCommandService>();   // 未 Configure 时用内置默认值
service.Configure(debugProjectSo);                       // 可选：套用项目配置
// 主线程每帧：scope.Tick(Time.deltaTime)
```

`Configure` 是幂等的；已经启动时改端口会按需重开监听。

**装在哪一层？** `ctx.GetService<T>()` 先沿 `Scope.Parent` **向上**找，找不到再从当前 Scope 起**向下兜底**
（自身 → `Children`… 广度优先）。所以装在系统层（浅层）也能摸到场景层的服务；只有「场景里另外独立摆放的根 Scope」
不在它的子树里、走不到。同一类型在子树里命中多个时取**第一个**并打一条警告。

### 3. 项目配置（`DebugProjectSo`）

`Create > Fang Framework/指令/项目配置`，或 FangHub「指令控制台」页 → 菜单栏 `调试项目 ▾ → 新建调试项目配置 SO…`（引导窗口）。

管三件事：**运行参数**（自动启动 / 监听地址 / 端口 / 控制台开关与快捷键 / 快捷按钮 / 扫描范围）、**分类顺序**、**文档输出**（目录 / 文件名 / 是否按分类拆文件）。

「分类顺序」留空 = 内置指令分类 `builtin` 在最前、其余按名称排序（`builtin` 是包内置那三条 `ping` / `list_commands` / `command_log` 的分类；忘标分类的指令归 `system`，两者刻意分开）。

**每个字段留空都回落内置默认值**（集中在 `DebugCommandDefaults`，由 `DebugSettings` 一处解析），所以没有 SO 也完全可用。

「扫描范围」留空 = 只扫**引用了本包**的程序集。这不是偷懒：写 `[DebugCommand]` 必须能解析到本包的类型，所以使用者程序集必然直接引用本包；本机实测全量扫 250 个程序集要 3291 ms，按引用过滤是 0 ms，扫到的指令完全一致。要全量扫就把程序集名前缀填进去。

### 4. 编辑器控制台

`Tools/Fang Framework/指令/打开控制台` —— PlayMode 下进程内直接执行，带历史下拉、输出区、调用日志与状态行（不经 TCP）。

### 5. 文档生成

`Tools/Fang Framework/指令/导出指令清单` —— 与运行时**同一扫描器**，按 SO 的分类顺序与输出路径落盘。解决「AI 制定测试计划时游戏没跑、不知道有哪些指令」的循环依赖。

### 6. 游戏内控制台

`DebugCommandService` 自己建一个 `DebugCommandConsole`（IMGUI），默认 `` ` `` 开关；快捷键与快捷按钮来自 SO。要换 Input System，继承覆写 `DebugCommandConsole.IsTogglePressed()` 即可。

## 示例

包内 `Samples~/Demo` 是一个**贴近真实**的示例：三层 Scope（App / Game / Scene）+ 三个带可变状态的服务 + 五个分类的指令 + 项目 SO + 场景，用来端到端验证本包。

FangHub「指令控制台」页 → 菜单栏「导入示例」→ 打开 `Demo.unity` → PlayMode → 用任意 TCP 客户端连 `127.0.0.1:7788`。示例 SO 刻意用了**非默认值**（端口 7788、开关快捷键 F1、3 个快捷按钮、分类顺序），这样一眼就能看出配置真的生效了。细节见示例自己的 `README.md`。

## 编译范围

命令层全部包在 `#if UNITY_EDITOR || DEVELOPMENT_BUILD` 里 —— 发布包（Release）里这段代码被剥空，零风险。

`DebugProjectSo` / `DebugQuickButton` **不加守卫**（它们是数据资产，Release 里也能存在）。

已知限制：Release 包被剥空；放在 Editor 专用程序集里的指令只在编辑器侧可见（打包后没有）。

## 状态

**已落地**：包骨架、命令核心、上下文、传输与协议、宿主与控制台、项目 SO 与向导、编辑器侧（控制台 / 文档 / 校验 / FangHub 页）、EditMode 测试、示例（`Samples~/Demo`）。

## 许可证

MIT，见 `LICENSE.md`。
