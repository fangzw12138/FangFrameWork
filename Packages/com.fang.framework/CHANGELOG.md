# Changelog

## [0.3.0] - 2026-09-21

### Changed

- **Core 回归 mono**：`Scope` / `Service` / `Controller<TConfig, TData>` 从纯 C# 改回 MonoBehaviour；`Data<TConfig>` 仍是纯 C#，`ConfigDataSo` 仍是 ScriptableObject。
- **`Scope` 实现 `ILifecycle`**：生命周期入口改为 `OnInit()` / `OnDispose()`（都是 `virtual`，取代原 `Dispose()`）；`IsDisposed` 删除，换成 `IsInitialized`（`OnInit()` 置 `true`，`OnDispose()` 置 `false`，同时充当 `OnDispose` 的幂等守卫）。
- **`AddService<T>()` 改为建物体 + `AddComponent<T>()`**：服务物体挂在 Scope 的子物体 `Services` 下（懒建）；约束由 `where T : Service, new()` 放宽为 `where T : Service`。
- **`CreateChildScope<T>()` 改为建子物体 + `AddComponent<T>()`**：子 Scope 物体挂在父 Scope 物体下，解析链仍是显式 `_parent`。
- **框架不再销毁任何 GameObject**：`RemoveService<T>()` 与 `OnDispose()` 只做注销与钩子回调，物体留给使用方或 Unity 层级回收。
- **`Service` 不定义任何 Unity 消息方法**：不做自注册，业务钩子只走 `OnInit` / `OnDispose` / `OnTick` / `OnFixedTick`。
- **`Scope` / `Service` / `Controller` 的 `OnInit` 由使用方显式驱动**：框架仍不提供宿主 MonoBehaviour，也不写 `Awake` / `Update` / `OnDestroy`。
- 测试替身改为 MonoBehaviour（`ProbeScope.Create<TScope>()` 工厂 + `DestroyAll()` 回收），并补上「服务物体挂在 `Services` 下」「框架不销毁物体」「未初始化时 `OnDispose` 是 no-op」「不声明 Unity 消息方法」等契约。
- 文档同步：`Documentation~/` 五份、`README.md`。

## [0.2.0] - 2026-09-21

### Changed

- **`Scope` 重写为纯 C# 抽象类**：删除 `Build` / `Register` / `Resolve` / `TryResolve` / `Inject` / `InjectHierarchy` / `CreateChild` 与就绪门槛，改为 `AddService<T>()` / `CreateChildScope<T>()` / `GetService<T>()` / `RemoveService<T>()` / `Tick` / `FixedTick` / `Dispose`。
- **`Service` 改为纯 C#**，实现 `IInjectable` + `ILifecycle`，持有注入好的 `protected Scope`；`OnInit` / `OnDispose` 为空实现。
- **`Controller<TConfig, TData>` 改为纯 C#**（不再是 MonoBehaviour），实现 `IInjectable` + `ILifecycle`，持有 `protected Scope`；`WorldObject` 仍是唯一的 MonoBehaviour。
- **生命周期改为三个裸接口**：`ILifecycle` / `ITickable` / `IFixedTickable`，取代 `IInitializable`；`Configure()` 删除。
- **父子级改为树**：父 `Dispose()` 递归释放全部子 Scope，子反向记 `_parent`；`GetService<T>()` 沿父链向上解析。
- **零反射**：删除构造函数缓存与反射构造，服务一律 `new T()`（`where T : Service, new()`）。
- **删除自定义异常类型**（`ScopeException` / `ScopeResolveException` / `ScopeCircularDependencyException` / `ScopeStateException`），解析失败改为抛 `InvalidOperationException`。
- 包结构：`Runtime/` 下只留 `Core/`，四件套领域收进 `Core/Domain/`；取消平级的 `Scope/`、`Service/`、`Domain/`。
- 测试全部按新 API 重写，并补上 `Scope` 成员面与可访问性的反射契约。

## [0.1.0] - 2026-09-21

### Added

- 包骨架与程序集定义：`Fang.Framework`、`Fang.Framework.Editor`、`Fang.Framework.Tests`、`Fang.Framework.Editor.Tests`。
- Domain 四件套：`ConfigDataSo`、`Data<TConfig>`、`Controller<TConfig, TData>`、`WorldObject<TController, TData, TConfig>`。
- `Service` 语义基类。
- `Scope` 容器：注册（实现 / 工厂 / 实例）、构建、解析、构造函数注入、层级解析与遮蔽、循环依赖检测、逆序释放。
- `IInitializable`、`IInjectable` 接口与四个 `Scope` 异常类型。
- EditMode 测试：契约测试、注册测试、层级测试、生命周期测试。
- `Documentation~/` 五份文档。
