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
| 反射 | 零反射。服务一律 `AddComponent<T>()`，依赖一律显式 `Scope.GetService<T>()` |
| 线程 | 主线程。`Scope` 不保证线程安全 |

## 安装

内嵌包：把 `com.fang.framework` 整个目录放到工程的 `Packages/` 下。

`Runtime/Fang.Framework.asmdef` 的 `autoReferenced` 为 `true`，因此 `Assets/` 下的预定义程序集（`Assembly-CSharp`）可直接 `using Fang.Framework;`，无需手工添加程序集引用。

从 git URL 安装：

```json
{
  "dependencies": {
    "com.fang.framework": "https://github.com/fangzw12138/FangFrameWork.git?path=/Packages/com.fang.framework#com.fang.framework/v0.5.0"
  }
}
```

需要本机 git CLI。Unity **不检测 git 依赖更新**，升级要改 URL 里的 tag。

## 扩展包

扩展包与核心包同仓（`Packages/com.fang.framework.xxx/`），走 git URL + `?path=` + 每包独立 tag 分发。**依赖只有单向：扩展包引用核心包，核心包永不引用扩展包。**

| 扩展包 | 内容 | 状态 |
| --- | --- | --- |
| `com.fang.framework.eventbus` | 类型安全发布订阅 | 0.1.0 |
| `com.fang.framework.objectpool` | 对象池 | 待做 |
| `com.fang.framework.serialization` | 序列化抽象（格式可插拔） | 待做 |
| `com.fang.framework.saveload` | 存档驱动：槽位、分段、区块文件、原子写 | 待做 |

### 装扩展包

**方式一：安装窗口**（推荐）

`Tools/Fang Framework/Extension Packages` → 刷新索引 → 点「安装」。窗口会比对索引版本与本地版本，给出安装 / 更新 / 卸载按钮，并在核心包未装时禁用按钮。

**方式二：手写 `Packages/manifest.json`**

```json
{
  "dependencies": {
    "com.fang.framework": "https://github.com/fangzw12138/FangFrameWork.git?path=/Packages/com.fang.framework#com.fang.framework/v0.5.0",
    "com.fang.framework.eventbus": "https://github.com/fangzw12138/FangFrameWork.git?path=/Packages/com.fang.framework.eventbus#com.fang.framework.eventbus/v0.1.0"
  }
}
```

两个决定性事实（详见 `Documentation~/扩展包分发.md`）：

1. **Unity 不检测 git 依赖更新** —— git 依赖解析后锁进 `packages-lock.json`，升级只能重新 `Client.Add` 一个新 tag。更新检测由安装窗口自己做。
2. **UPM 不支持包内声明 git URL 依赖** —— 所以扩展包的 `dependencies` 是空对象，依赖靠 asmdef 引用 + 窗口前置检查 + 文档声明落地。

扩展包索引是仓库根 `packages.json`，用 Unity 自带 `JsonUtility` 解析，零第三方依赖。

## 内容

| 类型 | 位置 | 形态 | 说明 |
| --- | --- | --- | --- |
| `ConfigDataSo` | `Runtime/Core/Domain` | ScriptableObject | 只读配置资产基类 |
| `Data<TConfig>` | `Runtime/Core/Domain` | 纯 C# | 运行时数据唯一真相来源 |
| `Controller<TConfig, TData>` | `Runtime/Core/Domain` | MonoBehaviour | 逻辑层 |
| `Service` | `Runtime/Core` | MonoBehaviour | Scope 管理的长期能力基类 |
| `Scope` | `Runtime/Core` | MonoBehaviour | 服务表 / 父子树 / 生命周期驱动 / 释放 |
| `ILifecycle` / `ITickable` / `IFixedTickable` | `Runtime/Core` | 接口 | 初始化与销毁、每帧、固定帧 |

依赖方向单向：`Controller → Data`。`Controller` 不认识表现层。

**`Scope` / `Service` / `Controller` 都是 MonoBehaviour；`Data<TConfig>` 是纯 C#，`ConfigDataSo` 是 ScriptableObject。核心包不提供表现层基类**：视觉脚本 `XxxVisual` 是领域侧的普通 MonoBehaviour，写法见 `Documentation~/表现层规范.md`。

`Scope` 把服务托管在自己的子物体 `Services` 下（`AddService<T>()` 建物体 + `AddComponent<T>()`）。框架不提供宿主 MonoBehaviour，也不写 `Awake` / `Update` / `OnDestroy` —— 使用方自己写一个，调 `Scope.OnInit` / `Scope.Tick` / `Scope.FixedTick` / `Scope.OnDispose`。框架不销毁任何 GameObject。

## 文档

- `Documentation~/架构总览.md`
- `Documentation~/快速开始.md`
- `Documentation~/表现层规范.md`
- `Documentation~/扩展包分发.md`
- `Documentation~/目录规范.md`
- `Documentation~/编码规范.md`
- `Documentation~/AI约束.md`

`Documentation~` 目录名带 `~` 后缀，不参与 Unity 导入。

## Editor

| 入口 | 说明 |
| --- | --- |
| `Tools/Fang Framework/Extension Packages` | 扩展包索引与安装窗口：拉索引、比对版本、装 / 更新 / 卸 |

程序集 `Fang.Framework.Editor`。索引解析（`ExtensionPackageIndex`）与安装状态机（`ExtensionPackageInstaller`）是纯逻辑、可单测；只有窗口（`ExtensionPackagesWindow`）接触 UI 与网络。

## 许可证

MIT，见 `LICENSE.md`。
