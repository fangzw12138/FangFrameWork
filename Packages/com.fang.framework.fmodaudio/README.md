# Fang Framework · FMOD Audio

`com.fang.framework.fmodaudio` —— Fang Framework 的 FMOD Studio 音频扩展包。

配置资产 FMOD-free（只存路径字符串），运行时完整保留可控播放、混音与静音能力。

## 依赖

本包**硬依赖两样东西**，缺一个就编译不过：

| 依赖 | 版本 | 为什么 |
| --- | --- | --- |
| `com.fang.framework`（核心包） | 0.5.1+ | `Service` / `ITickable` / `ConfigDataSo` |
| FMOD for Unity（`FMODUnity` / `FMODUnityEditor` 程序集） | **2.03+** | asmdef 直引 `FMODUnity` / `FMODUnityEditor`，不做 `#if` 门控、不自动写宏 |
| `com.unity.addressables` | — | FMOD 插件自己的 `FMODUnity.asmdef` 引用了 `Unity.Addressables` / `Unity.ResourceManager` |

`package.json` 的 `dependencies` 是空对象 —— UPM 不支持包内声明 git URL 依赖，也不该替工程决定 FMOD 的安装方式。依赖关系靠 asmdef 引用 + 本文档声明。

**没装 FMOD 的工程装本包会直接 `CS0246`（找不到 `FMODUnity`），这是已知并接受的取舍。**

## 安装

FMOD for Unity 2.03+ 先导入到工程（Unity 的 `FMOD/FMOD for Unity` 包或 FMOD 官网的 `.unitypackage` 都会把插件放进 `Assets/Plugins/FMOD`），确认 `Assets/Plugins/FMOD/Resources/FMODStudioSettings.asset` 存在。

本包尚未发版（未打 tag、未注册仓库根的 `packages.json`），暂时按内嵌包使用：把 `Packages/com.fang.framework.fmodaudio` 放进工程即可。

## 用法

### 1. 建配置

`Tools/Fang Framework/Fang Hub` → 侧栏「FMOD 音频」→ 点「新建配置」。会在固定路径 `Assets/FangFramework/FmodAudio/FmodAudioConfig.asset` 建一份，预填 4 条通道：

| channelId | busPath | 默认音量 |
| --- | --- | --- |
| `bgm` | `bus:/Music` | 0.7 |
| `sfx` | `bus:/SFX` | 1.0 |
| `ui` | `bus:/UI` | 1.0 |
| `voice` | `bus:/Voice` | 1.0 |

通道映射与默认音量都能在页面右侧的 Inspector 里改。总线路径必须落在 FMOD 工程里真实存在（默认值对应 FMOD 默认工程的 Music / SFX / UI / Voice 总线，名字不同就改成自己的）。

### 2. 注册服务并初始化

```csharp
public sealed class SystemScope : Scope
{
    public override void OnInit()
    {
        base.OnInit();
        AddService<FmodAudioService>();
    }
}
```

```csharp
var service = Scope.GetService<FmodAudioService>();
service.Initialize(config);
```

`Initialize` 会：清空上一次的状态 → 全量校验配置 → 把 FMOD 运行时拉起来（**只在 Play 模式可用**）→ 幂等挂 `StudioListener` + `AudioListener` 到主相机 → 提交通道映射 → 应用主音量与通道音量。

校验不过、不在 Play 模式、或 FMOD 运行时起不来时返回 `false`，并保持「未初始化」状态（不会留下半套映射表）。

两个容易踩的点：

- **FMOD 的 `RuntimeManager` 是惰性初始化的**：在它被第一次使用之前，`RuntimeManager.IsInitialized` 一直是 `false`。所以本包在 `Initialize` 里主动读一次 `RuntimeManager.StudioSystem` 把运行时拉起来，而不是等别人先初始化 —— 否则「只装了本包、还没播过任何声音」的工程会永远起不来。
- **只在 Play 模式**：编辑器里 `RuntimeManager` 会直接报错说「不要在编辑器里用它」。所以 `Initialize` 在编辑模式一律返回 `false`（记一条 Error）。

