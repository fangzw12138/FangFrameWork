# Changelog

## [0.1.0] - 2026-09-21

### Added

- 包骨架与程序集定义：`Fang.Framework`、`Fang.Framework.Editor`、`Fang.Framework.Tests`、`Fang.Framework.Editor.Tests`。
- Domain 四件套：`ConfigDataSo`、`Data<TConfig>`、`Controller<TConfig, TData>`、`WorldObject<TController, TData, TConfig>`。
- `Service` 语义基类。
- `Scope` 容器：注册（实现 / 工厂 / 实例）、构建、解析、构造函数注入、层级解析与遮蔽、循环依赖检测、逆序释放。
- `IInitializable`、`IInjectable` 接口与四个 `Scope` 异常类型。
- EditMode 测试：契约测试、注册测试、层级测试、生命周期测试。
- `Documentation~/` 五份文档。
