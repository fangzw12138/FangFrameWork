using NUnit.Framework;

namespace Fang.Framework.FmodAudio.Editor.Tests
{
    public class FmodEventPathTests
    {
        [TearDown]
        public void TearDown()
        {
            FmodTestSetup.Cleanup();
        }

        [Test]
        public void An_empty_event_path_is_not_valid()
        {
            Assert.IsFalse(new FmodEventPath().IsValid);
            Assert.IsFalse(new FmodEventPath(string.Empty).IsValid);
            Assert.IsNull(new FmodEventPath().Path);
        }

        [Test]
        public void An_event_path_keeps_the_value_it_was_built_with()
        {
            var path = new FmodEventPath("event:/SFX/Hit");

            Assert.IsTrue(path.IsValid);
            Assert.AreEqual("event:/SFX/Hit", path.Path);
        }

        [Test]
        public void A_channel_keeps_its_id_and_bus_path()
        {
            var config = FmodTestSetup.NewConfig();
            FmodTestSetup.SetChannels(config, ("bgm", "bus:/Music", 0.5f));

            var channel = config.BusChannels[0];
            Assert.AreEqual("bgm", channel.ChannelId);
            Assert.AreEqual("bus:/Music", channel.BusPath);
            Assert.AreEqual(0.5f, channel.DefaultVolume);
        }

        [Test]
        public void A_channel_clamps_its_default_volume()
        {
            var config = FmodTestSetup.NewConfig();
            FmodTestSetup.SetChannels(config, ("bgm", "bus:/Music", 1f));

            FmodTestSetup.SetChannelVolumeOf(config, 0, 5f);
            Assert.AreEqual(1f, config.BusChannels[0].DefaultVolume);

            FmodTestSetup.SetChannelVolumeOf(config, 0, -2f);
            Assert.AreEqual(0f, config.BusChannels[0].DefaultVolume);
        }

        [Test]
        public void A_fresh_config_has_no_channels_and_the_master_bus()
        {
            var config = FmodTestSetup.NewConfig();

            Assert.AreEqual("bus:/", config.MasterBusPath);
            Assert.AreEqual(1f, config.DefaultMasterVolume);
            Assert.AreEqual(0, config.BusChannels.Count);
        }

        [Test]
        public void A_config_clamps_its_default_master_volume()
        {
            var config = FmodTestSetup.NewConfig();

            FmodTestSetup.SetDefaultMasterVolume(config, 3f);
            Assert.AreEqual(1f, config.DefaultMasterVolume);

            FmodTestSetup.SetDefaultMasterVolume(config, -1f);
            Assert.AreEqual(0f, config.DefaultMasterVolume);
        }
    }
}
