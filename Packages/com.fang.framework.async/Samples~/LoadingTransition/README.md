# Loading Transition 样例

`com.fang.framework.async` 的整棵异步族最小可运行范式：启动 → 标题 → 关卡 → 回标题 → 关卡，四层异步 `AsyncScope` 配一张加载画面。

## 一、结构

```text
LoadingTransitionHost          宿主（本样例唯一写 Unity 消息方法的类型）
  Canvas                       代码搭建：遮罩 + 进度条 + 两个按钮
    LoadingTransitionOverlay   LoadingTransitionVisual（只订阅事件，零轮询）
  App                          AppScope（永久容器）
    Services                   InitDriver + 两个模拟加载步骤
    TitleScope                 在启动轮里创建（属于 App 的初始化）
      GameScope                点「进入游戏」后创建
        Services
        SceneScope
```

| 类型 | 角色 |
| --- | --- |
| `AppScope` | 永久容器。切换入口 `EnterGame()` / `ReturnToTitle()` = 摘旧子 + `CreateChildScope` + **显式 `await child.InitAsync()`** |
| `TitleScope` / `GameScope` / `SceneScope` | 每轮的子树。`GameScope` 里本层服务与子 scope 用 `UniTask.WhenAll` 并行 |
| `InitDriver` | `AsyncService` + 核心 `ITickable`：每帧把**自己这一层**的「服务 + 子 scope」等权折算成 `InitProgress` 报给本层 |
| `LoadingSteps` | 9 个基于 `await UniTask.Yield` 的模拟加载步骤，负责产生平滑进度 |
| `LoadingTransitionVisual` | 加载画面：只订阅 `InitStarted` / `InitProgressChanged` / `InitCompleted`，只在这些事件里改 UI |

## 二、启动算一轮，之后的每次切换各算一轮

- **启动（App）算一轮**：`AppScope.OnInitAsync()` 建出第一个 `TitleScope` 并 `await`，所以 App 的 `InitProgress` 会把子树一起折算进去 —— 进度 0→100 一次走完、遮罩只出一次、中途不回退。
- **之后的切换各算一轮**：`AppScope` 在 `await child.InitAsync()` **之前**触发 `RoundPrepared`，宿主借此把加载画面切到新根上。必须在启动之前切 —— 否则订阅挂上时 `IsInitializing` 已经是 true、首帧进度就丢了。
- 两条级联口径是刻意不对称的：**初始化不级联**（父在自己的钩子里显式 `await` 子）；**释放自动级联**（父自毁会连子物体一起带走，所以必须先逐个 `await` 完子树）。

## 三、这里**故意**用 C# 原生 `event`，不是事件总线

- 视觉层订阅的是**它所呈现的那个对象**（scope）的生命周期事件 —— 这正是核心 `表现层规范.md` §二的口径：「状态走 `Data`（视觉脚本自己读），一次性命令走事件……**C# 原生 `event` 就够，框架不提供任何新机制**」。
- `com.fang.framework.eventbus` 的定位是**跨对象的一次性命令**：发送方**不认识**接收方（例如伤害结算 → 音效服务 + 视觉 + 成就各自订阅）。加载画面与它呈现的 scope 是一对一的装配关系，中间插一层总线只会让「谁在看这个加载画面」更难查。
- 本包**刻意不依赖** eventbus（`package.json` 的 `dependencies` 只有 UniTask 与核心包），样例程序集也不引用 `Fang.Framework.EventBus` —— 装异步包不该被迫再装一个包。
- 真正该走总线的场合：加载完成要通知**接收方未知或多方**的系统（存档、成就、音乐）。那时用 `Scope.GetService<EventBus>().Publish(...)`。

## 四、进度是「状态」，事件只是通知

`InitProgress` 按核心口径属于状态，所以 `Watch(root)` 会直接读一次当前值兜底，事件只负责变化通知。另外框架在初始化完成时是**直接置 1、不发 `InitProgressChanged`**，因此视觉层在 `InitCompleted` 里补一次 `SetProgress(1f)`。

## 五、运行

1. 用 Package Manager 从 `com.fang.framework.async` 导入本样例（需要 UniTask 与核心包；FangHub 的「框架与扩展」页会自动补 OpenUPM 注册表）。
2. 打开 `LoadingTransition.unity`，进 Play。
3. Console 会打印整条流程（统一 `LoadingTransition:` 前缀）：每轮 `overlay shown, watching X` → `X init started` → 25/50/75% → `X init completed` → `overlay hidden`。
4. 遮罩显示期间挡住输入（`CanvasGroup.blocksRaycasts`），所以加载中连点按钮不会叠轮。

## 六、照抄时注意的两个「顺序」坑

1. **过期根的事件必须丢掉。** 框架的顺序是「先置完成 → `await` 方同步恢复 → **之后**才发 `InitCompleted`」。所以「停看旧根、改看新根」的视觉层必须判断事件里的根**是否还是当前根**；否则旧根迟到的 `InitCompleted` 会把新根的进度条顶成 100%、并把遮罩提前淡出（本样例第一版就踩了，用 `IsCurrent(root)` 守住的）。
2. **切换入口要先摘旧子、再建新子。** `EnterGame()` / `ReturnToTitle()` 把两种子类型都摘一遍，否则「已经在标题时再点返回标题」会留下两个子 scope。
