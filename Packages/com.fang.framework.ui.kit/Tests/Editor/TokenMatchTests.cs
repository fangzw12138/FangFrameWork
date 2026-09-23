using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Fang.Framework.UI.Kit.Editor.Tests
{
    public class TokenMatchTests
    {
        [TearDown]
        public void TearDown()
        {
            KitTestSetup.Cleanup();
        }

        [Test]
        public void Entries_expose_what_was_serialized()
        {
            var image = NewImage();
            var match = KitTestSetup.NewMatch(("Color/Primary", image));

            Assert.AreEqual(1, match.Entries.Count);
            Assert.AreEqual("Color/Primary", match.Entries[0].Id);
            Assert.AreSame(image, match.Entries[0].Target);
        }

        [Test]
        public void ApplyFrom_writes_every_token_bound_to_an_id()
        {
            var text = NewText();
            var match = KitTestSetup.NewMatch(("Text/Title", text));

            var size = KitTestSetup.NewToken<FontSizeTokenSo>("Text/Title");
            KitTestSetup.SetFloat(size, "_fontSize", 42f);
            var color = KitTestSetup.NewToken<ColorTokenSo>("Text/Title");
            KitTestSetup.SetColor(color, "_color", Color.green);
            var project = KitTestSetup.NewProject(size, color);

            match.ApplyFrom(project);

            Assert.AreEqual(42f, text.fontSize);
            Assert.AreEqual(Color.green, text.color);
        }

        [Test]
        public void ApplyFrom_lets_the_later_token_win_for_the_same_id()
        {
            var image = NewImage();
            var match = KitTestSetup.NewMatch(("Color/Primary", image));

            var first = KitTestSetup.NewToken<ColorTokenSo>("Color/Primary");
            KitTestSetup.SetColor(first, "_color", Color.green);
            var second = KitTestSetup.NewToken<ColorTokenSo>("Color/Primary");
            KitTestSetup.SetColor(second, "_color", Color.red);
            var project = KitTestSetup.NewProject(first, second);

            match.ApplyFrom(project);

            Assert.AreEqual(Color.red, image.color);
        }

        [Test]
        public void ApplyFrom_uses_one_token_for_two_ids()
        {
            var title = NewText();
            var body = NewText();
            var match = KitTestSetup.NewMatch(("Text/Title", title), ("Text/Body", body));

            var size = KitTestSetup.NewToken<FontSizeTokenSo>("Text/Title");
            KitTestSetup.SetFloat(size, "_fontSize", 18f);
            var project = KitTestSetup.NewProject(size);

            match.ApplyFrom(project);

            Assert.AreEqual(18f, title.fontSize);
        }

        [Test]
        public void ApplyFrom_skips_an_entry_without_a_target()
        {
            var image = NewImage();
            var match = KitTestSetup.NewMatch(("Color/Primary", null), ("Color/Primary", image));
            var project = KitTestSetup.NewProject(KitTestSetup.NewToken<ColorTokenSo>("Color/Primary"));

            match.ApplyFrom(project);

            Assert.AreEqual(Color.white, image.color);
        }

        [Test]
        public void ApplyFrom_skips_an_id_without_a_token()
        {
            var image = NewImage();
            var match = KitTestSetup.NewMatch(("Color/Primary", image));
            var project = KitTestSetup.NewProject();

            match.ApplyFrom(project);

            Assert.AreEqual(Color.white, image.color);
        }

        [Test]
        public void ApplyFrom_does_nothing_for_a_null_project()
        {
            var image = NewImage();
            var match = KitTestSetup.NewMatch(("Color/Primary", image));

            match.ApplyFrom(null);

            Assert.AreEqual(Color.white, image.color);
        }

        private static Image NewImage()
        {
            return KitTestSetup.NewGameObject("Image").AddComponent<Image>();
        }

        private static TextMeshProUGUI NewText()
        {
            return KitTestSetup.NewGameObject("Text").AddComponent<TextMeshProUGUI>();
        }
    }
}
