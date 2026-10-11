using System;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

namespace Fang.Framework.FmodAudio
{
    public sealed class FmodAudioService : Service, ITickable
    {
        private sealed class ActiveEvent
        {
            public EventInstance Instance;
            public EventReference EventRef;
        }

        private enum PlayLocation
        {
            Flat,
            Position,
            Attached,
        }

        private readonly Dictionary<int, ActiveEvent> _activeEvents = new Dictionary<int, ActiveEvent>();
        private readonly Dictionary<string, FmodBusChannel> _channelsById = new Dictionary<string, FmodBusChannel>(StringComparer.Ordinal);
        private readonly Dictionary<string, float> _channelVolumes = new Dictionary<string, float>(StringComparer.Ordinal);
        private readonly List<int> _finishedInstances = new List<int>();

        private FmodAudioConfigSo _config;
        private float _masterVolume = 1f;
        private int _nextInstanceId = 1;
        private int _serviceGeneration;
        private bool _initialized;

        public float MasterVolume => _masterVolume;

        public bool Initialize(FmodAudioConfigSo config)
        {
            Shutdown();

            if (!FmodConfigValidator.TryValidate(config, out var error))
            {
                Debug.LogError("[FmodAudio] Initialize failed: " + error);
                return false;
            }

            if (!TryStartRuntime(out var runtimeError))
            {
                Debug.LogError("[FmodAudio] Initialize failed: " + runtimeError);
                return false;
            }

            AttachListenerToMainCamera();

            _config = config;
            _masterVolume = config.DefaultMasterVolume;

            var channels = config.BusChannels;
            for (var i = 0; i < channels.Count; i++)
            {
                var channel = channels[i];
                _channelsById[channel.ChannelId] = channel;
                _channelVolumes[channel.ChannelId] = channel.DefaultVolume;
            }

            _initialized = true;

            ApplyMasterVolume();
            ApplyChannelVolumes();

            return true;
        }

        public void Shutdown()
        {
            _initialized = false;
            _serviceGeneration++;

            foreach (var active in _activeEvents.Values)
            {
                if (!active.Instance.isValid())
                {
                    continue;
                }

                active.Instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                active.Instance.release();
            }

            _activeEvents.Clear();
            _channelsById.Clear();
            _channelVolumes.Clear();
            _config = null;
            _masterVolume = 1f;
        }

        public override void OnDispose()
        {
            Shutdown();
        }

        public void OnTick(float deltaTime)
        {
            if (_activeEvents.Count == 0)
            {
                return;
            }

            foreach (var pair in _activeEvents)
            {
                var instance = pair.Value.Instance;
                if (!instance.isValid())
                {
                    _finishedInstances.Add(pair.Key);
                    continue;
                }

                instance.getPlaybackState(out var state);
                if (state != PLAYBACK_STATE.STOPPED)
                {
                    continue;
                }

                instance.release();
                _finishedInstances.Add(pair.Key);
            }

            for (var i = 0; i < _finishedInstances.Count; i++)
            {
                _activeEvents.Remove(_finishedInstances[i]);
            }

            _finishedInstances.Clear();
        }

        public FmodEventHandle Play(EventReference eventReference)
        {
            return PlayCore(eventReference, PlayLocation.Flat, Vector3.zero, null, false, 1f);
        }

        public FmodEventHandle Play(FmodEventPath eventPath)
        {
            return TryResolve(eventPath, out var eventReference)
                ? Play(eventReference)
                : FmodEventHandle.Invalid;
        }

        public FmodEventHandle PlayAt(EventReference eventReference, Vector3 position)
        {
            return PlayCore(eventReference, PlayLocation.Position, position, null, false, 1f);
        }

        public FmodEventHandle PlayAt(FmodEventPath eventPath, Vector3 position)
        {
            return TryResolve(eventPath, out var eventReference)
                ? PlayAt(eventReference, position)
                : FmodEventHandle.Invalid;
        }

        public FmodEventHandle PlayAttached(EventReference eventReference, GameObject target)
        {
            if (target == null)
            {
                Debug.LogError("[FmodAudio] PlayAttached failed: the target is null.");
                return FmodEventHandle.Invalid;
            }

            return PlayCore(eventReference, PlayLocation.Attached, Vector3.zero, target, false, 1f);
        }

        public FmodEventHandle PlayAttached(FmodEventPath eventPath, GameObject target)
        {
            return TryResolve(eventPath, out var eventReference)
                ? PlayAttached(eventReference, target)
                : FmodEventHandle.Invalid;
        }

        public void PlayOneShot(EventReference eventReference)
        {
            PlayCore(eventReference, PlayLocation.Flat, Vector3.zero, null, true, 1f);
        }

