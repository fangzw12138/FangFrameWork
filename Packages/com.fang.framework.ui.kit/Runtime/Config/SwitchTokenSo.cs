using System;
using UnityEngine;

namespace Fang.Framework.UI.Kit
{
    [CreateAssetMenu(fileName = "SwitchToken", menuName = "Fang Framework/UI Kit/开关 Token")]
    public sealed class SwitchTokenSo : TokenSo
    {
        [Tooltip("写进轨道底图的 Sprite；留空 = 不碰底图。")]
        [SerializeField] private Sprite _trackSprite;

        [Tooltip("写进滑块的 Sprite；留空 = 不碰滑块底图。")]
        [SerializeField] private Sprite _knobSprite;

        [Tooltip("开 / 关配色是一组，勾上才整组写进开关；不勾 = 不碰。")]
        [SerializeField] private bool _useColors;

        [Tooltip("轨道「开」颜色。")]
        [SerializeField] private Color _trackOnColor = Color.white;

        [Tooltip("轨道「关」颜色。")]
        [SerializeField] private Color _trackOffColor = Color.white;

        [Tooltip("滑块「开」颜色。")]
        [SerializeField] private Color _knobOnColor = Color.white;

        [Tooltip("滑块「关」颜色。")]
        [SerializeField] private Color _knobOffColor = Color.white;

        [Tooltip("开关文字的 token；留空 = 不碰。")]
        [SerializeField] private TextTokenSo _label;

        public override Type TargetType => typeof(KitSwitch);

        public override void Apply(Component target)
        {
            if (!EnsureTarget(target))
            {
                return;
            }

            var toggle = (KitSwitch)target;

            if (_trackSprite != null && toggle.Track != null)
            {
                toggle.Track.sprite = _trackSprite;
            }

            if (_knobSprite != null && toggle.Knob != null)
            {
                toggle.Knob.sprite = _knobSprite;
            }

            if (_useColors)
            {
                toggle.TrackOnColor = _trackOnColor;
                toggle.TrackOffColor = _trackOffColor;
                toggle.KnobOnColor = _knobOnColor;
                toggle.KnobOffColor = _knobOffColor;
            }

            if (_label != null && toggle.Label != null)
            {
                _label.Apply(toggle.Label);
            }

            toggle.SetState(toggle.IsOn, true);
        }
    }
}