### 3. 播放

```csharp
service.PlayOneShotAt(config.HitSound, worldPosition);
service.PlayOneShotAttached(config.SwingSound, WorldObject.gameObject);
service.PlayOneShot(new FmodEventPath("event:/UI/Click"));
```

业务侧 config 里用 `FmodEventPath` 字段（Inspector 上带 `⊙` 按钮，从 FMOD 事件缓存里选）；代码里想直接用官方 `EventReference` 也行 —— 播放入口两种都有。

可控播放（需要停止 / 改参数 / 播放快照）用返回句柄的版本：

```csharp
var handle = service.PlayAt(musicPath, position);
service.SetParameter(handle, "Intensity", 0.5f);
service.Stop(handle);
```

### 4. 混音

```csharp
service.SetMasterVolume(settings.MasterVolume);
service.SetChannelVolume("bgm", settings.MusicVolume);
service.SetChannelVolume("sfx", settings.SFXVolume);
service.MuteAll(settings.Muted);
```

### 5. 驱动 Tick

`FmodAudioService` 实现 `ITickable`：`Scope.Tick` 会清掉自然播完（`PLAYBACK_STATE.STOPPED`）的实例并 `release()`，避免句柄泄漏。宿主记得按框架约定调 `Scope.Tick`。

## 编辑器

`Tools/Fang Framework/Fang Hub` → 侧栏「FMOD 音频」。页面分三块：

- **FMOD 状态**：插件版本（`FMOD.VERSION.number`）、设置资产路径、事件缓存事件数、运行时是否已初始化。
- **配置**：左列表选配置，右侧看基本信息、通道表、校验结果、运行时快照（主音量 / 通道音量 / 静音 / 活动实例与播放状态），并用 `InspectorElement` 直接改配置。
- **校验**：配置一致性（通道 id 非空不重复、bus 路径带 `bus:/` 前缀）+ 主总线路径格式。总线与事件是否真实存在只能在 FMOD 运行时判定。

`FmodEventPath` 字段在 Inspector 上是「文本框 + `⊙`」：`⊙` 弹出 FMOD 事件缓存里的事件列表，选一个回填路径；事件缓存取不到列表时退化成纯文本框，可以直接手填。

## API

### `FmodAudioService : Service, ITickable`

| 成员 | 说明 |
| --- | --- |
| `bool Initialize(FmodAudioConfigSo config)` | 校验配置并把 FMOD 运行时拉起来（仅 Play 模式）；失败返回 `false` 且不改状态 |
| `void Shutdown()` | 立即停止并 `release()` 全部活动实例、清空映射表、服务代次自增；可重复调用 |
| `void OnDispose()` | 由 `Scope` 驱动，等价于 `Shutdown()` |
| `void OnTick(float deltaTime)` | 由 `Scope.Tick` 驱动，回收已停止的实例 |
| `float MasterVolume { get; }` | 当前主音量 |
| `FmodEventHandle Play(EventReference)` / `Play(FmodEventPath)` | 2D 播放，返回句柄 |
| `FmodEventHandle PlayAt(EventReference, Vector3)` / `PlayAt(FmodEventPath, Vector3)` | 固定位置播放 |
| `FmodEventHandle PlayAttached(EventReference, GameObject)` / `PlayAttached(FmodEventPath, GameObject)` | 挂在物体上播放（跟随移动） |
| `void PlayOneShot(EventReference)` / `PlayOneShot(FmodEventPath)` | 2D 一次性 |
| `void PlayOneShotAt(EventReference, Vector3)` | 位置一次性 |
| `void PlayOneShotAt(EventReference, Vector3, float volume)` | 位置一次性 + 单次音量 |
| `void PlayOneShotAt(FmodEventPath, Vector3)` | 位置一次性（路径版） |
| `void PlayOneShotAttached(EventReference, GameObject)` / `PlayOneShotAttached(FmodEventPath, GameObject)` | 挂物体一次性 |
| `void Stop(FmodEventHandle, bool allowFadeout = true)` | 停止单个实例；`allowFadeout` 为真时等淡出结束再释放 |
| `void StopAll(bool allowFadeout = true)` | 停止全部活动实例 |
| `void SetParameter(FmodEventHandle, string name, float value)` | 按名字写事件参数 |
| `void SetMasterVolume(float)` | 主音量，`[0,1]` 钳位 |
| `void SetChannelVolume(string channelId, float)` | 按通道 id 写总线音量；未登记的 id 只记警告 |
| `float? GetChannelVolume(string channelId)` | 未登记的 id 返回 `null` |
| `void SetBusVolume(string busPath, float)` | 直接写任意总线音量（不要求登记为通道） |
| `float? GetBusVolume(string busPath)` | 总线不存在返回 `null` |
| `void MuteAll(bool)` | 走 `RuntimeManager.MuteAllEvents` |
| `void PauseAll(bool)` | 走 `RuntimeManager.PauseAllEvents` |

