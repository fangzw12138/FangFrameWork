# Fang Framework · Config Inspector

`com.fang.framework.configinspector` —— Fang Framework 的「展开配置资产」扩展包。

给 Inspector 里的 `ConfigDataSo` 引用字段加一个折叠箭头，展开后内联显示被引用配置资产的内容，可以直接编辑。零第三方依赖，全是编辑器代码（**没有运行时程序集**）。

## 依赖

本包**引用核心包** `com.fang.framework`（asmdef 引用 `Fang.Framework`；挂 FangHub 页还需要 `Fang.Framework.Editor`）。

`package.json` 的 `dependencies` 是空对象 —— UPM 不支持包内声明 git URL 依赖，因此依赖关系靠 asmdef 引用 + FangHub 扩展包页的前置检查 + 本文档声明。安装本包前请先确保核心包已装。

## 安装

**方式一：FangHub 的扩展包页**（推荐）

`Tools/Fang Framework/Fang Hub` → 侧栏点「框架与扩展」→ 点「刷新」→ 点「安装」。

**方式二：手写 `Packages/manifest.json`**

```json
{
  "dependencies": {
    "com.fang.framework": "https://github.com/fangzw12138/FangFrameWork.git?path=/Packages/com.fang.framework#com.fang.framework/v0.5.1",
    "com.fang.framework.configinspector": "https://github.com/fangzw12138/FangFrameWork.git?path=/Packages/com.fang.framework.configinspector#com.fang.framework.configinspector/v0.1.0"
  }
}
```

需要本机有 git CLI。

## 用法

装完即生效，没有要打开的窗口。任何**声明类型是 `ConfigDataSo` 或其子类**的序列化字段都会多一个折叠箭头：

```csharp
public sealed class HeroWorldObject : MonoBehaviour
{
    [SerializeField] private HeroConfigDataSo _config;   // 这个字段有 ▶，展开后内联显示资产内容
    [SerializeField] private ScriptableObject _other;    // 这个字段不受影响
}
```

点 ▶（或点字段名）展开，展开区就是那个配置资产的属性，改完照常落盘；再点一次折叠。

## 生效范围与限制

| 项 | 说明 |
| --- | --- |
| 生效范围 | 声明类型是 `ConfigDataSo` 或其子类的字段；`ScriptableObject` 或其它 SO 声明类型的字段不受影响 |
| 生效位置 | 全工程：Inspector、Prefab、Scene 里的这类字段都走它 |
| 展开层数 | 最多 2 层；自引用或超出深度显示 `(circular reference)` / `(max depth reached)` 提示，不会继续往下套 |
| 关闭方式 | **没有运行期开关**，卸载本包即可恢复默认绘制 |

## 编辑器

本包带一个 FangHub 页（`Editor/ConfigInspectorPage.cs`，程序集 `Fang.Framework.ConfigInspector.Editor`）：`Tools/Fang Framework/Fang Hub` → 侧栏「配置展开」。这一页只放说明（生效范围 / 用法 / 限制），没有任何操作 —— 抽屉是全局行为，不需要面板驱动。

挂 FangHub 页的完整写法见核心包 `Documentation~/FangHub.md`。

## 设计理由

见 `Documentation~/配置展开.md`：为什么独立成包、为什么作用域收窄到 `ConfigDataSo`、为什么不做运行期开关。

## 许可证

MIT，见 `LICENSE.md`。
