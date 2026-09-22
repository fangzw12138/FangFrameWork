# UI

`com.fang.framework.ui` 的设计理由。

## 一、它解决什么

核心包明确**不提供表现层基类**（`表现层规范.md` §一：`WorldObject` 已删除，表现层三件套只有约定没有类型），UI 面板同样是表现层，所以面板基类、面板服务、面板栈这些能力必须由扩展包补上 —— 这就是本包。

它给出四件事：

1. **双轨面板基类**：UGUI（预制体 + Canvas）与 UI Toolkit（UXML/USS/PanelSettings）走同一套 API。
2. **`UIService : Service`**：由 `Scope` 托管，负责创建、复用、面板栈、层排序与跟随面板的位置更新。
3. **配置资产**：层配置、面板配置（两轨各一种）、UI 项目配置（目录字段 + 层与面板清单）。配置只读。
4. **FangHub 页**：面板清单浏览/定位 + 面板脚手架（新增、创建预制体、删除、校验）。

## 二、为什么面板形态是「Controller 三件套」

核心包的三件套是 `ConfigDataSo` / `Data<TConfig>` / `Controller<TConfig, TData>`，`Data` 是唯一真相来源（`AI约束.md` §二）。本包的面板直接沿用这条：

| 角色 | 本包类型 |
| --- | --- |
| 配置 | `UIPanelConfigDataSo`（`UIPrefabPanelConfigDataSo` / `UIVisualTreePanelConfigDataSo`） |
| 数据 | `UIPanelData<TConfig> : Data<TConfig>`（面板可派生自己的 `XxxData`） |
| 逻辑 | `UIPanelController<TConfig, TData> : Controller<TConfig, TData>` |

不引入 `UIView` + `UIPresenter` 双类（参考工程 `Packages/UI` 的做法）：那会与核心包三件套并行存在两套「逻辑 + 状态」模型，`Data` 的唯一真相来源地位也会被稀释。

代价（认下）：面板控制器同时扮演「使用方宿主」—— UGUI 轨读自己的 `[SerializeField]` 控件引用，UITK 轨在 `OnInit()` 里建 `UIDocument` 并绑定控件。`AI约束.md` §13 允许 Unity 接触面落在「视觉脚本与使用方自己写的宿主」上，面板控制器就是面板自己的宿主。

## 三、为什么双轨

UGUI 与 UI Toolkit 都是真实在用的：老工程 HUD 用 UGUI 预制体，新面板用 UXML。两条轨的差别只在「内容从哪来」：

| 轨 | 配置 | 内容来源 | 控制器基类 |
| --- | --- | --- | --- |
| UGUI | `UIPrefabPanelConfigDataSo` | `GameObject Prefab`（根上挂控制器） | `UIPanelController<TConfig, TData>` |
| UITK | `UIVisualTreePanelConfigDataSo` | `VisualTreeAsset` + `PanelSettings` + `StyleSheet` | `UIVisualTreePanelController<TConfig, TData>`（多一个 `protected VisualElement Root`） |

面板栈、层排序、跟随、输入阻塞这些**与轨无关**的能力只实现一次，两条轨共用。

## 四、为什么配置里没有类型名

参考工程用「程序集名 + 类型全名 + `Type.GetType`」在运行时还原 View/Presenter 类型 —— 那是反射，`AI约束.md` §11 禁止。本包的取舍是**让调用方显式给出类型**：

```csharp
var panel = ui.Open<HudController, HudConfigDataSo, HudData>(hudConfig);
```

代价（认下）：`Open` 有三个泛型参数，调用点略长。收益是编译期绑定 —— 配置资产里没有类型名字符串，改类名/重构由编译器兜底，也不会出现「配置指向了已删除的类型」这种运行时才发现的问题。

`Data` 由面板自己创建，避免反射构造：

```csharp
public override HudData CreateData(HudConfigDataSo config)
{
    return new HudData(config);
}
```

## 五、为什么没有资源加载层

`AI约束.md` §1 要求零第三方依赖，参考工程的 `Addressables` / `UniTask` 加载与释放全部去掉：配置资产直接持有 `VisualTreeAsset` / `PanelSettings` / `StyleSheet` / `GameObject` 引用，`UIService` 不做任何加载、不做异步。

