using FMODUnity;
using NUnit.Framework;
using UnityEngine;

namespace Fang.Framework.FmodAudio.Editor.Tests
{
    public class FmodAudioServiceTests
    {
        private const string NotInitialized = "The service is not initialized";
        private const string RuntimeNotRunning = "FMOD runtime is not running";
        private const string PlayModeOnly = "only available in play mode";

        private FmodAudioService service;

        [SetUp]
        public void SetUp()
        {
            service = FmodTestSetup.NewService();
        }

        [TearDown]
        public void TearDown()
        {
            FmodTestSetup.Cleanup();
        }

        [Test]
        public void MasterVolume_starts_at_one()
        {
            Assert.AreEqual(1f, service.MasterVolume);
        }

        [Test]
        public void Initialize_rejects_a_missing_config()
        {
            FmodTestSetup.ExpectLog(LogType.Error, "配置为空");

            Assert.IsFalse(service.Initialize(null));
        }

        [Test]
        public void Initialize_rejects_a_config_with_a_duplicate_channel_id()
        {
            var config = FmodTestSetup.NewConfig();
            FmodTestSetup.SetChannels(
                config,
                ("sfx", "bus:/SFX", 1f),
                ("sfx", "bus:/Music", 1f));

            FmodTestSetup.ExpectLog(LogType.Error, "重复");

            Assert.IsFalse(service.Initialize(config));
        }

        [Test]
        public void Initialize_rejects_a_valid_config_outside_play_mode()
        {
            var config = FmodTestSetup.NewConfig();

            FmodTestSetup.ExpectLog(LogType.Error, PlayModeOnly);

            Assert.IsFalse(service.Initialize(config));
        }

        [Test]
        public void Shutdown_is_idempotent()
        {
            service.Shutdown();
            service.Shutdown();

            Assert.AreEqual(1f, service.MasterVolume);
        }

        [Test]
        public void OnDispose_shuts_the_service_down()
        {
            service.OnDispose();

            Assert.AreEqual(1f, service.MasterVolume);
        }

        [Test]
        public void OnTick_does_nothing_without_active_events()
        {
            service.OnTick(0.016f);
            service.OnTick(0.016f);
        }

        [Test]
        public void Play_returns_an_invalid_handle_before_initialize()
        {
            var reference = new EventReference();
            var path = new FmodEventPath("event:/SFX/Hit");

            FmodTestSetup.ExpectLog(LogType.Error, NotInitialized);
            Assert.IsFalse(service.Play(reference).IsValid);

            FmodTestSetup.ExpectLog(LogType.Error, NotInitialized);
            Assert.IsFalse(service.PlayAt(reference, Vector3.one).IsValid);

            FmodTestSetup.ExpectLog(LogType.Error, RuntimeNotRunning);
            Assert.IsFalse(service.Play(path).IsValid);

            FmodTestSetup.ExpectLog(LogType.Error, RuntimeNotRunning);
            Assert.IsFalse(service.PlayAt(path, Vector3.one).IsValid);
        }

        [Test]
        public void PlayAttached_rejects_a_missing_target()
        {
            FmodTestSetup.ExpectLog(LogType.Error, "PlayAttached failed");
            Assert.IsFalse(service.PlayAttached(new EventReference(), null).IsValid);

            FmodTestSetup.ExpectLog(LogType.Error, RuntimeNotRunning);
            Assert.IsFalse(service.PlayAttached(new FmodEventPath("event:/SFX/Hit"), null).IsValid);
        }

