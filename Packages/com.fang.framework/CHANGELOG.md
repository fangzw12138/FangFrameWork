# Changelog

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