AB / Addressables / 图集管理属于另一个扩展包（`assetmanagement`）的职责；等它落地后，配置资产换成「资源键」、`UIPanelFactory` 接一层加载即可，面板栈与控制器不动。

## 六、生命周期与销毁

| 谁 | 做什么 |
| --- | --- |
| `UIService` | 建物体（层级根 + Canvas + 面板实例）、登记、开栈、释放（`OnClose` 钩子 + 隐藏），**从不销毁** |
| 面板控制器 | `OnInit()` 建自己的内容（UGUI 读引用 / UITK 建 `UIDocument`），`OnOpen` / `OnClose` / `OnFocus` / `OnBlur` 由栈驱动 |
| 使用方 | 销毁物体：销毁服务物体即回收整棵 UI 层级（`UIService` 的 `UI` 根挂在自己物体下） |

`AI约束.md` §14「框架不销毁 GameObject」与 `表现层规范.md` §三「管理服务从不 Destroy」在这里的具体含义是：

- `Close(panelId)` 是**释放**：出栈 + `OnClose` + 隐藏，实例保留复用 —— 与参考工程 `LostMemories` 的「实例保留」一致；
- 同一配置再次 `Open` 返回**同一实例**（不重复创建）；
- `UIService.OnDispose()`（由 `Scope.OnDispose()` 驱动）只做 `PopAll` + 逐个 `OnDispose()` + 清表，不碰任何物体。

## 七、面板栈

`UIPanelStack`（纯逻辑，可单测）维护打开顺序与焦点：

| 操作 | 行为 |
| --- | --- |
| `Push(panel, closePreviousOnOpen, blocksInput)` | 旧栈顶 `OnBlur`；若 `closePreviousOnOpen` 则隐藏 + `OnClose` 并记住「被上层隐藏」；新面板 `OnOpen` + `OnFocus` |
| `Pop(panel)` | `OnBlur` + `OnClose` + 出栈；若被移除项的前一项是被它隐藏的，则恢复显示 + `OnOpen` + `OnFocus` |
| `PopTop()` / `PopAll()` | 自顶向下依次 `Pop` |

- `closePreviousOnOpen` 是**面板级**配置（`UIPanelConfigDataSo.ClosePreviousOnOpen`），不是整层互斥：同一个 UI 里既有「设置面板盖住主菜单」也有「HUD 与背包共存」，硬编码互斥规则会在第二个场景里被推翻。
- `blocksInput` 也是面板级配置，栈按计数维护 `HasInputBlockingPanel`，并在 0→1 / 1→0 时发 `InputBlockingPanelOpened` / `InputBlockingPanelClosed`，用来关掉角色输入。计数而不是布尔，是因为可能同时有两个模态面板。

## 八、层与排序

层不是枚举，而是配置资产（`UILayerConfigDataSo`：`Id` + `BaseSortOrder`）。面板按 `LayerId` 归属，最终排序号 = 层 `BaseSortOrder` + 面板 `SortOrder`：

- 具体有哪些层（HUD / Popup / Dialog / Notification…）是业务决定，写进框架就是「领域业务概念进核心」的反面教材；
- `LayerId` 留空或查不到层时退回面板自身 `SortOrder`，并在 `Configure` 时报 `LogWarning`（不吞错，也不让面板开不出来）。

UGUI 轨按排序号复用 Canvas（`UICanvasPool`：`ScreenSpaceOverlay` + `CanvasScaler(ScaleWithScreenSize, 1920x1080, Expand)` + `GraphicRaycaster`）；UITK 轨把排序号写进 `UIDocument.sortingOrder`。

## 九、跟随面板

世界坐标 → 屏幕坐标的换算分两套（UGUI 用 `transform.position`，UITK 用 `RuntimePanelUtils.ScreenToPanel`），但**驱动只有一处**：`UIService` 实现 `ITickable`，`OnTick` 里统一更新所有可见跟随面板。

