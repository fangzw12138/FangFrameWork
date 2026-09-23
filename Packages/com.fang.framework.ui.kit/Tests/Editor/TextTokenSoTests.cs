using System.Text.RegularExpressions;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Fang.Framework.UI.Kit.Editor.Tests
{
    public class TextTokenSoTests
    {
        [TearDown]
        public void TearDown()
        {
            KitTestSetup.Cleanup();
        }

        [Test]
        public void Text_tokens_accept_only_tmp_text()
        {
            var tokens = new TokenSo[]
            {
                KitTestSetup.NewToken<FontAssetTokenSo>(),
                KitTestSetup.NewToken<FontSizeTokenSo>(),
                KitTestSetup.NewToken<FontStyleTokenSo>(),
                KitTestSetup.NewToken<LineSpacingTokenSo>(),
            };

            foreach (var token in tokens)
            {
                Assert.AreEqual(typeof(TMP_Text), token.TargetType);
                Assert.IsTrue(token.Accepts(NewText()));
                Assert.IsFalse(token.Accepts(null));
                Assert.IsFalse(token.Accepts(NewImage()));
            }
        }

        [Test]
        public void FontAssetToken_writes_the_font()
        {
            var font = FirstFontAsset();
            if (font == null)
            {
                Assert.Ignore("工程里没有 TMP 字体资产，跳过。");
            }

            var text = NewText();
            var token = KitTestSetup.NewToken<FontAssetTokenSo>();
            KitTestSetup.SetObject(token, "_font", font);

            token.Apply(text);

            Assert.AreSame(font, text.font);
        }

        [Test]
        public void FontAssetToken_reports_an_error_when_no_font_is_set()
        {
            var text = NewText();
            var before = text.font;
            var token = KitTestSetup.NewToken<FontAssetTokenSo>();

            LogAssert.Expect(LogType.Error, new Regex("字体没填"));

            token.Apply(text);

            Assert.AreSame(before, text.font);
        }

        [Test]
        public void FontSizeToken_writes_the_font_size()
        {
            var text = NewText();
            var token = KitTestSetup.NewToken<FontSizeTokenSo>();
            KitTestSetup.SetFloat(token, "_fontSize", 42f);

            token.Apply(text);

            Assert.AreEqual(42f, text.fontSize);
        }

        [Test]
        public void FontStyleToken_writes_the_style()
        {
            var text = NewText();
            text.fontStyle = FontStyles.Bold;
            var token = KitTestSetup.NewToken<FontStyleTokenSo>();
            KitTestSetup.SetInt(token, "_style", (int)FontStyles.Italic);

            token.Apply(text);

            Assert.AreEqual(FontStyles.Italic, text.fontStyle);
        }

        [Test]
        public void LineSpacingToken_writes_the_line_spacing()
        {
            var text = NewText();
            var token = KitTestSetup.NewToken<LineSpacingTokenSo>();
            KitTestSetup.SetFloat(token, "_lineSpacing", 12f);

            token.Apply(text);

            Assert.AreEqual(12f, text.lineSpacing);
        }

        [Test]
        public void Text_tokens_report_an_error_and_change_nothing_for_a_non_text_target()
        {
            var image = NewImage();
            image.color = Color.red;
            var token = KitTestSetup.NewToken<FontSizeTokenSo>();
            KitTestSetup.SetFloat(token, "_fontSize", 42f);

            LogAssert.Expect(LogType.Error, new Regex("target 类型不符"));

            token.Apply(image);

            Assert.AreEqual(Color.red, image.color);
        }

        private static TMP_FontAsset FirstFontAsset()
        {
            var guids = UnityEditor.AssetDatabase.FindAssets("t:TMP_FontAsset");
            return guids.Length == 0
                ? null
                : UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private static TextMeshProUGUI NewText()
        {
            return KitTestSetup.NewGameObject("Text").AddComponent<TextMeshProUGUI>();
        }

        private static Image NewImage()
        {
            return KitTestSetup.NewGameObject("Image").AddComponent<Image>();
        }
    }
}
