# FangHub

核心包内的**编辑器工具枢纽**：一个窗口，左侧按分组列出工具页，右侧渲染选中页。
扩展包用「属性 + 接口」声明自己的编辑器页面，FangHub 自动发现 —— **扩展包零注册代码，核心包永不引用扩展包**。

入口：`Tools/Fang Framework/Fang Hub`。核心包自带的「框架与扩展」页（包中心：核心包更新 + 扩展包索引 / 安装 / 更新 / 卸载）也在 Hub 里，不再单独开窗口。

## 一、类型清单

| 类型 | 位置 | 形态 | 职责 |
| --- | --- | --- | --- |
| `FangHubWindow` | `Editor/Hub` | `EditorWindow` | 窗口本体。UI Toolkit 纯 C# 建树（不引 UXML / USS），唯一接触 UI 的地方 |
| `FangHubPageAttribute` | `Editor/Hub` | 特性 | 声明页面：`Id` / `Title` / `Category` / `Description` / `Order` |
| `IFangHubPage` | `Editor/Hub` | 接口 | 页面基础：`OnInitialize(FangHubWindow)` / `OnSelected()` |
| `IFangHubImGuiPage` | `Editor/Hub` | 接口 | IMGUI 渲染能力：`string OnGUI()` |
| `IFangHubVisualElementPage` | `Editor/Hub` | 接口 | UI Toolkit 渲染能力：`VisualElement CreateVisualElement()` |
| `FangHubPageDescriptor` | `Editor/Hub` | 纯 C# | 扫描结果：页面元数据 + `PageType` |
| `FangHubPageRegistry` | `Editor/Hub` | 静态类 | `Discover()`（`TypeCache` 扫描）+ `BuildView()`（纯逻辑视图构建） |
| `FangHubLayout` / `FangHubGroup` | `Editor/Hub` | 纯 C# `[Serializable]` | 分组与条目顺序的布局模型，增删改全是纯逻辑 |
| `FangHubLayoutStore` | `Editor/Hub` | 静态类 | 布局持久化：`JsonUtility` ↔ `EditorPrefs` |
| `FangHubPageState` | `Editor/Hub` | 静态类 | 给页面存自己的输入 / 选项状态 |
| `FangEditorPrefs` | `Editor` | 静态类 | 工程 token + 键前缀，保证同机多工程互不串 |
| `ExtensionPackagesPage` | `Editor/Hub/Pages` | 页面 | 内置「框架与扩展」页（包中心）：顶部核心包一行 + 左栏扩展包列表（状态筛选 / 搜索）+ 右栏详情与操作，逻辑走 `ExtensionPackageIndex` / `ExtensionPackageCatalog` / `ExtensionPackageInstaller` |

分层与 `Editor/` 的既有约定一致：**纯逻辑（可单测，不碰 UI）** / **接触 UI**（`FangHubWindow`、`ExtensionPackagesPage`）。

## 二、页面怎么写

### 2.1 UI Toolkit（主推）

```csharp
[FangHubPage("audio-clips", "音频片段", "Audio", Description = "生成与管理音频片段。", Order = 0)]
public sealed class AudioClipsPage : IFangHubVisualElementPage
{
    public void OnInitialize(FangHubWindow window)
    {
    }

    public void OnSelected()
    {
    }

    public VisualElement CreateVisualElement()
    {
        var root = new VisualElement();
        root.style.flexGrow = 1f;
        root.Add(new Label("音频片段"));
        return root;
    }
}
```

`CreateVisualElement()` 返回的根元素会被 Hub 挂进详情区，并补 `flexGrow = 1`。

### 2.2 IMGUI（特殊场景）

页面自己已有 IMGUI 代码（`EditorGUILayout`）、要直接复用某个 `Editor` 的 `OnInspectorGUI()`、或要在 IMGUI 里显示 `XxxDataSo` 时，用 `IFangHubImGuiPage`：

```csharp
[FangHubPage("audio-inspector", "音频配置", "Audio", Description = "直接编辑配置资产。", Order = 1)]
public sealed class AudioInspectorPage : IFangHubImGuiPage
{
    public void OnInitialize(FangHubWindow window)
    {
    }

    public void OnSelected()
    {
    }

    public string OnGUI()
    {
        EditorGUILayout.LabelField("音频配置", EditorStyles.boldLabel);
        return "已保存。";
    }
}
```