参考工程用 `UniTask.Yield(PostLateUpdate)` 自驱循环 —— 那是第三方依赖，而且每个跟随面板一条循环、生命周期散落。走 `ITickable` 的代价是**使用方要显式调 `Scope.Tick(deltaTime)`**，这与核心包「框架不抢 Unity 回调」的立场一致。

`UIVisualTreeBinder.ApplyScreenPosition` 里有一段 y 轴翻转：本引擎版本的 `ScreenToPanel` 只做等比缩放、不做上下翻转，翻转必须自己补。

## 十、多 UI 项目（多 demo）

「UI 项目」是一等概念：**一个 UI 项目 = 一个 `UIProjectConfigDataSo` 资产**，它就是这个 demo 的 UI 总控。一个 Unity 工程可以有无数个 UI 项目（每个 demo 一个），互不影响 —— 目录、层、面板清单都跟着各自的 SO 走，而不是写死在编辑器里。

UI 项目 SO 存三类东西：

| 内容 | 字段 | 归谁用 |
| --- | --- | --- |
| 目录（逐项显式，5 项） | 面板 SO 目录 / 面板预制体目录 / 控制器脚本目录 / UXML·USS 目录 / 层 SO 目录 | 编辑器（脚手架推路径、校验）；运行期不读 |
| 根命名空间 | `RootNamespace` | 编辑器：生成脚本的命名空间 = `{RootNamespace}.UI.Panels` |
| 清单 | `Layers` + `Panels` | 运行期 `Configure`（层排序、面板登记） |

- 目录一律写 `Assets/...` 工程相对路径；字段没填或非法时，脚手架拒绝生成并报错（校验会逐项列出）。
- **每个 UI 项目各自一套层**（层 SO 放该项目的层 SO 目录）。
- 面板资产按目录字段落位：

| 资产 | 路径 |
| --- | --- |
| 面板 SO | `{面板 SO 目录}/{Panel}ConfigData.asset` |
| 面板预制体 | `{面板预制体目录}/{Panel}.prefab` |
| 脚本 | `{控制器脚本目录}/{Panel}/{Panel}Data.cs`、`{Panel}Controller.cs` |
| UXML / USS | `{UXML·USS 目录}/{Panel}/{Panel}.uxml`、`{Panel}.uss` |

- **运行期选项目**：哪个 demo 用哪个 UI 项目，初始化就传哪个 —— `uiService.Configure(项目SO)`（签名不变）；两个 demo 各自 `Configure` 自己的 SO，互不影响。
- **用法**：`Assets` 右键 `Create > Fang Framework > UI > 项目配置` 新建 UI 项目 SO → 填 5 个目录与根命名空间 → 在 FangHub 的 UI 页选中它，之后的清单 / 新增 / 删除 / 校验都作用于它。

## 十一、编辑器接入

### 11.1 FangHub 页

`Tools/Fang Framework/Fang Hub` → 侧栏「UI」（`[FangHubPage("framework-ui", "UI", "UI")]`，页面状态走 `FangHubPageState`，会记住上次选中的 UI 项目）：

- **UI 项目选择**：顶部 `ObjectField` 列出工程内全部 `UIProjectConfigDataSo`，选中哪个，下面的清单 / 生成 / 删除 / 校验就作用于哪个 —— 可选取、不写死路径。
- **清单**：逐行显示选中 UI 项目 `Panels` 里每个面板的 Id / 层 / 轨 / 完整性（脚本是否存在、UXML 与 PanelSettings 或预制体是否齐），支持搜索与定位（配置、脚本、目录）。
- **新增**：填名称 + 选轨（UGUI / UITK）→ 按所选项目的目录字段与根命名空间生成 UXML/USS（UITK 轨）、`XxxData.cs`、`XxxController.cs`、面板配置资产，并登记进该项目的 `Panels`。
- **创建预制体**（UGUI 轨）：脚本编译完成后点一次，生成预制体（根上挂控制器，RectTransform 拉伸铺满）并写回配置。
- **删除**：先列出将被删除的目录/预制体/配置与「从 UI 项目中摘除」，确认后执行。
- **校验**：5 个目录字段与根命名空间、每个面板的缺失项、重复 Id、面板层是否能解析到本项目的 `Layers`、以及「目录里存在但没登记」的配置。