        public void PlayOneShot(FmodEventPath eventPath)
        {
            if (TryResolve(eventPath, out var eventReference))
            {
                PlayOneShot(eventReference);
            }
        }

        public void PlayOneShotAt(EventReference eventReference, Vector3 position)
        {
            PlayCore(eventReference, PlayLocation.Position, position, null, true, 1f);
        }

        public void PlayOneShotAt(EventReference eventReference, Vector3 position, float volume)
        {
            PlayCore(eventReference, PlayLocation.Position, position, null, true, volume);
        }

        public void PlayOneShotAt(FmodEventPath eventPath, Vector3 position)
        {
            if (TryResolve(eventPath, out var eventReference))
            {
                PlayOneShotAt(eventReference, position);
            }
        }

        public void PlayOneShotAttached(EventReference eventReference, GameObject target)
        {
            if (target == null)
            {
                Debug.LogError("[FmodAudio] PlayOneShotAttached failed: the target is null.");
                return;
            }

            PlayCore(eventReference, PlayLocation.Attached, Vector3.zero, target, true, 1f);
        }

        public void PlayOneShotAttached(FmodEventPath eventPath, GameObject target)
        {
            if (TryResolve(eventPath, out var eventReference))
            {
                PlayOneShotAttached(eventReference, target);
            }
        }

        public void Stop(FmodEventHandle handle, bool allowFadeout = true)
        {
            if (!IsHandleCurrent(handle))
            {
                return;
            }

            if (!_activeEvents.TryGetValue(handle.InstanceId, out var active))
            {
                return;
            }

            if (!active.Instance.isValid())
            {
                _activeEvents.Remove(handle.InstanceId);
                return;
            }

            active.Instance.stop(allowFadeout ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT : FMOD.Studio.STOP_MODE.IMMEDIATE);
        }

        public void StopAll(bool allowFadeout = true)
        {
            var mode = allowFadeout ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT : FMOD.Studio.STOP_MODE.IMMEDIATE;

            foreach (var active in _activeEvents.Values)
            {
                if (active.Instance.isValid())
                {
                    active.Instance.stop(mode);
                }
            }
        }

        public void SetParameter(FmodEventHandle handle, string name, float value)
        {
            if (!IsHandleCurrent(handle) || string.IsNullOrEmpty(name))
            {
                return;
            }

            if (!_activeEvents.TryGetValue(handle.InstanceId, out var active) || !active.Instance.isValid())
            {
                return;
            }

            active.Instance.setParameterByName(name, value);
        }

        public void SetMasterVolume(float value)
        {
            _masterVolume = Mathf.Clamp01(value);

            if (_initialized)
            {
                ApplyMasterVolume();
            }
        }

        public void SetChannelVolume(string channelId, float value)
        {
            if (string.IsNullOrEmpty(channelId))
            {
                return;
            }

            if (!_channelsById.TryGetValue(channelId, out var channel))
            {
                Debug.LogWarning("[FmodAudio] The channel is not registered: " + channelId);
                return;
            }

            var volume = Mathf.Clamp01(value);
            _channelVolumes[channelId] = volume;

            if (_initialized)
            {
                SetBusVolumeInternal(channel.BusPath, volume);
            }
        }

        public float? GetChannelVolume(string channelId)
        {
            if (string.IsNullOrEmpty(channelId))
            {
                return null;
            }

            return _channelVolumes.TryGetValue(channelId, out var volume) ? volume : (float?)null;
        }

        public void SetBusVolume(string busPath, float volume)
        {
            if (!RequireRuntime())
            {
                return;
            }

            SetBusVolumeInternal(busPath, Mathf.Clamp01(volume));
        }

        public float? GetBusVolume(string busPath)
        {
            if (!RequireRuntime() || string.IsNullOrEmpty(busPath))
            {
                return null;
            }

            try
            {
                RuntimeManager.GetBus(busPath).getVolume(out var volume);
                return volume;
            }
            catch (BusNotFoundException)
            {
                Debug.LogWarning("[FmodAudio] Bus not found: " + busPath);
                return null;
            }
        }

        public void MuteAll(bool muted)
        {
            if (!RequireRuntime())
            {
                return;
            }

            RuntimeManager.MuteAllEvents(muted);
        }

        public void PauseAll(bool paused)
        {
            if (!RequireRuntime())
            {
                return;
            }

            RuntimeManager.PauseAllEvents(paused);
        }

