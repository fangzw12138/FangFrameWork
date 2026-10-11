# FMOD 音频

`com.fang.framework.fmodaudio` 的设计理由、职责边界，以及它与 CastawaySurvival 里那份旧实现（`Assets/FangGameFrameWork/Packages/FmodAudioPackage`）的逐条差异。

参考实现已逐个文件读完，并在该工程里找到 13 处真实调用点（`EquipAbilityController` / `CharacterSFXController` / `AttackableAbilityController` / `ItemHPAbilityController` / `PlacementAbilityController` / `CastawaySurvivalEntry` / `CastawaySurvivalSystemLifetimeScope`），本包按这些调用点的实际用法定 API。

## 一、职责边界

**本包只管「把 FMOD 的事件播出去、把混音调成想要的样子」。**

| 归本包 | 不归本包 |
| --- | --- |
| 事件引用类型（配置层 FMOD-free）、通道映射与默认音量 | bank 加载与卸载（FMOD 插件自己管） |
| 播放（2D / 定点 / 挂物体）、一次性播放、停止、事件参数 | 音频资源导入、FMOD 工程构建 |
| 主音量 / 通道音量 / 总线音量 / 静音 / 暂停 | 业务音量设置（谁调 `SetChannelVolume` 是使用方的事） |
| 活动实例的句柄与回收 | 3D 衰减、混响、快照内容（都在 FMOD 工程里配） |

配置资产持有**字符串路径**（`FmodEventPath`），不持有 FMOD 类型，因此：配置层可以脱离 FMOD 单测；核心包与业务配置资产不必 `using FMODUnity`。运行时则同时接受官方 `EventReference`。

## 二、与参考实现的逐条差异

| # | 旧实现 | 问题 | 本包 |
| --- | --- | --- | --- |
| 1 | 每次 `Initialize` 都 `AddComponent<StudioListener>()` + `AddComponent<AudioListener>()`；找不到主相机直接 `return false` 阻断初始化 | 重复挂组件；没有主相机时服务完全不可用 | `GetComponent ?? AddComponent` 幂等；找不到 `Camera.main` 只记警告并继续（2D 播放不依赖监听器） |
| 2 | 用 `position != Vector3.zero` 当作「有位置」 | 在原点定点播放时漏设 3D 属性 | 显式三种播放位置：`Flat` 不设、`Position` 必调 `set3DAttributes`、`Attached` 必走 `AttachInstanceToGameObject` |
| 3 | `_activeEvents` 只增不减 | 自然播完的一次性事件永久留在字典里（句柄与实例泄漏） | 实现 `ITickable`，`OnTick` 把 `PLAYBACK_STATE.STOPPED` 的实例 `release()` 并移除（代次表 + 复用缓冲，不产生每帧垃圾） |
| 4 | `ActiveEvent.IsSnapshot` 字段 + `PlaySnapshot()` 只置字段不做事 | 死字段 / 死方法 | 全删。FMOD 的快照本身就是 event，`Play` 即可 |
| 5 | `MuteAll` 走 `RuntimeManager.MuteAllEvents`，同时 `_muted` 又参与 `ApplyMasterVolume` | 两套静音语义叠加（把主音量写成 0 会丢掉用户音量） | 静音只走 `RuntimeManager.MuteAllEvents`，音量接口不再叠加静音 |
| 6 | `GetVcaVolume` / `SetVcaVolume` | 无调用方 | 删 |
| 7 | `SetParameter(handle, PARAMETER_ID, value)` 重载 | 无调用方 | 删，只留 by-name |
| 8 | `IsInitialized` / `IsMuted` / `ActiveEventCount` 公开属性 | 业务无调用方 | 从运行时公开面移除；编辑器页走反射读私有字段（沿用 `EventBusPage` 先例：不为了一个只读观察器给运行时开公开 API）。静音状态改读 FMOD 自己的 `RuntimeManager.IsMuted` |
| 9 | `FMODEventPath` + `FMODEventPathDrawer` | 类型（配置层 FMOD-free）有保留价值，抽屉的选择器体验也要留 | 类型保留并更名 `FmodEventPath`；抽屉重写进本包 Editor 程序集，事件缓存不可用时退化为纯文本框 |
| 10 | `Initialize` 先全量校验、后提交状态 | 顺序是对的 | 保留，并把校验抽成纯逻辑 `FmodConfigValidator`（可脱离 FMOD 单测） |
| 11 | 配置写入口 | — | 配置资产对外零写入；Editor 侧一律 `SerializedObject` / `SerializedProperty` |
| 12 | 无 link.xml | IL2CPP 裁剪会砍掉 FMOD 的反射面 | 包内放 `Runtime/link.xml`（保留 `FMODUnity` / `FMODUnityResonance` / `FMODUnityHaptics`）；**未实测**，见第四节 |

