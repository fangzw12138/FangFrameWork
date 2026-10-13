# Changelog

## [0.1.2] - 2026-10-13

### Added

- EditMode 测试 6 项（`AsyncProgressTests`）：等权平均、成员完成后本层自动跟随、子 scope 进度向父层传播、初始化后冻结在 1、成员移除后重算、完成会抛进度事件。

### Changed

- `AsyncLifecycleBehaviour`：完成时不再直接写 `InitProgress = 1`，改为走 `ReportInitProgress(1f)` —— 完成也会抛 `InitProgressChanged`，订阅方不必再在 `InitCompleted` 里自己补 100%（旧版把它列为「坑 §7.3」）。
- `AsyncScope`：**自己聚合进度**。本层 `InitProgress` = 「本层服务 + 子 scope」的等权平均；成员一注册就纳入统计、成员一变化就重算（事件驱动，零轮询）；`IsInitialized` 之后冻结在 1。业务方不再需要往每一层塞一个「每帧折算进度」的服务。
  - 已知边界：只实现 `IAsyncLifecycle`、**没有继承 `AsyncLifecycleBehaviour`** 的服务（`异步快速开始.md` §3.2 那条路）没有进度事件，它的进度只在同一层有别的成员变化时才被重新折算 —— 要实时跟随就继承 `AsyncLifecycleBehaviour`。
  - 权重固定为等权；串行 / 并行 / 加权的取舍仍由业务方在自己的钩子里决定。

### Removed

- **Samples~/LoadingTransition**：删掉 `InitDriver`（`AsyncService` + `ITickable`，每帧折算进度）—— 框架现在自己做这件事，样例四层 Scope 的装配不再含任何进度相关代码。

## [0.1.1] - 2026-10-02

### Fixed

- `AsyncScope.DestroySelf()`：自毁前先判 `this == null`。Unity 可能已在自毁链等待期间销毁了本对象（宿主 scope 先被销毁，或编辑器停止 Play），此时访问 `gameObject` 会抛 `MissingReferenceException`；该异常由 fire-and-forget 的释放链吞下，最终以 UniTask 未观察异常（`UniTaskScheduler.PublishUnobservedTaskException`）的形式在控制台爆出。现在这种情况下直接跳过自毁。

## [0.1.0] - 2026-10-02

### Added

- **Runtime**（程序集 `Fang.Framework.Async`，引用 `Fang.Framework` / `UniTask`）：
  - `IAsyncLifecycle`：两个入口（`UniTask InitAsync()` / `UniTask DisposeAsync()`）+ 三个状态（`IsInitialized` / `IsInitializing` / `InitProgress`）；`IAsyncInjectable`（可选，实现了才会拿到 `AsyncScope`）。
  - `AsyncLifecycleBehaviour`：状态机唯一实现 —— 幂等入口（先挂句柄再启动）、完成源、进度（`ReportInitProgress`）、3 个事件（`InitStarted` / `InitProgressChanged` / `InitCompleted`，只报本层）、钩子转发、释放编排（子类钩子 → 框架步骤）。
  - `AsyncScope`：父子 / 服务 / 注入 / 向上查找 / `Tick`·`FixedTick` 递归（异步族自己一套）+ `RemoveChild` / `RemoveService`（返回 `UniTask`）；释放**自动级联**（先子树、后本层服务、最后自毁）。
  - `AsyncService` / `AsyncController<TConfig, TData>`（复用核心的 `Data<TConfig>` / `ConfigDataSo` / `ITickable`）。控制器的数据**并进入口**：`public UniTask InitAsync(TData data)` 是唯一公开入口，`Data` 只写一次。
- **Samples~/LoadingTransition**：四层异步 scope（App → 标题 / 关卡 → 场景）配一张加载画面的完整样例，含 `InitDriver` 折算子树进度、轮次切换（`EnterGame` / `ReturnToTitle`）与「过期根事件守卫」。
- **Documentation~/异步快速开始.md**：两族关系与不可混用、接口与基类、转换对照、语义表、`AsyncScope` 机制表、两条刻意不对称的级联口径、四个坑（释放途中取消未完成的初始化 / 过期根事件守卫 / 置 1 不发事件 / 移除要走父侧 API）、与事件总线的边界、安装、样例指路。
- EditMode 测试 54 项：契约（22）、状态机（13）、层级与服务（20）。
- `README.md` / `CHANGELOG.md` / `LICENSE.md`。

### Notes

- `package.json` 的 `dependencies` **只声明** `com.cysharp.unitask`（OpenUPM 注册表能解析），**不声明核心包** —— 核心包按 git URL + tag 分发、不在任何 registry 里，写进去会解析失败且会把版本钉死。对核心的依赖靠 asmdef 的 `references` + FangHub「框架与扩展」页的前置检查 + `README.md` 声明落地（核心 `扩展包分发.md` 事实 2）。
- 与核心同步族的 `Scope` / `Service` / `Controller` **不继承、不混用**：一棵 scope 树要么整棵同步族、要么整棵异步族。核心包不因本包存在而新增任何依赖（仍零第三方）。
