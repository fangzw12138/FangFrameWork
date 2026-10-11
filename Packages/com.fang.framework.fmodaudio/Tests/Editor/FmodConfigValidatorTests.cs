using NUnit.Framework;

namespace Fang.Framework.FmodAudio.Editor.Tests
{
    public class FmodConfigValidatorTests
    {
        [TearDown]
        public void TearDown()
        {
            FmodTestSetup.Cleanup();
        }

        [Test]
        public void Validate_rejects_a_missing_config()
        {
            Assert.IsFalse(FmodConfigValidator.TryValidate(null, out var error));
            Assert.IsFalse(string.IsNullOrEmpty(error));
        }

        [Test]
        public void Validate_accepts_a_complete_config()
        {
            var config = FmodTestSetup.NewConfig();
            FmodTestSetup.SetChannels(
                config,
                ("bgm", "bus:/Music", 0.7f),
                ("sfx", "bus:/SFX", 1f));

            Assert.IsTrue(FmodConfigValidator.TryValidate(config, out var error));
            Assert.IsNull(error);
        }

        [Test]
        public void Validate_accepts_an_empty_channel_list()
        {
            Assert.IsTrue(FmodConfigValidator.TryValidate(FmodTestSetup.NewConfig(), out _));
        }

        [Test]
        public void Validate_rejects_an_empty_channel_id()
        {
            var config = FmodTestSetup.NewConfig();
            FmodTestSetup.SetChannels(config, (string.Empty, "bus:/SFX", 1f));

            Assert.IsFalse(FmodConfigValidator.TryValidate(config, out var error));
            Assert.IsTrue(error.Contains("channelId"));
        }

        [Test]
        public void Validate_rejects_a_duplicate_channel_id()
        {
            var config = FmodTestSetup.NewConfig();
            FmodTestSetup.SetChannels(
                config,
                ("sfx", "bus:/SFX", 1f),
                ("sfx", "bus:/Music", 1f));

            Assert.IsFalse(FmodConfigValidator.TryValidate(config, out var error));
            Assert.IsTrue(error.Contains("重复"));
        }

        [Test]
        public void Validate_rejects_an_empty_bus_path()
        {
            var config = FmodTestSetup.NewConfig();
            FmodTestSetup.SetChannels(config, ("sfx", string.Empty, 1f));

            Assert.IsFalse(FmodConfigValidator.TryValidate(config, out var error));
            Assert.IsTrue(error.Contains("busPath"));
        }

        [Test]
        public void Validate_rejects_a_bus_path_without_the_bus_prefix()
        {
            var config = FmodTestSetup.NewConfig();
            FmodTestSetup.SetChannels(config, ("sfx", "SFX", 1f));

            Assert.IsFalse(FmodConfigValidator.TryValidate(config, out var error));
            Assert.IsTrue(error.Contains("bus:/"));
        }

        [Test]
        public void Validate_rejects_an_empty_master_bus_path()
        {
            var config = FmodTestSetup.NewConfig();
            FmodTestSetup.SetMasterBusPath(config, string.Empty);

            Assert.IsFalse(FmodConfigValidator.TryValidate(config, out var error));
            Assert.IsTrue(error.Contains("主总线"));
        }

        [Test]
        public void Validate_rejects_a_master_bus_path_without_the_bus_prefix()
        {
            var config = FmodTestSetup.NewConfig();
            FmodTestSetup.SetMasterBusPath(config, "Master");

            Assert.IsFalse(FmodConfigValidator.TryValidate(config, out var error));
            Assert.IsTrue(error.Contains("bus:/"));
        }

        [Test]
        public void Validate_rejects_a_null_channel_entry()
        {
            var config = FmodTestSetup.NewConfig();
            FmodTestSetup.AddNullChannel(config);

            Assert.AreEqual(1, config.BusChannels.Count);
            Assert.IsNull(config.BusChannels[0]);
            Assert.IsFalse(FmodConfigValidator.TryValidate(config, out var error));
            Assert.IsTrue(error.Contains("空引用"));
        }

        [Test]
        public void Validate_reports_the_first_problem_in_row_order()
        {
            var config = FmodTestSetup.NewConfig();
            FmodTestSetup.SetChannels(
                config,
                (string.Empty, "bus:/SFX", 1f),
                ("sfx", string.Empty, 1f));

            Assert.IsFalse(FmodConfigValidator.TryValidate(config, out var error));
            Assert.IsTrue(error.Contains("第 0 条"));
        }
    }
}