- Hub 用 `IMGUIContainer` + `ScrollView` 承载 IMGUI 页，内容高度自适应。
- `OnGUI()` 返回的非空字符串会作为提示条显示在内容下方（状态回传通道）。
- **PlayMode 下 IMGUI 页不渲染**，只显示「游戏运行时不刷新工具页预览」。原因：常驻可见窗口每帧重绘重量级工具页（缩略图 / 3D 预览）会叠加 Game 视图的 GPU 负载，实测可触发 TDR 设备丢失导致编辑器崩溃。UI Toolkit 页不受影响。

### 2.3 生命周期

| 时机 | 行为 |
| --- | --- |
| 选中页面 | `Activator.CreateInstance` → `OnInitialize(window)` → 建内容 → `OnSelected()` |
| 再次选中同一页 | **重建实例**（不缓存）。页面的输入状态一律走 `FangHubPageState` |
| 切走 / 关窗 | 内容元素从树上摘除，实例丢给 GC；**没有销毁回调** |
| 构造或 `OnGUI()` 抛异常 | 详情区显示错误文本，窗口与其它页不受影响 |
| 页面里的异步操作 | 自带收敛（例如轮询在完成时自己注销 `EditorApplication.update`），Hub 不代管 |

### 2.4 扫描规则

`FangHubPageRegistry.Discover()` 按 `TypeCache` 扫描全部已加载程序集，逐条过滤：

| 规则 | 处理 |
| --- | --- |
| `abstract` / 接口 | 跳过 |
| 未实现 `IFangHubPage` | 跳过 |
| 未实现 `IFangHubImGuiPage` 也未实现 `IFangHubVisualElementPage` | 跳过 |
| `Id` / `Title` / `Category` 为空 | 跳过 + `LogWarning` |
| 缺少公开无参构造函数 | 跳过 + `LogWarning` |
| `Id` 与已发现页面重复 | 保留先到者 + `LogWarning` |
| 排序 | `Order` 升序，同序按 `Title` 序号比较 |

## 三、扩展包怎么接

1. 扩展包加 `Editor/` 目录与 asmdef（如 `Fang.Framework.Audio.Editor.asmdef`）：

```json
{
  "name": "Fang.Framework.Audio.Editor",
  "references": ["Fang.Framework", "Fang.Framework.Editor"],
  "includePlatforms": ["Editor"],
  "autoReferenced": false
}
```

2. 写一个实现 `IFangHubPage` + `IFangHubVisualElementPage`（或 `IFangHubImGuiPage`）的类，打上 `[FangHubPage(...)]`，公开无参构造函数。
3. 装包即出现在侧栏；卸载后布局里的条目落到「未分配」，窗口不报错。

核心包不引用扩展包，扩展包也不需要在核心包里登记任何东西。

真实接入案例：`Packages/com.fang.framework.eventbus/Editor/EventBusPage.cs`（程序集 `Fang.Framework.EventBus.Editor`）—— 只读观察运行中的订阅表，用编辑器侧反射读私有字段，不给运行时开 API。理由见该包 `Documentation~/事件总线.md` §八。

## 四、侧栏与布局

| 能力 | 说明 |
| --- | --- |
| 分组 | 折叠 / 重命名 / 新建 / 删除（组内条目退回未分配） |
| 条目 | 右键删除；`+` 按钮把未分配页面加回某个分组 |
| 拖拽 | 组内排序、跨组移动、分组排序；拖拽时显示插入位置指示条 |
| 搜索 | 按 `Title` / `Description` / `Category` 不区分大小写过滤，命中为空的分组不显示 |

布局状态（分组、组内顺序、折叠状态、已见过的页面）存 `EditorPrefs`，键为 `Fang.Framework.Editor.<工程token>.FangHub.Layout`（工程 token 取 `Application.dataPath` 的 MD5）。**不落资产**：包目录只读，往包里 `CreateAsset` 会失败；存 `EditorPrefs` 也不污染工程目录。