### 2.1 参考实现里的一个真实缺陷：Stop 会切掉淡出

旧实现在 `Stop` / `StopAll` 里 `stop(ALLOWFADEOUT)` 之后**立刻** `release()`。FMOD 的 `release()` 是「标记释放」，但紧接着释放正在淡出的实例会切掉淡出尾巴 —— `ALLOWFADEOUT` 就等于白写。

本包的 `Stop` 只调 `stop(mode)`，实例留在活动表里；淡出结束、`getPlaybackState` 变成 `STOPPED` 后由 `OnTick` 统一 `release()` 并移除。`allowFadeout: false` 传 `IMMEDIATE`，下一帧回收。`Shutdown` 仍然是「`IMMEDIATE` + `release()`」同步清空（退出时不需要淡出）。

### 2.2 校验顺序与「FMOD 运行时怎么起来」

`Initialize` 的顺序是：

```text
Shutdown()  →  纯配置校验  →  确认在 Play 模式并把 FMOD 运行时拉起来  →  挂监听器  →  提交映射表  →  应用音量
```

旧实现把「主相机 / RuntimeManager 是否就绪」放在配置校验之前，配置写错了也只会看到环境错误。现在纯校验排在最前，配置问题永远报得出真正的原因。

第二道闸门这里踩过一个真实的坑，值得记下来：

- **`RuntimeManager.IsInitialized` 不能当「FMOD 是否可用」的判据。** FMOD 的 `RuntimeManager` 是惰性初始化的 —— 只有第一次有人去读 `RuntimeManager.Instance`（建立 `FMOD.UnityIntegration.RuntimeManager` 物体、初始化 Studio System、按设置加载 bank）之后，`IsInitialized` 才会变成 `true`。在没人播过声音的工程里，这个值会一直是 `false`。
- 旧实现用 `if (!RuntimeManager.IsInitialized) return false;` 当闸门，等于要求「必须有别人先把 FMOD 拉起来」；新工程／只装了音频包的工程会永远初始化不了。实测：在 Play 模式、什么都没播过时 `IsInitialized == false`，服务直接拒绝启动。
- 本包改成：**Play 模式**是硬条件（编辑器里 FMOD 会直接报错说不要在编辑器里用 `RuntimeManager`），然后由 `Initialize` 主动读一次 `RuntimeManager.StudioSystem.isValid()` 把运行时拉起来（`EnsureRuntimeStarted()`），再把异常与「拉起来之后仍然不就绪」都报到调用方。实测修好后：`Initialize` 返回 `true`，`IsInitialized` 从 `false` 变 `true`，音量与静音接口全部生效。

顺带一个由此而来的结论：**总线与事件只有在其定义所在的 bank 被加载后才解析得到**。工程没指对 FMOD 工程 / bank 没进构建时，服务照样「初始化成功」，只是每个总线写音量会得到 `Bus not found` 警告、每个事件会得到 `Event not found` 错误。这是 FMOD 侧的事实，不是本包的缺陷；所以 FangHub 页的校验区也把「总线是否存在只能运行时判定」写在明处。

### 2.3 比 `FmodConfigValidator` 原计划多出的两条规则

