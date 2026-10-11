# Fang Framework · Async UI

`com.fang.framework.ui.async` —— 建立在**异步 Scope** 上的 UI 面板扩展包：UGUI 与 UI Toolkit 双轨、面板栈、层排序。

面板控制器继承 `Fang.Framework.Async.AsyncController<TConfig, TData>`，因此面板本身就在**异步树**里：`Scope.GetService<T>()` 沿异步 scope 向上查找，`InitAsync` / `DisposeAsync` 由框架负责，打开与关闭是 `UniTask`。

本包**不使用**核心同步族的 `Scope` / `Service` / `Controller<,>`，也不依赖同步 UI 包 `com.fang.framework.ui`。

## 依赖

| 依赖 | 说明 |
| --- | --- |
| `com.cysharp.unitask` `2.5.11` | 来自 OpenUPM 注册表 `package.openupm.com`；写在 `package.json` 的 `dependencies` 里 |
| `com.fang.framework` | 核心包，只读复用 `ConfigDataSo` / `Data<TConfig>`。不写在 `dependencies` 里（按 git URL + tag 分发、不在任何 registry 中），靠 asmdef 的 `references` 落地 |
| `com.fang.framework.async` | 异步族，提供 `AsyncScope` / `AsyncService` / `AsyncController<TConfig, TData>`。同上，靠 asmdef 的 `references` 落地 |

## 依赖方向

```text
Fang.Framework.UI.Async
  └── Fang.Framework.Async   AsyncScope / AsyncService / AsyncController<TConfig, TData>
        └── Fang.Framework   ConfigDataSo / Data<TConfig>
```

## 配置资产

| 资产 | 菜单 | 说明 |
| --- | --- | --- |
| 层配置 | `Create > Fang Framework > UI (Async) > 层配置` | 一个层一条：`Id` + `BaseSortOrder` |
| UGUI 面板配置 | `Create > Fang Framework > UI (Async) > UGUI 面板配置` | `Prefab` + 层 / 排序 / 关闭前一个 / 阻塞输入 |
| UITK 面板配置 | `Create > Fang Framework > UI (Async) > UITK 面板配置` | `VisualTreeAsset` / `PanelSettings` / `StyleSheets` + 同上 |
| 工程配置 | `Create > Fang Framework > UI (Async) > 工程配置` | 根命名空间、脚手架目录 + 层清单 + 面板注册表 |

配置资产对外**零写入口**，只有只读属性；编辑器侧的写入走 `SerializedObject`。

面板配置**必须登记进工程配置**才能被打开。

## 安装

**方式一：FangHub 的扩展包页**（推荐）

`Tools/Fang Framework/Fang Hub` → 侧栏点「扩展包」→ 点「刷新」→ 点「安装」。

**方式二：手写 `Packages/manifest.json`**

```json
{
  "dependencies": {
    "com.fang.framework": "https://github.com/fangzw12138/FangFrameWork.git?path=/Packages/com.fang.framework#com.fang.framework/v0.5.3",
    "com.fang.framework.async": "https://github.com/fangzw12138/FangFrameWork.git?path=/Packages/com.fang.framework.async#com.fang.framework.async/v0.1.0",
    "com.fang.framework.ui.async": "https://github.com/fangzw12138/FangFrameWork.git?path=/Packages/com.fang.framework.ui.async#com.fang.framework.ui.async/v0.1.0"
  }
}
```

需要本机有 git CLI。

## 用法

```csharp
using Cysharp.Threading.Tasks;
using Fang.Framework.Async;
using Fang.Framework.UI.Async;

public sealed class GameScope : AsyncScope
{
    public UIService UI { get; private set; }

    protected override UniTask OnInitAsync()
    {
        UI = AddService<UIService>();
        UI.Configure(projectConfig);          // UIProjectConfigDataSo，由使用方给（Resources / 序列化字段）
        return UniTask.CompletedTask;
    }
}
```

### 面板

```csharp
using Cysharp.Threading.Tasks;
using Fang.Framework.UI.Async;

public sealed class HudData : UIPanelData<UIPrefabPanelConfigDataSo>
{
    public HudData(UIPrefabPanelConfigDataSo config) : base(config)
    {
    }
}

public sealed class HudController : UIPanelController<UIPrefabPanelConfigDataSo, HudData>
{
    [SerializeField] private TMP_Text _title;

    public override HudData CreateData(UIPrefabPanelConfigDataSo config)
    {
        return new HudData(config);
    }

    protected override UniTask OnOpenAsync()
    {
        _title.text = "第 1 天";
        return UniTask.CompletedTask;
    }
}
```

UGUI 轨把控制器挂在预制体根上，`OnOpenAsync()` 里读自己的 `[SerializeField]` 控件引用。
UI Toolkit 轨用 `UIVisualTreePanelController<TConfig, TData>`，基类负责建 `UIDocument`，子类在基类初始化之后用 `Root.Q<T>("name")` 取控件。

### 打开与关闭

```csharp
var hud = await ui.OpenAsync<HudController, UIPrefabPanelConfigDataSo, HudData>(hudConfigId);
var again = await ui.OpenAsync<HudController, UIPrefabPanelConfigDataSo, HudData>(hudConfig);   // 同配置复用同一实例

await ui.CloseAsync(hudConfigId);   // 出栈 + OnCloseAsync + 隐藏，实例保留
await ui.CloseTopAsync();
await ui.CloseAllAsync();

ui.IsOpen(hudConfigId);
var panel = ui.GetOpened(hudConfigId);
```

### 输入阻塞

```csharp
ui.InputBlockingPanelOpened += () => input.enabled = false;
ui.InputBlockingPanelClosed += () => input.enabled = true;

var blocked = ui.HasInputBlockingPanel;
```

## 编辑器

本包带一个 FangHub 页（`Editor/UIPage.cs`，程序集 `Fang.Framework.UI.Async.Editor`）：`Tools/Fang Framework/Fang Hub` → 侧栏「UI」→ 页「Async UI」。

- **清单**：面板配置的 Id / 层 / 轨 / 完整性（登记、脚本、UXML、PanelSettings、预制体），支持搜索与定位。
- **新增**：名称 + 轨（UGUI / UITK）+ 目标根目录 + 根命名空间 → 生成 `UI/Panels/<Name>/`（UXML/USS）、`Scripts/UI/Panels/<Name>/`（`<Name>Data.cs`、`<Name>Controller.cs`）、`Configs/UI/<Name>ConfigData.asset`，并登记进项目配置。
- **创建预制体**（UGUI 轨）：脚本编译完成后点一次。
- **删除**：先列出将删除的内容，确认后执行。
- **校验**：列出缺失项与重复 Id。

挂 FangHub 页的完整写法见核心包 `Documentation~/FangHub.md`。

## 设计理由

见 `Documentation~/异步UI.md`：为什么用 Controller 三件套、为什么配置不含类型名、为什么 UIService 不销毁面板物体、面板栈与层排序的取舍、收尾为什么每一步各自兜底、测试替身为什么单独一个程序集。

## 许可证

MIT，见 `LICENSE.md`。