        [Test]
        public void PlayOneShot_entries_do_nothing_before_initialize()
        {
            var reference = new EventReference();
            var path = new FmodEventPath("event:/SFX/Hit");

            FmodTestSetup.ExpectLog(LogType.Error, NotInitialized);
            service.PlayOneShot(reference);

            FmodTestSetup.ExpectLog(LogType.Error, RuntimeNotRunning);
            service.PlayOneShot(path);

            FmodTestSetup.ExpectLog(LogType.Error, NotInitialized);
            service.PlayOneShotAt(reference, Vector3.one);

            FmodTestSetup.ExpectLog(LogType.Error, NotInitialized);
            service.PlayOneShotAt(reference, Vector3.one, 0.5f);

            FmodTestSetup.ExpectLog(LogType.Error, RuntimeNotRunning);
            service.PlayOneShotAt(path, Vector3.one);

            FmodTestSetup.ExpectLog(LogType.Error, NotInitialized);
            service.PlayOneShotAttached(reference, service.gameObject);

            FmodTestSetup.ExpectLog(LogType.Error, RuntimeNotRunning);
            service.PlayOneShotAttached(path, service.gameObject);
        }

        [Test]
        public void PlayOneShotAttached_rejects_a_missing_target()
        {
            FmodTestSetup.ExpectLog(LogType.Error, "PlayOneShotAttached failed");
            service.PlayOneShotAttached(new EventReference(), null);

            FmodTestSetup.ExpectLog(LogType.Error, RuntimeNotRunning);
            service.PlayOneShotAttached(new FmodEventPath("event:/SFX/Hit"), null);
        }

        [Test]
        public void Stop_ignores_an_invalid_handle()
        {
            service.Stop(FmodEventHandle.Invalid);
            service.Stop(FmodEventHandle.Invalid, false);
        }

        [Test]
        public void Stop_ignores_a_handle_from_an_older_generation()
        {
            service.Stop(new FmodEventHandle(1, 99));
            service.Stop(new FmodEventHandle(1, 99), false);
        }

        [Test]
        public void StopAll_does_nothing_without_active_events()
        {
            service.StopAll();
            service.StopAll(false);
        }

        [Test]
        public void SetParameter_ignores_an_invalid_handle()
        {
            service.SetParameter(FmodEventHandle.Invalid, "Health", 1f);
            service.SetParameter(new FmodEventHandle(1, 99), "Health", 1f);
            service.SetParameter(new FmodEventHandle(1, 99), string.Empty, 1f);
        }

        [Test]
        public void SetMasterVolume_clamps_the_value()
        {
            service.SetMasterVolume(4f);
            Assert.AreEqual(1f, service.MasterVolume);

            service.SetMasterVolume(-4f);
            Assert.AreEqual(0f, service.MasterVolume);

            service.SetMasterVolume(0.35f);
            Assert.AreEqual(0.35f, service.MasterVolume);
        }

        [Test]
        public void SetChannelVolume_warns_for_a_channel_that_was_never_registered()
        {
            FmodTestSetup.ExpectLog(LogType.Warning, "not registered");

            service.SetChannelVolume("bgm", 0.5f);

            Assert.IsNull(service.GetChannelVolume("bgm"));
        }

        [Test]
        public void GetChannelVolume_is_null_for_an_empty_id()
        {
            Assert.IsNull(service.GetChannelVolume(null));
            Assert.IsNull(service.GetChannelVolume(string.Empty));
        }

        [Test]
        public void SetBusVolume_does_nothing_before_the_runtime_starts()
        {
            FmodTestSetup.ExpectLog(LogType.Error, PlayModeOnly);
            service.SetBusVolume("bus:/", 0.5f);

            FmodTestSetup.ExpectLog(LogType.Error, PlayModeOnly);
            service.SetBusVolume(string.Empty, 0.5f);
        }

        [Test]
        public void GetBusVolume_is_null_before_the_runtime_starts()
        {
            FmodTestSetup.ExpectLog(LogType.Error, PlayModeOnly);
            Assert.IsNull(service.GetBusVolume("bus:/"));

            FmodTestSetup.ExpectLog(LogType.Error, PlayModeOnly);
            Assert.IsNull(service.GetBusVolume(null));
        }

        [Test]
        public void MuteAll_and_PauseAll_do_nothing_before_the_runtime_starts()
        {
            FmodTestSetup.ExpectLog(LogType.Error, PlayModeOnly, 4);

            service.MuteAll(true);
            service.PauseAll(true);
            service.MuteAll(false);
            service.PauseAll(false);
        }    }
}
