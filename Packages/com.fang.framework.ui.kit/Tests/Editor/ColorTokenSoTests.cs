using System.Text.RegularExpressions;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Fang.Framework.UI.Kit.Editor.Tests
{
    public class ColorTokenSoTests
    {
        [TearDown]
        public void TearDown()
        {
            KitTestSetup.Cleanup();
        }

        [Test]
        public void TargetType_describes_a_color_token()
        {
            var token = KitTestSetup.NewToken<ColorTokenSo>();

            Assert.AreEqual(typeof(Graphic), token.TargetType);
        }

        [Test]
        public void Accepts_every_graphic_including_tmp_text()
        {
            var token = KitTestSetup.NewToken<ColorTokenSo>();

            Assert.IsTrue(token.Accepts(NewImage()));
            Assert.IsTrue(token.Accepts(NewText()));
            Assert.IsFalse(token.Accepts(null));
            Assert.IsFalse(token.Accepts(KitTestSetup.NewGameObject("Empty").transform));
        }

        [Test]
        public void Apply_writes_the_color_to_an_image()
        {
            var image = NewImage();
            var token = KitTestSetup.NewToken<ColorTokenSo>();
            KitTestSetup.SetColor(token, "_color", Color.green);

            token.Apply(image);

            Assert.AreEqual(Color.green, image.color);
        }

        [Test]
        public void Apply_writes_the_color_to_a_tmp_text()
        {
            var text = NewText();
            var token = KitTestSetup.NewToken<ColorTokenSo>();
            KitTestSetup.SetColor(token, "_color", Color.cyan);

            token.Apply(text);

            Assert.AreEqual(Color.cyan, text.color);
        }

        [Test]
        public void Apply_reports_an_error_for_a_non_graphic_target()
        {
            var transform = KitTestSetup.NewGameObject("Empty").transform;
            var token = KitTestSetup.NewToken<ColorTokenSo>();

            LogAssert.Expect(LogType.Error, new Regex("target 类型不符"));

            token.Apply(transform);
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
