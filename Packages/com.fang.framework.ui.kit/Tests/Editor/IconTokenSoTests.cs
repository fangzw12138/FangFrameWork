using System.Text.RegularExpressions;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Fang.Framework.UI.Kit.Editor.Tests
{
    public class IconTokenSoTests
    {
        [TearDown]
        public void TearDown()
        {
            KitTestSetup.Cleanup();
        }

        [Test]
        public void Icon_token_targets_an_image()
        {
            var token = KitTestSetup.NewToken<IconTokenSo>();

            Assert.AreEqual(typeof(Image), token.TargetType);
            Assert.IsTrue(token.Accepts(NewImage()));
            Assert.IsFalse(token.Accepts(null));
            Assert.IsFalse(token.Accepts(KitTestSetup.NewComponent<TextMeshProUGUI>("Text")));
            Assert.IsFalse(token.Accepts(KitTestSetup.NewComponent<KitButton>("KitButton")));
        }

        [Test]
        public void Apply_writes_the_sprite()
        {
            var sprite = KitTestSetup.NewSprite("Dot");
            var image = NewImage();
            var token = KitTestSetup.NewToken<IconTokenSo>();
            KitTestSetup.SetObject(token, "_sprite", sprite);

            token.Apply(image);

            Assert.AreSame(sprite, image.sprite);
        }

        [Test]
        public void Apply_leaves_the_sprite_alone_when_it_is_empty()
        {
            var kept = KitTestSetup.NewSprite("Kept");
            var image = NewImage();
            image.sprite = kept;
            var token = KitTestSetup.NewToken<IconTokenSo>();

            token.Apply(image);

            Assert.AreSame(kept, image.sprite);
        }

        [Test]
        public void Apply_writes_the_color_only_when_the_toggle_is_on()
        {
            var image = NewImage();
            image.color = Color.red;
            var token = KitTestSetup.NewToken<IconTokenSo>();
            KitTestSetup.SetBool(token, "_useColor", true);
            KitTestSetup.SetColor(token, "_color", Color.blue);

            token.Apply(image);

            Assert.AreEqual(Color.blue, image.color);
        }

        [Test]
        public void Apply_leaves_the_color_alone_when_the_toggle_is_off()
        {
            var image = NewImage();
            image.color = Color.red;
            var token = KitTestSetup.NewToken<IconTokenSo>();
            KitTestSetup.SetColor(token, "_color", Color.blue);

            token.Apply(image);

            Assert.AreEqual(Color.red, image.color);
        }

        [Test]
        public void Apply_reports_an_error_and_changes_nothing_for_a_non_image_target()
        {
            var text = KitTestSetup.NewComponent<TextMeshProUGUI>("Text");
            text.color = Color.green;
            var token = KitTestSetup.NewToken<IconTokenSo>();
            KitTestSetup.SetBool(token, "_useColor", true);
            KitTestSetup.SetColor(token, "_color", Color.blue);

            LogAssert.Expect(LogType.Error, new Regex("target 类型不符"));

            token.Apply(text);

            Assert.AreEqual(Color.green, text.color);
        }

        private static Image NewImage()
        {
            return KitTestSetup.NewComponent<Image>("Image");
        }
    }
}