一次性播放入口是「立即 `release()`」的 fire-and-forget：不返回句柄，不需要 Tick 回收。

### `FmodEventHandle`

| 成员 | 说明 |
| --- | --- |
| `static FmodEventHandle Invalid` | 无效句柄（`InstanceId == 0`） |
| `int InstanceId` | 服务内的实例序号 |
| `int ServiceGeneration` | 服务代次；跨 `Shutdown` / `Initialize` 的旧句柄会被忽略 |
| `bool IsValid` | `InstanceId > 0` |

### 配置

| 类型 | 成员 | 说明 |
| --- | --- | --- |
| `FmodAudioConfigSo : ConfigDataSo` | `float DefaultMasterVolume` | `[0,1]` 钳位，默认 1 |
| | `string MasterBusPath` | 默认 `bus:/` |
| | `IReadOnlyList<FmodBusChannel> BusChannels` | 通道映射 |
| `FmodBusChannel` | `string ChannelId` / `string BusPath` / `float DefaultVolume` | 一条通道映射，`DefaultVolume` `[0,1]` 钳位 |
| `FmodEventPath` | `FmodEventPath()` / `FmodEventPath(string)` / `string Path` / `bool IsValid` | FMOD-free 的事件路径配置类型 |

配置资产**只读**：没有任何 setter。Editor 侧写它走 `SerializedObject` / `SerializedProperty` + `EditorUtility.SetDirty`。

### 编辑器类型

| 类型 | 说明 |
| --- | --- |
| `FmodAudioConfigScaffolder` | `static string AssetPath` / `static FmodAudioConfigSo Load()` / `static FmodAudioConfigSo Create()`；固定路径建配置并预填 4 条通道，已存在时返回它而不是覆盖 |
| `FmodEventPathDrawer` | `FmodEventPath` 的属性抽屉 |
| `FmodAudioPage` | FangHub 页 |

## 边界

- 不做：VCA、快照专用 API（FMOD 的快照本身就是 event，`Play` 即可）、按 `PARAMETER_ID` 写参数、音频可视化、bank 加载（FMOD 插件自己管）。
- 总线与事件只有在其定义所在的 bank 已加载时才解析得到。工程没指对 FMOD 工程 / bank 没进构建时，只会得到 `Bus not found` / `Event not found` 警告与错误日志，不会崩 —— 音量写不进去、事件播不出来，但服务状态是自洽的。
- 不销毁 GameObject（框架约束）：服务物体留给使用方或 Unity 层级回收。
- 主线程限定，与核心包一致。
- IL2CPP 裁剪：包内带 `Runtime/link.xml` 保留 `FMODUnity` / `FMODUnityResonance` / `FMODUnityHaptics`。**这一条尚未在 IL2CPP 构建里实测过**；如果实测发现包内 link.xml 不被采集，需要在工程 `Assets/` 下另放一份 link.xml。

设计理由、与参考实现的差异、迁移说明见 `Documentation~/FMOD音频.md`。

## 许可证

MIT，见 `LICENSE.md`。
