using NUnit.Framework;

namespace Fang.Framework.UI.Async.Tests
{
    public class UILayerResolverTests
    {
        [TearDown]
        public void TearDown()
        {
            TestConfigs.DestroyAll();
        }

        [Test]
        public void Resolve_adds_the_layer_base_sort_order_to_the_panel_sort_order()
        {
            var layer = TestConfigs.CreateLayer("Hud", 100);
            var panel = TestConfigs.CreateVisualTreePanel("Panel", "Hud", 5, false, false);

            Assert.AreEqual(105, UILayerResolver.Resolve(new[] { layer }, panel));
        }

        [Test]
        public void Resolve_falls_back_to_the_panel_sort_order_when_the_layer_id_is_empty()
        {
            var layer = TestConfigs.CreateLayer("Hud", 100);
            var panel = TestConfigs.CreateVisualTreePanel("Panel", null, 7, false, false);

            Assert.AreEqual(7, UILayerResolver.Resolve(new[] { layer }, panel));
        }

        [Test]
        public void Resolve_falls_back_to_the_panel_sort_order_when_the_layer_is_missing()
        {
            var layer = TestConfigs.CreateLayer("Hud", 100);
            var panel = TestConfigs.CreateVisualTreePanel("Panel", "Missing", 7, false, false);

            Assert.AreEqual(7, UILayerResolver.Resolve(new[] { layer }, panel));
        }

        [Test]
        public void Resolve_returns_zero_when_the_layer_list_is_null()
        {
            var panel = TestConfigs.CreateVisualTreePanel("Panel", "Hud", 0, false, false);

            Assert.AreEqual(0, UILayerResolver.Resolve(null, panel));
        }

        [Test]
        public void Resolve_returns_zero_for_a_null_config()
        {
            Assert.AreEqual(0, UILayerResolver.Resolve(null, null));
        }

        [Test]
        public void FindLayer_returns_null_for_a_null_or_empty_id()
        {
            var layer = TestConfigs.CreateLayer("Hud", 100);

            Assert.IsNull(UILayerResolver.FindLayer(new[] { layer }, null));
            Assert.IsNull(UILayerResolver.FindLayer(new[] { layer }, string.Empty));
        }

        [Test]
        public void FindLayer_returns_null_when_the_layer_is_missing()
        {
            var layer = TestConfigs.CreateLayer("Hud", 100);

            Assert.IsNull(UILayerResolver.FindLayer(new[] { layer }, "Missing"));
            Assert.IsNull(UILayerResolver.FindLayer(null, "Hud"));
        }

        [Test]
        public void FindLayer_returns_the_matching_layer()
        {
            var hud = TestConfigs.CreateLayer("Hud", 100);
            var popup = TestConfigs.CreateLayer("Popup", 500);

            Assert.AreSame(popup, UILayerResolver.FindLayer(new[] { hud, popup }, "Popup"));
        }
    }
}