计划里列的校验是「空引用 / 空 channelId / 重复 channelId / 空 busPath」。实现时多加了「bus 路径必须以 `bus:/` 开头」与「主总线路径非空且带前缀」两条，理由是：编辑期根本拿不到 FMOD 的总线列表（见第三节），格式校验是编辑器侧唯一能替「总线是否存在」做的事；而写错的 bus 路径在运行时只会得到一句 `Bus not found` 警告，代价高。两条规则都不会误伤合法路径。

## 三、编辑器侧能校验到什么程度

FMOD 的编辑器缓存（`FMODUnity.EventManager` / `EventCache`）只暴露 **事件 / bank / 参数**，**不暴露总线列表**。所以：

| 想校验的东西 | 编辑期能不能 | 怎么做 |
| --- | --- | --- |
| 配置一致性（空 id、重复 id、空路径、前缀） | ✅ | `FmodConfigValidator`（纯逻辑，已单测） |
| 事件路径是否存在 | ✅（选的时候） | `FmodEventPath` 字段上的 `⊙` 从 `EventManager.Events` 里选，选中即存在 |
| 总线路径是否存在 | ❌ | 只能在 FMOD 运行时由 `RuntimeManager.GetBus` 判定 |

页面不假装能做到做不到的事：校验区会把上面这句话直接写出来，而不是显示一个「通过」骗人。

配置资产本身**不含事件路径**（事件路径在业务自己的 `XxxConfigDataSo` 里），所以页面也没有「扫描全工程事件路径」这种重活儿。

## 四、link.xml

`Runtime/link.xml` 保留 `FMODUnity` / `FMODUnityResonance` / `FMODUnityHaptics` 三个程序集，防 IL2CPP 裁剪砍掉 FMOD 的反射面（FMOD 用反射查找 `EventHandler` / `StudioEventEmitter` 的子类）。

**没实测过。** 验证它需要跑一次 IL2CPP 构建再看裁剪报告，本轮环境不具备。如果将来实测发现包内 link.xml 不被采集，退化方案是在工程 `Assets/` 下另放一份同样的 link.xml（Unity 对工程内的 link.xml 采集是确定的）。

本包**没有**做「页面上按钮一键生成工程级 link.xml」：在没证实包内那份无效之前，那是个没有调用方的功能。

## 五、FMOD 依赖

asmdef **直引** `FMODUnity` / `FMODUnityEditor`，不做 `#if` 门控、不自动写宏。理由：装了本包就是要用 FMOD，门控只会把「FMOD 没装」这个明确错误延后成运行时的一堆空实现。代价是没装 FMOD 的工程装本包会 `CS0246`，已在 `package.json` 描述、`README.md` 与 FangHub 页三处标注「需要 FMOD for Unity 2.03+」。

顺带一个容易踩的点：FMOD 插件自己的 `FMODUnity.asmdef` 引用了 `Unity.Addressables` / `Unity.ResourceManager`，所以工程必须装 `com.unity.addressables`，否则 FMOD 插件本身编译不过。FMOD 代码里用到 Addressables 的地方都带 `#if UNITY_ADDRESSABLES_EXIST`，代码是安全的，挂掉的是 asmdef 引用解析。

## 六、迁移（从 CastawaySurvival 的旧包）

| 旧 | 新 |
| --- | --- |
| 命名空间 `FangGameFrameWork.Packages.FmodAudioPackage` | `Fang.Framework.FmodAudio` |
| `FmodAudioService : Service, IDisposable` | `FmodAudioService : Service, ITickable`（`Dispose()` → `OnDispose()` override） |
| `FmodAudioProjectConfigSo` / `FmodBusMapping` / `FMODEventPath` | `FmodAudioConfigSo` / `FmodBusChannel` / `FmodEventPath` |
| `PlayEvent` / `PlayEventAt` / `PlayEventAttached` | `Play` / `PlayAt` / `PlayAttached` |
| `GetMasterVolume()` | `MasterVolume` 属性 |
| `MuteAll` 叠加 `_muted` | `MuteAll` 只走 FMOD |
| `Dispose()` | `OnDispose()`（框架驱动） |

