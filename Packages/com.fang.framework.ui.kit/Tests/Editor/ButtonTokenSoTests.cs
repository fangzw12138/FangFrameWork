using System.Text.RegularExpressions;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Fang.Framework.UI.Kit.Editor.Tests
{
    public class ButtonTokenSoTests
    {
        [TearDown]
        public void TearDown()
        {
            KitTestSetup.Cleanup();
        }

        [Test]
        public void Button_token_targets_a_kit_button()
        {
            var token = KitTestSetup.NewToken<ButtonTokenSo>();

            Assert.AreEqual(typeof(KitButton), token.TargetType);
            Assert.IsTrue(token.Accepts(NewKitButton()));
            Assert.IsFalse(token.Accepts(null));
            Assert.IsFalse(token.Accepts(KitTestSetup.NewComponent<Image>("Image")));
            Assert.IsFalse(token.Accepts(KitTestSetup.NewComponent<TextMeshProUGUI>("Text")));
        }

        [Test]
        public void Apply_writes_the_background_sprite()
        {
            var sprite = KitTestSetup.NewSprite("Bg");
            var kit = NewKitButton();
            var token = KitTestSetup.NewToken<ButtonTokenSo>();
            KitTestSetup.SetObject(token, "_backgroundSprite", sprite);

            token.Apply(kit);

            Assert.AreSame(sprite, kit.Background.sprite);
        }

        [Test]
        public void Apply_writes_the_color_block_only_when_the_toggle_is_on()
        {
            var kit = NewKitButton();
            var token = KitTestSetup.NewToken<ButtonTokenSo>();
            KitTestSetup.SetBool(token, "_useStates", true);
            SetNormalColor(token, Color.cyan);

            token.Apply(kit);

            Assert.AreEqual(Color.cyan, kit.Target.colors.normalColor);
        }

        [Test]
        public void Apply_leaves_the_color_block_alone_when_the_toggle_is_off()
        {
            var kit = NewKitButton();
            var before = kit.Target.colors.normalColor;
            var token = KitTestSetup.NewToken<ButtonTokenSo>();
            SetNormalColor(token, Color.cyan);

            token.Apply(kit);

            Assert.AreEqual(before, kit.Target.colors.normalColor);
        }

        [Test]
        public void Apply_dispatches_the_nested_label_token_onto_the_label()
        {
            var kit = NewKitButton();
            var label = KitTestSetup.NewToken<TextTokenSo>();
            KitTestSetup.SetBool(label, "_useFontSize", true);
            KitTestSetup.SetFloat(label, "_fontSize", 17f);
            KitTestSetup.SetBool(label, "_useColor", true);
            KitTestSetup.SetColor(label, "_color", Color.yellow);

            var token = KitTestSetup.NewToken<ButtonTokenSo>();
            KitTestSetup.SetObject(token, "_label", label);

            token.Apply(kit);

            Assert.AreEqual(17f, kit.Label.fontSize);
            Assert.AreEqual(Color.yellow, kit.Label.color);
        }

        [Test]
        public void Apply_dispatches_the_nested_icon_token_onto_the_icon()
        {
            var kit = NewKitButton();
            var sprite = KitTestSetup.NewSprite("Icon");
            var icon = KitTestSetup.NewToken<IconTokenSo>();
            KitTestSetup.SetObject(icon, "_sprite", sprite);
            KitTestSetup.SetBool(icon, "_useColor", true);
            KitTestSetup.SetColor(icon, "_color", Color.magenta);

            var token = KitTestSetup.NewToken<ButtonTokenSo>();
            KitTestSetup.SetObject(token, "_icon", icon);

            token.Apply(kit);

            Assert.AreSame(sprite, kit.Icon.sprite);
            Assert.AreEqual(Color.magenta, kit.Icon.color);
        }

        [Test]
        public void Apply_writes_both_motion_slots()
        {
            var kit = NewKitButton();
            var click = KitTestSetup.Track(ScriptableObject.CreateInstance<UiMotionSo>());
            var locked = KitTestSetup.Track(ScriptableObject.CreateInstance<UiMotionSo>());
            var token = KitTestSetup.NewToken<ButtonTokenSo>();
            KitTestSetup.SetObject(token, "_onClick", click);
            KitTestSetup.SetObject(token, "_onLocked", locked);

            token.Apply(kit);

            Assert.AreSame(click, kit.OnClick);
            Assert.AreSame(locked, kit.OnLocked);
        }

        [Test]
        public void Apply_leaves_the_motion_slots_alone_when_they_are_empty()
        {
            var kept = KitTestSetup.Track(ScriptableObject.CreateInstance<UiMotionSo>());
            var kit = NewKitButton();
            kit.OnClick = kept;
            var token = KitTestSetup.NewToken<ButtonTokenSo>();

            token.Apply(kit);

            Assert.AreSame(kept, kit.OnClick);
            Assert.IsNull(kit.OnLocked);
        }

        [Test]
        public void Apply_skips_the_label_silently_when_the_button_has_none()
        {
            var kit = NewKitButton(withLabel: false);
            var label = KitTestSetup.NewToken<TextTokenSo>();
            KitTestSetup.SetBool(label, "_useFontSize", true);
            KitTestSetup.SetFloat(label, "_fontSize", 17f);

            var token = KitTestSetup.NewToken<ButtonTokenSo>();
            KitTestSetup.SetObject(token, "_label", label);

            token.Apply(kit);

            Assert.IsNull(kit.Label);
        }

        [Test]
        public void Apply_skips_the_icon_silently_when_the_button_has_none()
        {
            var kit = NewKitButton(withIcon: false);
            var icon = KitTestSetup.NewToken<IconTokenSo>();
            KitTestSetup.SetBool(icon, "_useColor", true);
            KitTestSetup.SetColor(icon, "_color", Color.magenta);

            var token = KitTestSetup.NewToken<ButtonTokenSo>();
            KitTestSetup.SetObject(token, "_icon", icon);

            token.Apply(kit);

            Assert.IsNull(kit.Icon);
        }

        [Test]
        public void Apply_skips_the_background_silently_when_the_button_has_none()
        {
            var sprite = KitTestSetup.NewSprite("Bg");
            var kit = NewKitButton(withBackground: false);
            var token = KitTestSetup.NewToken<ButtonTokenSo>();
            KitTestSetup.SetObject(token, "_backgroundSprite", sprite);

            token.Apply(kit);

            Assert.IsNull(kit.Background);
        }

        [Test]
        public void Apply_writes_nothing_when_the_token_is_empty()
        {
            var kit = NewKitButton();
            var before = kit.Target.colors.normalColor;
            var token = KitTestSetup.NewToken<ButtonTokenSo>();

            token.Apply(kit);

            Assert.AreEqual(before, kit.Target.colors.normalColor);
            Assert.IsNull(kit.OnClick);
            Assert.IsNull(kit.OnLocked);
        }

        [Test]
        public void Apply_reports_an_error_and_changes_nothing_for_a_non_button_target()
        {
            var image = KitTestSetup.NewComponent<Image>("Image");
            image.color = Color.red;
            var token = KitTestSetup.NewToken<ButtonTokenSo>();
            KitTestSetup.SetObject(token, "_backgroundSprite", KitTestSetup.NewSprite("Bg"));

            LogAssert.Expect(LogType.Error, new Regex("target 类型不符"));

            token.Apply(image);

            Assert.IsNull(image.sprite);
        }

        private static void SetNormalColor(ButtonTokenSo token, Color color)
        {
            var serialized = new SerializedObject(token);
            var states = serialized.FindProperty("_states");
            states.FindPropertyRelative("m_NormalColor").colorValue = color;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static KitButton NewKitButton(bool withBackground = true, bool withIcon = true, bool withLabel = true)
        {
            var go = KitTestSetup.NewGameObject("KitButton");

            Image background = null;
            if (withBackground)
            {
                background = go.AddComponent<Image>();
            }

            var button = go.AddComponent<Button>();
            button.targetGraphic = background;

            var kit = go.AddComponent<KitButton>();

            Image icon = null;
            if (withIcon)
            {
                icon = KitTestSetup.NewComponent<Image>("Icon");
                icon.transform.SetParent(go.transform, false);
            }

            TMP_Text label = null;
            if (withLabel)
            {
                label = KitTestSetup.NewComponent<TextMeshProUGUI>("Label");
                label.transform.SetParent(go.transform, false);
            }

            KitTestSetup.SetObject(kit, "_background", background);
            KitTestSetup.SetObject(kit, "_button", button);
            KitTestSetup.SetObject(kit, "_icon", icon);
            KitTestSetup.SetObject(kit, "_label", label);

            return kit;
        }
    }
}