        private FmodEventHandle PlayCore(
            EventReference eventReference,
            PlayLocation location,
            Vector3 position,
            GameObject target,
            bool oneShot,
            float volume)
        {
            if (!_initialized)
            {
                Debug.LogError("[FmodAudio] The service is not initialized.");
                return FmodEventHandle.Invalid;
            }

            EventInstance instance;
            try
            {
                instance = RuntimeManager.CreateInstance(eventReference);
            }
            catch (EventNotFoundException)
            {
                Debug.LogError("[FmodAudio] Event not found: " + eventReference);
                return FmodEventHandle.Invalid;
            }

            if (!instance.isValid())
            {
                Debug.LogError("[FmodAudio] Failed to create an EventInstance.");
                return FmodEventHandle.Invalid;
            }

            try
            {
                ApplyLocation(instance, location, position, target);
                instance.setVolume(Mathf.Clamp01(volume));
                instance.start();
            }
            catch (Exception exception)
            {
                instance.release();
                Debug.LogException(exception);
                return FmodEventHandle.Invalid;
            }

            if (oneShot)
            {
                instance.release();
                return FmodEventHandle.Invalid;
            }

            var instanceId = _nextInstanceId++;
            _activeEvents[instanceId] = new ActiveEvent
            {
                Instance = instance,
                EventRef = eventReference,
            };

            return new FmodEventHandle(instanceId, _serviceGeneration);
        }

        private static void ApplyLocation(EventInstance instance, PlayLocation location, Vector3 position, GameObject target)
        {
            switch (location)
            {
                case PlayLocation.Position:
                    instance.set3DAttributes(RuntimeUtils.To3DAttributes(position));
                    break;
                case PlayLocation.Attached:
                    RuntimeManager.AttachInstanceToGameObject(instance, target);
                    break;
            }
        }

        private static bool TryResolve(FmodEventPath eventPath, out EventReference eventReference)
        {
            eventReference = default;

            if (eventPath == null || !eventPath.IsValid)
            {
                Debug.LogError("[FmodAudio] The event path is empty.");
                return false;
            }

            if (!RuntimeManager.IsInitialized)
            {
                Debug.LogError("[FmodAudio] FMOD runtime is not running.");
                return false;
            }

            try
            {
                eventReference = new EventReference { Guid = RuntimeManager.PathToGUID(eventPath.Path) };
                return true;
            }
            catch (EventNotFoundException)
            {
                Debug.LogError("[FmodAudio] Event not found: " + eventPath.Path);
                return false;
            }
        }

        private bool IsHandleCurrent(FmodEventHandle handle)
        {
            return handle.IsValid && handle.ServiceGeneration == _serviceGeneration;
        }

        private void ApplyMasterVolume()
        {
            SetBusVolumeInternal(_config.MasterBusPath, _masterVolume);
        }

        private void ApplyChannelVolumes()
        {
            foreach (var channelId in _channelsById.Keys)
            {
                var channel = _channelsById[channelId];
                _channelVolumes.TryGetValue(channelId, out var volume);
                SetBusVolumeInternal(channel.BusPath, volume);
            }
        }

        private static void SetBusVolumeInternal(string busPath, float volume)
        {
            if (string.IsNullOrEmpty(busPath))
            {
                Debug.LogWarning("[FmodAudio] The bus path is empty.");
                return;
            }

            try
            {
                RuntimeManager.GetBus(busPath).setVolume(volume);
            }
            catch (BusNotFoundException)
            {
                Debug.LogWarning("[FmodAudio] Bus not found: " + busPath);
            }
        }

        private static bool TryStartRuntime(out string error)
        {
            error = null;

            if (!Application.isPlaying)
            {
                error = "the FMOD runtime is only available in play mode.";
                return false;
            }

            try
            {
                EnsureRuntimeStarted();
            }
            catch (Exception exception)
            {
                error = "the FMOD runtime did not start (" + exception.Message + ").";
                return false;
            }

            if (!RuntimeManager.IsInitialized)
            {
                error = "the FMOD runtime did not start.";
                return false;
            }

            return true;
        }

        private static void EnsureRuntimeStarted()
        {
            if (RuntimeManager.IsInitialized)
            {
                return;
            }

            // 读一次 StudioSystem 就会触发 FMOD 运行时的惰性初始化（只在 Play 模式可用）。
            RuntimeManager.StudioSystem.isValid();
        }

        private static bool RequireRuntime()
        {
            if (TryStartRuntime(out var error))
            {
                return true;
            }

            Debug.LogError("[FmodAudio] " + error);
            return false;
        }

        private static void AttachListenerToMainCamera()
        {
            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogWarning("[FmodAudio] Camera.main is missing; the FMOD listener was not attached. Flat playback still works.");
                return;
            }

            var target = mainCamera.gameObject;
            if (target.GetComponent<StudioListener>() == null)
            {
                target.AddComponent<StudioListener>();
            }

            if (target.GetComponent<AudioListener>() == null)
            {
                target.AddComponent<AudioListener>();
            }
        }
    }
}
