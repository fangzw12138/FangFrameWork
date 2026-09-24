# Command Console 示例

`com.fang.framework.commandconsole` 的示例：一个**贴近真实**的小游戏外壳（三层 Scope + 三个带状态的服务），
用它把本包从「扫描 → TCP → 执行 → 回包 → 控制台 → 文档」整条链路跑通。

**导入后可以直接改** —— 这里的东西都是你的工程资产，不是包内资产。

机制说明看包内 `Documentation~/调试.md`，用法看包根 `README.md`。

## 怎么用

1. 打开 `Demo.unity`，直接 Play。
2. 控制台会打印 `[DebugCommand] TCP 指令服务已启动: 127.0.0.1:7788` —— 端口来自示例 SO，不是默认的 7777。
3. 用任意 TCP 客户端连上去（一行一个 JSON，行分隔）：

```text
→ {"id":1,"cmd":"ping","args":[]}
← {"ok":true,"data":{"pong":true,"listening":true,"ticking":true,"port":7788,...},"id":1}

→ {"id":2,"cmd":"player_sethp","args":["42"]}
← {"ok":true,"data":{"hp":42,"maxHp":100,"level":1,"position":{"x":0.0,"y":0.0,"z":0.0}},"id":2}

→ {"id":3,"cmd":"world_spawn","args":["3"]}
← {"ok":true,"data":{"spawned":3,"total":3,"lastPosition":{...}},"id":3}
```

PowerShell 一把梭：

```powershell
$c = New-Object System.Net.Sockets.TcpClient('127.0.0.1', 7788)
$s = $c.GetStream()
$w = New-Object System.IO.StreamWriter($s); $w.AutoFlush = $true
$r = New-Object System.IO.StreamReader($s)
$w.WriteLine('{"id":1,"cmd":"ping","args":[]}')
$r.ReadLine()
$c.Close()
```

4. 游戏内控制台：**F1** 开关（示例 SO 把默认的 `` ` `` 改成了 F1），内置 3 个按钮 + 示例的 3 个按钮。
5. 编辑器侧：`Tools/Fang Framework/Fang Hub → 指令控制台`，或菜单 `Tools/Fang Framework/指令/导出指令清单`。

## 目录

```text
Demo/
├── Demo.unity                       演示场景：Main Camera / Directional Light / Ground / App / DemoHost
├── CommandConsoleDemoProject.asset  调试项目 SO（**故意用非默认值**，见下）
└── Scripts/
    ├── Fang.Framework.CommandConsole.Demo.asmdef
    ├── DemoHost.cs                  只驱动生命周期（OnInit / Tick / FixedTick / OnDispose）
    ├── AppScope.cs                  会话服务 + **调试指令服务**（装在系统层）
    ├── GameScope.cs                 中间层
    ├── SceneScope.cs                玩家 / 生成服务
    ├── DemoSessionService.cs        Service, ITickable：会话名 / tick 次数 / Time.timeScale
    ├── DemoPlayerService.cs         hp / maxHp / level / position
    ├── DemoSpawnService.cs          真的建 / 删 Cube
    ├── DemoUnregisteredService.cs   刻意不注册（演示「找不到服务返回 null」）
    └── DemoCommands.cs              17 条指令（session / player / world / demo）
```

## SO 为什么全用非默认值

端口 **7788**（默认 7777）、开关键 **F1**（默认 `` ` ``）、3 个快捷按钮、分类顺序 `builtin→session→player→world→demo`、
文档输出到 `Assets/CommandConsoleDocs` —— 这样**一眼就能看出 `Configure(SO)` 真的生效了**：
哪一项回落成默认值，就说明那份配置没套上。

## 指令清单

| 分类 | 指令 | 说明 |
| --- | --- | --- |
| `session` | `session_state` / `session_rename <name>` / `session_timescale <value>` | 读会话、改名字（中文）、改 `Time.timeScale`（0~10，越界报错） |
| `player` | `player_state` / `player_sethp <v>` / `player_damage <n>` / `player_heal <n>` / `player_level <v>` / `player_teleport <x,y,z>` | 读改状态、参数越界报错、`"x,y,z"` 网格坐标解析 |
| `world` | `world_spawn <count>` / `world_clear` / `world_count` | **主线程**建 / 删 GameObject |
| `demo` | `demo_echo <text>` / `demo_throw` / `demo_bad_int <v>` / `demo_unregistered_service` / `demo_vector3_pitfall` | 刻意失败路径 |
| `builtin` | `ping` / `list_commands` / `command_log` | 包内置 |

编辑器里还会多出几条 `probe_*` —— 那是本包 EditMode 测试用的探针，只在编辑器里存在，打包后没有。

## 两个真实坑（示例就是拿来踩它们的）

**1. 向下兜底只在「宿主 Scope 的子树」里找**

示例把 `DebugCommandService` 装在**系统层**（`AppScope`），而玩家 / 生成服务在**场景层**（`SceneScope`）——
`ctx.GetService<T>()` 先沿 `Scope.Parent` 向上找，找不到再从当前 Scope 起**向下兜底**，所以从 App 层出发
能摸到 Scene 层的服务（`player_*` / `world_*` 就是这么工作的）。

边界在于：向下兜底只走**当前 Scope 的子树**（`Scope.Children`）。如果场景里另外独立摆了一个根 Scope
（不在 App 层的子树里），指令就走不到它 —— 这时要么把它挂进这棵树，要么把指令服务挪到能看见它的那一层。
同一类型在子树里命中多个时取第一个，并打一条警告。

**2. 返回值里别直接放 `UnityEngine.Vector3`**

Newtonsoft 默认的 `ReferenceLoopHandling.Error` 会把 `Vector3.normalized` 这条链判成自引用，
整条指令以 `Self referencing loop detected for property 'normalized'` 失败。
`demo_vector3_pitfall` 就是这条坑的可执行复现；示例里其它指令都走 `Flat(...)` 摊平成 `{x,y,z}`。
要根治得在包侧把序列化器换成 `ReferenceLoopHandling.Ignore`，或注册一个 `Vector3` 转换器。

## 注意

- 示例脚本整段包在 `#if UNITY_EDITOR || DEVELOPMENT_BUILD` 里 —— 包内类型在 Release 会被剥空，不包的话消费者的 Release 构建会编译失败。
- 包升级后这份示例**不会自动更新**（Samples 导入是复制一份到 `Assets/`）。想拿新版：删掉这份，再从 FangHub 页点一次「导入示例」。