序列化字段名也变了（旧的是无前缀 `channelId` / `busPath` / `defaultVolume`，新的是 `_channelId` / `_busPath` / `_defaultVolume`），旧配置资产需要重建 —— 新包用 FangHub 页的「新建配置」一键重建最快。

业务调用点基本是原样替换：`PlayOneShotAt(EventReference, Vector3)`、`PlayOneShotAttached(EventReference | string, GameObject)`、`SetMasterVolume`、`SetChannelVolume("bgm" | "sfx", float)` 的签名都没变，`FmodEventPath.Path` 也还在，所以 `_audio.PlayOneShotAttached(config.AttackSound.Path, go)` 这种写法换成新类型就行。

## 七、本轮的验证记录

| 项 | 结果 |
| --- | --- |
| `unity_editor.refresh` | 0 error / 0 warning |
| EditMode 测试 | 本包 43 项全绿；全工程 EditMode 552 项全绿（无回归） |
| FangHub 页被发现 | `id=framework-fmod-audio`、`title=FMOD 音频`、`category=FMOD`、`order=0` |
| 一键新建配置 | 生成 `Assets/FangFramework/FmodAudio/FmodAudioConfig.asset`，预填 `bgm→bus:/Music(0.7)` / `sfx→bus:/SFX(1)` / `ui→bus:/UI(1)` / `voice→bus:/Voice(1)`，主总线 `bus:/`、默认主音量 1 |
| 页面校验 | 故意把 `masterBusPath` 写成 `Master`、把第二条通道 id 改成重复的 `bgm`：列表行标记与校验区都出现 `✗ channelId 重复：bgm。`；改回后恢复 ✓ |
| `FmodEventPath` 抽屉 | 事件缓存为空时不显示 `⊙`，退化为纯文本框（当前工程没有 FMOD 事件列表，这是预期路径） |
| Play 模式运行时 | `Initialize` 返回 `true`；`RuntimeManager.IsInitialized` 由 `false` 变 `true`；`MasterVolume 1→0.5`；`GetChannelVolume("bgm") 0.7→0.25`；`MuteAll(true)` 后 `RuntimeManager.IsMuted==true`；`PauseAll` 往返正常；不存在的路径只记 `Event not found` 不抛异常 |
| 监听器幂等 | 主相机上 `StudioListener` / `AudioListener` 各 0→1，连续 `Initialize` 两次仍是 1 |
| 页面运行时快照 | 显示「已初始化=是 / 主音量 0.50 / 通道 bgm 0.25、sfx 1.00、ui 1.00、voice 1.00 / 活动实例 0」 |

**没验证到的部分（需要人工在有 FMOD 工程的工程里做）**：

- 真实事件的播放、停止、淡出、参数、快照 —— 本机没有 FMOD 工程与 bank，`CreateInstance` / `GetBus` 全都会 `not found`。
- `Runtime/link.xml` 是否被 IL2CPP 采集 —— 需要一次 IL2CPP 构建 + 裁剪报告。

## 八、本轮的本地验证装置（不进仓库）

`Assets/` 被 `.gitignore` 忽略，所以下面这些都只是本机验证用，不进仓库：

- 从 CastawaySurvival 拷来的 FMOD for Unity 2.03.19 插件 → `Assets/Plugins/FMOD`（`addons/Haptics` 没拷，它引用了本工程没有的 `Unity.XR.OpenXR`）；`FMODStudioSettings.asset` 里的 bank 列表与 `sourceBankPath` 已清空（否则运行时一直刷 bank not found）。
- `Packages/manifest.json` 补了 `com.unity.addressables`（FMOD 插件自己的 asmdef 引用了 `Unity.Addressables` / `Unity.ResourceManager`）。manifest 不跟踪。
- 一键新建配置的产物 `Assets/FangFramework/FmodAudio/FmodAudioConfig.asset`。
