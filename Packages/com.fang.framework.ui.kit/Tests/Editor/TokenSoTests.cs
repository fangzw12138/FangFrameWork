using NUnit.Framework;

namespace Fang.Framework.UI.Kit.Editor.Tests
{
    public class TokenSoTests
    {
        [TearDown]
        public void TearDown()
        {
            KitTestSetup.Cleanup();
        }

        [Test]
        public void MatchId_starts_empty_which_means_not_enabled()
        {
            Assert.IsNull(KitTestSetup.NewToken<ColorTokenSo>().MatchId);
        }

        [Test]
        public void MatchId_is_independent_of_the_asset_id()
        {
            var token = KitTestSetup.NewToken<ColorTokenSo>("Color/Primary");
            KitTestSetup.SetString(token, "_id", "MyColorToken");

            Assert.AreEqual("MyColorToken", token.Id);
            Assert.AreEqual("Color/Primary", token.MatchId);
        }
    }
}