### 11.2 两个取舍

1. **UGUI 预制体手动创建**。参考工程在脚手架里写类型名 → `DidReloadScripts` + `SessionState` 续跑回填。本包配置不含类型名，预制体也不需要回填，因此不做时序魔法：脚本生成后等编译，点一次「创建预制体」即可。
2. **脚手架不强制整工程刷新**。`CreatePanel` 只 `ImportAsset` 它自己写的 UXML/USS（配置要引用它们），不调 `AssetDatabase.Refresh()`；生成的 `.cs` 交给下一次编辑器刷新去编译。脚手架动作不该顺带把整个工程重编译一遍。

### 11.3 测试程序集的一个坑

Unity 拒绝在**编辑器程序集**里 `AddComponent` MonoBehaviour（`Can't add script behaviour ... because it is an editor script`）。而本包的 `UIService` 测试需要真实的 `Scope` 与面板控制器，所以测试替身单独放在**非 Editor 平台**的测试程序集 `Fang.Framework.UI.TestDoubles`（`includePlatforms: []` + `testAssemblies: true` + `UNITY_INCLUDE_TESTS`），测试程序集 `Fang.Framework.UI.Tests`（Editor 平台）引用它。

## 十二、类型清单

| 类型 | 说明 |
| --- | --- |
| `UILayerConfigDataSo` | 层：`BaseSortOrder` |
| `UIPanelConfigDataSo` | 面板配置基类：`LayerId` / `SortOrder` / `ClosePreviousOnOpen` / `BlocksInput` |
| `UIPrefabPanelConfigDataSo` | UGUI 轨：`Prefab` |
| `UIVisualTreePanelConfigDataSo` | UITK 轨：`VisualTreeAsset` / `PanelSettings` / `StyleSheets` |
| `UIProjectConfigDataSo` | UI 项目（总控）：目录字段（面板 SO / 预制体 / 脚本 / UXML·USS / 层 SO）+ `RootNamespace` + `Layers` + `Panels` |
| `UIPanelData<TConfig>` | 面板数据：`LayerId` / `SortOrder` / `IsOpen` / `IsVisible` |
| `UIPanelController<TConfig, TData>` | 面板控制器基类：`CreateData` / `OnInit` / `OnDispose` / `OnOpen` / `OnClose` / `OnFocus` / `OnBlur` / `SetVisible` |
| `UIVisualTreePanelController<TConfig, TData>` | UITK 轨控制器基类：多一个 `protected VisualElement Root` |
| `IUIPanel` | 面板句柄（面板栈的异构元素类型） |
| `IUIFollowPanel` | 跟随面板句柄 |
| `UIFollowPanelController<TConfig, TData>` | 跟随面板基类：`SetTarget` + `ApplyScreenPosition` |
| `UIPrefabFollowPanelController<TConfig, TData>` | UGUI 跟随实现 |
| `UIVisualTreeFollowPanelController<TConfig, TData>` | UITK 跟随实现 |
| `UIService` | 服务：`Configure` / `SetFollowCamera` / `Open` / `OpenFollow` / `Close` / `CloseTop` / `CloseAll` / `IsOpen` / `GetOpened` / `OnTick` / `OnDispose` + 输入阻塞事件 |
| `UIPanelStack` / `UILayerResolver` / `UIPanelFactory` / `UICanvasPool` / `UIVisualTreeBinder` | `internal`，纯逻辑或轨内实现 |

## 十三、明确不做

- 资源加载层（AB / Addressables / 图集）—— 归 `assetmanagement` 扩展包。
- 整层互斥（「同层只能开一个」）、Dialog / Notification 业务壳 —— 归使用方。
- 转场动画（参考工程的 `FUguiPanelTransition`）与安全区换算（参考工程 `SafeAreaUtil` 的面板单位陷阱）—— 等有明确使用场景再加。
- 模板初始化（一键生成 PanelSettings / 主题 USS / 项目配置 / 示例面板）、Play 模式运行时查看页。
- 硬编码层枚举、项目专属字体逻辑。
