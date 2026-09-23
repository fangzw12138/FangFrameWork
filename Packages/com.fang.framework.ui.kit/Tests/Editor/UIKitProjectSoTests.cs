using NUnit.Framework;

namespace Fang.Framework.UI.Kit.Editor.Tests
{
    public class UIKitProjectSoTests
    {
        [TearDown]
        public void TearDown()
        {
            KitTestSetup.Cleanup();
        }

        [Test]
        public void GetTokens_returns_every_token_with_that_match_id_in_library_order()
        {
            var size = KitTestSetup.NewToken<FontSizeTokenSo>("Text/Title");
            var color = KitTestSetup.NewToken<ColorTokenSo>("Text/Title");
            var project = KitTestSetup.NewProject(size, color);

            var tokens = project.GetTokens("Text/Title");

            Assert.AreEqual(2, tokens.Count);
            Assert.AreSame(size, tokens[0]);
            Assert.AreSame(color, tokens[1]);
        }

        [Test]
        public void GetTokens_ignores_tokens_with_another_match_id()
        {
            var size = KitTestSetup.NewToken<FontSizeTokenSo>("Text/Title");
            var color = KitTestSetup.NewToken<ColorTokenSo>("Color/Primary");
            var project = KitTestSetup.NewProject(size, color);

            var title = project.GetTokens("Text/Title");

            Assert.AreEqual(1, title.Count);
            Assert.AreSame(size, title[0]);
            Assert.AreSame(color, project.GetTokens("Color/Primary")[0]);
        }

        [Test]
        public void GetTokens_ignores_tokens_that_are_not_enabled()
        {
            var notEnabled = KitTestSetup.NewToken<FontSizeTokenSo>();
            var enabled = KitTestSetup.NewToken<FontSizeTokenSo>("Text/Title");
            var project = KitTestSetup.NewProject(notEnabled, enabled);

            var tokens = project.GetTokens("Text/Title");

            Assert.AreEqual(1, tokens.Count);
            Assert.AreSame(enabled, tokens[0]);
        }

        [Test]
        public void GetTokens_returns_empty_for_an_unknown_or_empty_id()
        {
            var project = KitTestSetup.NewProject(KitTestSetup.NewToken<FontSizeTokenSo>("Text/Title"));

            Assert.AreEqual(0, project.GetTokens("Color/Primary").Count);
            Assert.AreEqual(0, project.GetTokens(null).Count);
            Assert.AreEqual(0, project.GetTokens(string.Empty).Count);
        }

        [Test]
        public void GetTokens_skips_empty_rows()
        {
            var size = KitTestSetup.NewToken<FontSizeTokenSo>("Text/Title");
            var project = KitTestSetup.NewProject(null, size);

            var tokens = project.GetTokens("Text/Title");

            Assert.AreEqual(1, tokens.Count);
            Assert.AreSame(size, tokens[0]);
        }

        [Test]
        public void Prefabs_and_tokens_start_empty()
        {
            var project = KitTestSetup.NewProject();

            Assert.AreEqual(0, project.Prefabs.Count);
            Assert.AreEqual(0, project.Tokens.Count);
        }
    }
}
