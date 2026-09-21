# Fang Framework

零第三方依赖的 Unity 游戏框架核心定义层。

## 定位

核心包只回答一个问题：**Data 是什么**。

- 身份：`Data<TConfig>.RuntimeId`
- 配置对应关系：`Data<TConfig>.Config`
- 由 config 完全重建：`Data<TConfig>` 只有带参构造，不存在无 config 的中间态

核心包**不回答**「Data 怎么落盘」。序列化、反序列化、存档驱动、配置解析层全部属于扩展包，见 `Documentation~/架构总览.md`。

## 兼容性

| 项 | 范围 |
| --- | --- |
| 引擎 | Unity 2022.3+ / 团结引擎 2022.3+ |
| 语言 | C# 9（不使用文件级 namespace、`global using`、`record struct`、`required`、原始字符串字面量） |
| 依赖 | 无。`package.json` 的 `dependencies` 为空对象 |
| 反射 | 零反射。服务一律 `new T()`，依赖一律显式 `Scope.GetService<T>()` |
| 线程 | 主线程。`Scope` 不保证线程安全 |

## 安装

内嵌包：把 `com.fang.framework` 整个目录放到工程的 `Packages/` 下。

`Runtime/Fang.Framework.asmdef` 的 `autoReferenced` 为 `true`，因此 `Assets/` 下的预定义程序集（`Assembly-CSharp`）可直接 `using Fang.Framework;`，无需手工添加程序集引用。

## 内容

| 类型 | 位置 | 形态 | 说明 |
| --- | --- | --- | --- |
| `ConfigDataSo` | `Runtime/Core/Domain` | ScriptableObject | 只读配置资产基类 |
| `Data<TConfig>` | `Runtime/Core/Domain` | 纯 C# | 运行时数据唯一真相来源 |
| `Controller<TConfig, TData>` | `Runtime/Core/Domain` | 纯 C# | 逻辑层 |
| `WorldObject<TController, TData, TConfig>` | `Runtime/Core/Domain` | MonoBehaviour | 表现层 |
| `Service` | `Runtime/Core` | 纯 C# | Scope 管理的长期能力基类 |
| `Scope` | `Runtime/Core` | 纯 C# | 服务表 / 父子树 / 生命周期驱动 / 释放 |
| `ILifecycle` / `ITickable` / `IFixedTickable` | `Runtime/Core` | 接口 | 初始化与销毁、每帧、固定帧 |

依赖方向单向：`WorldObject → Controller → Data`。`Controller` 不认识 `WorldObject`。

**框架核心零 MonoBehaviour，`WorldObject` 是唯一例外。** 框架不提供宿主 MonoBehaviour —— 使用方自己写一个，调 `Scope.Tick` / `Scope.FixedTick` / `Scope.Dispose`。

## 文档

- `Documentation~/架构总览.md`
- `Documentation~/快速开始.md`
- `Documentation~/目录规范.md`
- `Documentation~/编码规范.md`
- `Documentation~/AI约束.md`

`Documentation~` 目录名带 `~` 后缀，不参与 Unity 导入。

## 许可证

MIT，见 `LICENSE.md`。
