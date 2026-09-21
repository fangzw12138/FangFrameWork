# Changelog

## [0.5.0] - 2026-09-21

### Removed

- **`WorldObject<TController, TData, TConfig>` 删除**（破坏性变更）：核心包不再提供表现层基类。载体职责在 0.3.0 已随 `Controller` 改回 MonoBehaviour 而失效，之后 WO 只剩「持有 `Controller` + 转发只读 `Data` / `Config`」，而 `Controller.Data` 本来就是 `public` 只读属性、`Inject(Scope)` 对表现层没有调用方 —— 零推测性 API 下没有保留理由。核心包 Domain 从四件套变三件套：`ConfigDataSo` / `Data<TConfig>` / `Controller<TConfig, TData>`。
- 契约测试里的全部 WO 断言：`TestWorldObject`、`WorldObject_constraints_are_correct`、`WorldObject_routes_Data_and_Config_without_backing_fields`、`WorldObject_Initialize_rejects_null`、`Controller_has_no_reference_to_WorldObject`，以及辅助方法 `AssertNoWorldObjectReference` / `IsWorldObjectType`。
- 示例里的空类 `HeroWorldObject`。

### Added

- `Documentation~/表现层规范.md`：删掉 WO 之后表现层怎么写 —— 为什么没有基类、表现层三件套（视觉脚本 `XxxVisual` / 视图句柄 `XxxIWO` / 挂载点约定）、生命周期与销毁（管理服务创建 + 登记 + 释放、从不 `Destroy`；销毁归使用方）、命名与目录、老工程实例、禁止事项。

### Changed

- 示例 `HeroService` → `HeroManagerService`：职责为「创建 + 登记 + 释放」，不 `Destroy`；创建入口 `CreateHero(HeroConfigDataSo)` 由 `SceneScope` 调用（配置由场景层传入）。
- 示例新增 `HeroVisual`（视觉脚本样板），由 `HeroController.OnInit()` 自己 `AddComponent` 并 `Initialize(this)`；`HeroController` 补 `OnInit()` / `OnDispose()`。
- `Controller` 双泛型理由澄清：`TData : Data<TConfig>` 这条约束本身就必须两个参数才表达得出来，与表现层无关 —— `架构总览.md` §四 原文把它归因于 WO 的 `where` 约束，是错的。
- 文档去 WO：`架构总览.md`（分层表、依赖方向整节重写、§四、Scope 语义表）、`快速开始.md`、`编码规范.md`（5.2 / 5.3 / 5.4 / 5.5、MonoBehaviour 列表）、`目录规范.md`、`AI约束.md`、`README.md`、`package.json` 示例描述、eventbus `事件总线.md`。
- `AI约束` 第 8 条依赖方向改为 `Controller → Data`（`Controller` 不得引用表现层）；第 12 条归属表去 WO；第 13 条补「Unity 接触面落在视觉脚本上」。
- `编码规范` 与 `AI约束` 的注释规则放宽为「允许一句话的职责 / 非标准写法备注，禁止理由型长注释」，与示例里的两条备注对齐。
- 核心包版本 `0.4.3` → `0.5.0`。

## [0.4.3] - 2026-09-21

### Fixed

- **装 / 卸后按钮不再变灰**：`RebuildRows()` 原来是先 `_list.Rebuild()` 再 `UpdateCoreLabel()`，于是绑定按钮时读到的还是上一次的 `_coreInstalled`（false），装完后「卸载」按钮是灰的，得再点一次刷新才恢复。改为先算 `_coreInstalled` 再重建列表，并补 `RefreshItems()` 强制按最新状态重绑。
- 核心包版本 `0.4.2` → `0.4.3`。

## [0.4.2] - 2026-09-21

### Changed

- **窗口顶部不再暴露索引地址**：索引是仓库根 `packages.json`，属于实现细节，默认地址写死在代码里、打开自动拉取。地址框收进折叠的「索引设置（高级）」，只有离线验证才需要展开改成 `file://`，旁边给「恢复默认」。
- **状态行不再被安装器覆盖**：`ExtensionPackageInstaller.RefreshInstalledPackages` 成功后不再写「已读取本地包状态。」，改为只在装 / 卸完成时写「已安装 x。/ 已卸载 x。」，所以索引拉完的「共 N 个扩展包。」能留在状态行上。
- 索引地址为空时自动回落到默认地址，不再要求用户填写。
- 核心包版本 `0.4.1` → `0.4.2`。

## [0.4.1] - 2026-09-21

### Fixed

- **安装窗口重做**：索引地址输入框不再被挤成一条缝（补 `flexGrow`）；空列表不再暴露 UI Toolkit 的英文 `List is empty`，改为中文提示；新增表头行（包 / 包标识 / 索引 / 本地 / 操作），行内改成「显示名 + 包标识」两列；内嵌包不再显示一个空按钮（改 `visibility` 保留列宽）。
- **打开窗口即拉索引**：首次打开自动拉取索引，不必先手点「刷新索引」。
- 核心包状态标签改用常量包名，不再依赖索引里的 `core.name` —— 之前索引未拉取时会误报「核心包：未装」。

### Changed

- 核心包版本 `0.4.0` → `0.4.1`。

## [0.4.0] - 2026-09-21

### Added

- **扩展包分发机制**：扩展包与核心包同仓，走 git URL + `?path=` + 每包独立 tag；索引文件放仓库根 `packages.json`，用 Unity 自带 `JsonUtility` 解析（零第三方依赖）。
- **`ExtensionPackageIndex`**（`Editor`，纯逻辑）：索引 DTO、`Parse` / `NormalizeRepository` / `BuildInstallUrl` / `IsNewer` / `ResolveState`。
- **`ExtensionPackageInstaller`**（`Editor`，纯逻辑）：`Client.List` / `Add` / `Remove` 的 `EditorApplication.update` 轮询状态机，暴露 `IsBusy` / `Status` / `Changed`。
- **`ExtensionPackagesWindow`**（`Editor`）：UI Toolkit 纯 C# 建树（不引 UXML / USS），菜单 `Tools/Fang Framework/Extension Packages`；索引地址存 `EditorPrefs`，`https://` 走 `UnityWebRequest`、`file://` 读本地文件（离线验证用）；核心包未装时禁用全部安装按钮。
- EditMode 测试 `Tests/Editor/ExtensionPackageIndexTests.cs`：解析 / 仓库地址归一 / 安装 URL 拼装 / 语义化版本比较 / 状态判定。
- `Documentation~/扩展包分发.md`；`目录规范.md` / `架构总览.md` / `快速开始.md` / `README.md` 同步。

### Changed

- 核心包版本 `0.3.0` → `0.4.0`。
- `package.json` 的 `dependencies` 仍为空对象；核心包不引用任何扩展包。

### Notes

- 扩展包**不由核心包编译**：`Fang.Framework.Editor` 只做索引与安装编排，不引用任何扩展包代码。
- 第一个扩展包 `com.fang.framework.eventbus` 0.1.0（类型安全发布订阅）与本版本同批发布，见 `Packages/com.fang.framework.eventbus/CHANGELOG.md`。

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
