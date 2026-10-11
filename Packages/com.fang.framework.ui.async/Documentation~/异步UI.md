# 异步 UI（`com.fang.framework.ui.async`）

本文记**设计理由**与**踩过的坑**。代码里不写解释性注释，理由都放这里。

## 1. 为什么挂在异步族，而不是核心同步族

`UIService` 是 `AsyncService`，面板控制器是 `AsyncController<TConfig, TData>`。

理由：面板的打开常常要等东西 —— 资源、数据、过场。同步族里这类等待只能靠回调或状态机；异步族里它就是 `await`。代价是 UI 的整条链都必须在异步 scope 树里，所以 `AddService<T>` / `GetService<T>` 的约束是 `IAsyncLifecycle`，核心 `Service` 装不进去 —— 两种 scope **不混用**，这也是本包不依赖 `com.fang.framework.ui` 的原因。

对外只暴露 `UIService`；`UIPanelStack` / `UILayerResolver` / `UICanvasPool` / `UIPanelFactory` 都是 `internal` 的纯逻辑或纯建物工具，不对外承诺。

## 2. 为什么配置里不写类型名

面板的「控制器类型」由调用方在 `OpenAsync<TPanel, TConfig, TData>()` 的泛型参数里给，配置资产里只存 `Id` / 层 / 排序 / 预制体或 UXML。

理由：配置里写类型名就要反射（`Type.GetType`），而反射在 IL2CPP 下会被裁剪、在包升级时会静默失效。泛型参数是编译期绑定，改错了编译不过。代价是调用点要多写几个类型参数 —— 这是有意的取舍：**错误发生在编译期，而不是运行期的某次打开面板**。

配置资产因此对外**零写入口**（只有只读属性），编辑器侧的写入统一走 `SerializedObject`，让 Unity 自己管脏标记与撤销。

## 3. 面板栈与层排序

`UIPanelStack` 是纯 C# 逻辑（不含 Unity 类型），可以脱离 Unity 单测。

- **栈**：`PushAsync` / `PopAsync` / `PopTopAsync` / `PopAllAsync`。栈只记「谁开着」，实体是调用方持有的 `IUIPanel`。
- **排序号**：`层.BaseSortOrder + 面板.SortOrder`。UGUI 轨按排序号从 `UICanvasPool` 取一个画布（同排序号复用同一块），UITK 轨全部挂在同一个 `UITK` 根下。每层一个 `Canvas`，是为了让「层之间」的遮挡关系由 `sortingOrder` 决定，而不是靠 `transform` 顺序 —— 后者会被 UGUI 的层级刷新打乱。
- **`ClosePreviousOnOpen`**：开关型面板（设置、背包）用同一份配置反复开关时，不必自己维护「先关旧的」。
- **`BlocksInput`**：栈内维护一个阻塞计数，进栈时 `+1`、出栈时 `-1`，计数从 0 变正 / 从正变 0 时发 `InputBlockingPanelOpened` / `Closed`。使用方订阅这两个事件去关输入 —— UI 不该直接知道输入系统长什么样。

## 4. 为什么 `UIService` 不销毁面板物体

`CloseAsync` 只做出栈 + `OnCloseAsync` + 隐藏，实例留在 `_openedPanels` 里等下次复用。

理由：UI 面板的**建物**（`Instantiate`、UITK 建树、控件查询）比**显隐**贵得多，而开关面板是高频动作。销毁只发生在 `UIService` 自身被 `DisposeAsync` 收尾时（`PopAllAsync` + 逐个 `DisposeAsync`）。

代价是内存常驻 —— 面板少而开关频繁时是划算的；面板多而几乎不复用时，应该改用「每次新建」的策略，那是另一套 API，不在本包范围内。

## 5. 收尾：每一步各自兜底

`UIService.OnDisposeAsync` 里的每一步都包在 `SafeAsync` 里，异常只记 `LogWarning`，然后继续收尾。

为什么必须这样：退出 Play 时 Unity 会**先拆掉整棵 UI 树**，我们的收尾链随后才回来。这时几乎所有面板对象都已经销毁，逐个 `DisposeAsync` 都可能抛。若不兜底：

1. 异常会从 `OnDisposeAsync` 跑出去，变成 Unobserved 刷屏；
2. 更糟的是**剩下的收尾全被跳过** —— 一块坏面板会掐掉后面所有的清理。

所以收尾的语义是「尽力而为，绝不中断」：一块坏面板不许把后面的收尾全掐掉。

## 6. 面板对象已被销毁时的行为

Unity 对象被销毁后，C# 引用还在，但 `== null` 为 `true`（重载过的 `==`）。

