# Fang Framework · UI

`com.fang.framework.ui` —— Fang Framework 的 UI 面板扩展包：UGUI 与 UI Toolkit 双轨、面板栈、层排序、跟随面板。

零第三方依赖，零反射。`UIService` 由 `Scope` 托管，面板沿用核心包三件套（`ConfigDataSo` / `Data` / `Controller`）。

## 依赖

本包**引用核心包** `com.fang.framework`（asmdef 引用 `Fang.Framework`）。

`package.json` 的 `dependencies` 是空对象 —— UPM 不支持包内声明 git URL 依赖，因此依赖关系靠 asmdef 引用 + FangHub 扩展包页的前置检查 + 本文档声明。安装本包前请先确保核心包已装。

## 安装

**方式一：FangHub 的扩展包页**（推荐）

`Tools/Fang Framework/Fang Hub` → 侧栏点「扩展包」→ 点「刷新」→ 点「安装」。

**方式二：手写 `Packages/manifest.json`**

```json
{
  "dependencies": {
    "com.fang.framework": "https://github.com/fangzw12138/FangFrameWork.git?path=/Packages/com.fang.framework#com.fang.framework/v0.5.0",
    "com.fang.framework.ui": "https://github.com/fangzw12138/FangFrameWork.git?path=/Packages/com.fang.framework.ui#com.fang.framework.ui/v0.1.0"
  }
}
```

需要本机有 git CLI。

## 资产

| 资产 | 菜单 | 用途 |
| --- | --- | --- |
| 层配置 | `Create > Fang Framework > UI > 层配置` | 一个层一条：`Id` + `BaseSortOrder` |
| UGUI 面板配置 | `Create > Fang Framework > UI > UGUI 面板配置` | `Prefab` + 层 / 排序 / 关闭前一个 / 阻塞输入 |
| UITK 面板配置 | `Create > Fang Framework > UI > UITK 面板配置` | `VisualTreeAsset` / `PanelSettings` / `StyleSheets` + 同上 |
| 项目配置 | `Create > Fang Framework > UI > 项目配置` | 层列表 + 面板注册表 |

面板配置**必须登记进项目配置**才能被打开。

## 用法

```csharp
using Fang.Framework;
using Fang.Framework.UI;

public sealed class GameScope : Scope
{
    public UIService UI { get; private set; }

    public override void OnInit()
    {
        base.OnInit();

        UI = AddService<UIService>();
        UI.Configure(ProjectConfig);          // UIProjectConfigDataSo，由使用方给（Resources / 序列化字段）
        UI.SetFollowCamera(Camera.main);      // 只有跟随面板需要
    }
}
```

### 面板

```csharp
using Fang.Framework.UI;

public sealed class HudData : UIPanelData<UIVisualTreePanelConfigDataSo>
{
    public HudData(UIVisualTreePanelConfigDataSo config) : base(config)
    {
    }
}

public sealed class HudController : UIVisualTreePanelController<UIVisualTreePanelConfigDataSo, HudData>
{
    private Label _score;

    public override HudData CreateData(UIVisualTreePanelConfigDataSo config)
    {
        return new HudData(config);
    }

    public override void OnInit()
    {
        base.OnInit();                        // UITK 轨：先让基类建 UIDocument
        _score = Root.Q<Label>("score");
    }

    protected override void OnOpen()
    {
        _score.text = "0";
    }
}
```

UGUI 轨把控制器挂在预制体根上，`OnInit()` 里读自己的 `[SerializeField]` 控件引用（不调用 `base.OnInit()`）。

### 打开与关闭

```csharp
var panel = ui.Open<HudController, UIVisualTreePanelConfigDataSo, HudData>(hudConfig);   // 同配置复用同一实例

ui.Close(panel.PanelId);      // 出栈 + OnClose + 隐藏，实例保留
ui.CloseTop();
ui.CloseAll();
ui.IsOpen(panel.PanelId);
```

跟随面板（每次 `OpenFollow` 新建实例，适合每个目标一个血条）：

```csharp
var marker = ui.OpenFollow<MarkerController, UIPrefabPanelConfigDataSo, MarkerData>(
    markerConfig,
    target,
    new Vector3(0f, 1.5f, 0f));
```

位置更新由 `UIService.OnTick` 驱动，使用方负责调 `Scope.Tick(deltaTime)`：

```csharp
scope.Tick(Time.deltaTime);
```

### 输入阻塞

```csharp
ui.InputBlockingPanelOpened += () => input.enabled = false;
ui.InputBlockingPanelClosed += () => input.enabled = true;
```

## 编辑器

本包带一个 FangHub 页（`Editor/UIPage.cs`，程序集 `Fang.Framework.UI.Editor`）：`Tools/Fang Framework/Fang Hub` → 侧栏「UI」。

- **清单**：面板配置的 Id / 层 / 轨 / 完整性（登记、脚本、UXML、PanelSettings、预制体），支持搜索与定位。
- **新增**：名称 + 轨（UGUI / UITK）+ 目标根目录 + 根命名空间 → 生成 `UI/Panels/<Name>/`（UXML/USS）、`Scripts/UI/Panels/<Name>/`（`<Name>Data.cs`、`<Name>Controller.cs`）、`Configs/UI/<Name>ConfigData.asset`，并登记进项目配置。
- **创建预制体**（UGUI 轨）：脚本编译完成后点一次。
- **删除**：先列出将删除的内容，确认后执行。
- **校验**：列出缺失项与重复 Id。

挂 FangHub 页的完整写法见核心包 `Documentation~/FangHub.md`。

## 设计理由

见 `Documentation~/UI.md`：为什么是 Controller 三件套、为什么配置不含类型名、为什么没有加载层、为什么服务不销毁物体、面板栈与层排序的取舍、测试替身为什么单独一个程序集。

## 许可证

MIT，见 `LICENSE.md`。
