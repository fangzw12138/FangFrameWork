# Changelog

## [Unreleased]

本包尚未发版：未打 tag、未注册仓库根的 `packages.json`。内容见 [0.1.0]。

## [0.1.0]

### Added

- 程序集 `Fang.Framework.FmodAudio`（全平台，`autoReferenced: true`，引用 `Fang.Framework` / `FMODUnity`）+ 编辑器程序集 `Fang.Framework.FmodAudio.Editor`（Editor，`autoReferenced: false`）+ EditMode 测试程序集 `Fang.Framework.FmodAudio.Editor.Tests`。
- `FmodAudioService : Service, ITickable`：`Initialize`（校验配置 + 在 Play 模式里主动拉起 FMOD 运行时）/ `Shutdown` / `OnDispose` / `OnTick`；可控播放（`Play` / `PlayAt` / `PlayAttached` 返回 `FmodEventHandle`）、一次性播放（`PlayOneShot` / `PlayOneShotAt` / `PlayOneShotAttached`）、`Stop` / `StopAll` / `SetParameter`、`SetMasterVolume` / `MasterVolume` / `SetChannelVolume` / `GetChannelVolume` / `SetBusVolume` / `GetBusVolume`、`MuteAll` / `PauseAll`。播放入口给 `EventReference` 与 `FmodEventPath` 对称两版。
- 配置层（无 FMOD 类型，可脱离 FMOD 单测）：`FmodAudioConfigSo`（只读）、`FmodBusChannel`、`FmodEventPath`、`FmodEventHandle`。
- 内部纯校验 `FmodConfigValidator`：空配置 / 空 channelId / channelId 重复 / 空 busPath / busPath 缺 `bus:/` 前缀 / 主总线路径。
- 编辑器：FangHub 页 `FmodAudioPage`（FMOD 状态 + 配置列表 + 校验 + 运行时快照 + `InspectorElement` 直接编辑）、`FmodAudioConfigScaffolder`（固定路径一键建配置，预填 4 条通道）、`FmodEventPathDrawer`（`⊙` 弹 FMOD 事件缓存列表，取不到时退化为纯文本框）。
- `Runtime/link.xml`：IL2CPP 保留 `FMODUnity` / `FMODUnityResonance` / `FMODUnityHaptics`（未在 IL2CPP 构建实测）。
- 文档：`README.md`、`Documentation~/FMOD音频.md`。