| 场景 | 表现 |
| --- | --- |
| 首次打开（无存档） | 按 `Category` 自动建组，组内按 `Order` → `Title` |
| 新装扩展包（新页面出现） | 自动补进同名分组，按 `Order` 插到合适位置 |
| 手动删掉的页面 | 不会再自动回来（`seenPageIds` 记住），用 `+` 加回 |
| 卸载扩展包 | 布局里的未知 id 原样保留（重装后回到原位），窗口不报错 |

## 五、页面状态

```csharp
FangHubPageState.SetString("audio-clips", "root", "Assets/Game");
var root = FangHubPageState.GetString("audio-clips", "root", "Assets");
FangHubPageState.SetBool("audio-clips", "overwrite", true);
FangHubPageState.SetInt("audio-clips", "type", 1);
```

`scope` 约定传页面自己的 `Id`（属性里那个），键由 `FangEditorPrefs.BuildKey` 拼成 `Fang.Framework.Editor.<工程token>.<scope>.<key>`。

## 六、与参考实现（CastawaySurvival 初版）的差异

| 初版做法 | 问题 | 现在 |
| --- | --- | --- |
| UXML / USS 写死 `Assets/FangGameFramework/...` 路径 | 包里没有 Assets 路径，UPM 安装后必然加载失败 | UI Toolkit 纯 C# 建树，不引 UXML / USS |
| `FangHubConfig.asset` 写死在 Assets 路径，`AssetDatabase.CreateAsset` | 包目录只读，创建资产会失败 | 布局存 `EditorPrefs`（按工程 token 隔离） |
| 搜索框图标 `search_icon.png` | 包内资源引用另有开销，收益低 | 纯文本搜索框 |
| `HubToolRegistry` 一个静态类做扫描 + 布局读写 + 分组 + 搜索 + 实例化 | 纯逻辑与 EditorPrefs 混在一起，没有测试接缝 | 拆成描述符 / 注册表 / 布局模型 / 持久化 / 页面状态五块 |
| 所有页面强制实现 `OnGUI()`，UI Toolkit 页要写空实现 | 主推 UI Toolkit 时接口别扭 | 基础接口 + IMGUI 能力接口 + UI Toolkit 能力接口，二选一 |
| `OnInitialize(workbenchWindow, manifest)` 里的 manifest 恒为 null | 调用方白处理一次 null | `OnInitialize(FangHubWindow)` 只传窗口 |
| 每秒轮询刷新状态指示器 | 无谓重绘 | 去掉轮询，选中态驱动 |

## 七、测试

`Tests/Editor/` 四个文件，EditMode 运行：

| 文件 | 覆盖 |
| --- | --- |
| `FangHubLayoutTests.cs` | 默认分组与排序、`Reconcile` 补新页 / 不复活已删页 / 容忍未知 id、增删条目、组内与跨组排序的索引修正、删组退未分配、分组排序、重命名与折叠 |
| `FangHubPageRegistryTests.cs` | 内置页被发现（元数据逐项断言）、三类跳过规则（无页面接口 / 无渲染接口 / 抽象类）、`BuildView` 的分组顺序、未知 id、搜索过滤与空分组处理 |
| `FangHubPrefsTests.cs` | 键格式（前缀 + 工程 token + scope + key）、`FangHubPageState` 三类值往返与默认值、布局的 `JsonUtility` 往返、非法 JSON 抛 `ArgumentException`（`FangHubLayoutStore` 的回落依据） |
| `ExtensionPackageCatalogTests.cs` | 包列表的四种状态筛选、搜索（显示名 / 包标识 / 描述 / tag，不区分大小写）、描述「本地优先、回落索引」、行状态组合、保持索引顺序、读临时 `package.json` |

两处刻意的取舍：

- **跳过规则的探针类型不带渲染接口**（因此不会被真实扫描收进侧栏），代价是「`Id` 重复」与「缺少无参构造」两条规则没有单测 —— 造出可命中的探针页会让测试页出现在开发者的 Hub 侧栏里。
- **`FangHubLayoutStore` 不做写入型往返单测**：它的键是工程级的固定键，写一次就会覆盖开发者本机的真实布局。改测 `JsonUtility` 往返（真正的风险点）与 `Load` 的可用性。