- `UIPanelController.OpenAsync` / `SetVisible` / `SetVisibleCore` 都先判 `if (this == null) return;`。不判的后果：取 `gameObject` 会抛 `MissingReferenceException`，而这条路径正好在**退出 Play / 拆 UI 树**时被走到。
- `CloseAsync` 在对象已销毁时**仍然**把 `Data.MarkClosed()` 打上 —— `Data` 是普通 C# 对象，还读得到。调用方的状态判断因此不会永远停在「开着」。
- `UIService.OpenAsync` 查 `_openedPanels` 时用 `UIPanelStack.IsAlive` 过滤：引用还在字典里但对象已销毁，就当它没开过，重建一块。
- `Describe(panel)` 取 `ConfigId` 前先判存活，给一个可读替身 —— 报表代码绝不因为取 id 再炸一次。

## 7. 异步顺序的不对称（重要）

`AsyncLifecycleBehaviour` 的顺序是不对称的：先置 `IsInitialized = true`，再 `TrySetResult`，**最后**才发 `InitCompleted`。而 UniTask 在 `TrySetResult` **内部同步恢复** awaiter。

结论：**不要在 `await` 之后立刻读状态做判断**。`await` 返回时，`InitCompleted` 事件可能还没发出去，视觉订阅方（加载遮罩、进度条）拿到事件时要自己判断「事件带的根是不是当前根」。这条不是本包的实现细节，是异步族的既有事实，写在这里免得下次又被咬。

## 8. 测试替身为什么单独一个程序集

`Fang.Framework.UI.Async.TestDoubles` 是**非 Editor** 程序集（`includePlatforms` 为空），而 `Fang.Framework.UI.Async.Tests` 是 Editor-only。

理由：Editor 程序集里**不能 `AddComponent`** —— 而测试面板要往 GameObject 上挂控制器。所以「测试用的面板控制器」必须放在一个全平台的程序集里，测试程序集再引用它。

测试用 `[UnityTest] IEnumerator` + `UniTask.ToCoroutine(null)` + `yield return`；EditMode 里销毁物体必须 `Application.isPlaying ? Destroy : DestroyImmediate`。

## 9. 画布配置为什么延迟取值

`UICanvasPool` 不缓存 `CanvasScaler` / `CanvasRenderSettings`，而是在**每次建画布时**调用 `UIService` 给的取值委托。

理由：建池发生在 `UIService.OnInitAsync`，而 `Configure(UIProjectConfigDataSo)` 是之后由使用方调的。若在构造时快照，池里拿到的永远是不带项目配置的默认值。

## 10. 画布渲染模式：Overlay 与 ScreenSpaceCamera

区别不在观感尺寸，而在**谁来画这块 UI**：

- `ScreenSpaceOverlay`：直接铺在屏幕上，不进任何相机的颜色目标，因此不受相机（正交/透视、位置、裁剪面）与后处理（Bloom、像素化、色板量化……）影响。
- `ScreenSpaceCamera`：由 `worldCamera` 那台相机渲染，**会进相机的颜色目标**，因此会被该相机的后处理一起处理，也会被比画布平面更近的场景物体遮挡。

回退规则：任何解析不到相机的情况（模式配了相机但场景里没有这台相机）一律退回 `ScreenSpaceOverlay`，并记一条 `LogWarning`。理由：相机模式下没有 `worldCamera` 的画布**什么都不画** —— 「点了开面板却一片空白」是最难查的一类现场。

`planeDistance` 会被夹到相机的 `[nearClipPlane + 0.01, farClipPlane - 0.01]`：出界会被整块裁掉，比被遮挡更难察觉。

## 11. 面板预制体为什么延后补建

`UIPanelPrefabQueue` 不在脚手架创建请求里立刻建预制体，而是等编辑器空闲、且连续稳定若干帧（`_idleFrames >= 10`）后才补。

理由：编译 / 资产导入期间 `AssetDatabase` 还没稳定，此时写资产会出现「预制体文件建出来了，但配置资产的 `_prefab` 回填没落盘」（实测复现）。失败会重试，超过 `MaxAttempts` 才放弃并记 `LogWarning`。

## 12. 脚手架绝不覆盖已有文件

`UIPanelScaffolder` 写脚本 / UXML / USS 一律先判存在（`TryWriteText`），已存在就跳过并把路径写进 `UIPanelScaffoldResult`；`CreatePrefab` 遇到已存在的预制体同样不覆盖，只把引用回填到配置。

理由：「面板配置已存在」那道守卫只挡得住**同一块面板再生成一次**。真正会丢代码的是另一条路径 —— 配置资产被删掉（或从没建成功）而手写脚本还在，此时若直接 `File.WriteAllText`，手写控制器会被脚手架静默冲掉。开发者常做的事（先手写控制器、再回头补配置）正好落在这条路径上。

代价：脚本已存在但内容不合脚手架预期时不会自动纠正 —— 由列表页的「完整性」与「校验」显示缺失项，人自己决定删还是改。

