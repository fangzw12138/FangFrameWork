using System;
using UnityEngine;
using UnityEngine.UI;

namespace Fang.Framework.UI.Kit
{
    [CreateAssetMenu(fileName = "SliderToken", menuName = "Fang Framework/UI Kit/滑条 Token")]
    public sealed class SliderTokenSo : TokenSo
    {
        [Tooltip("底图的 token；留空 = 不碰。")]
        [SerializeField] private IconTokenSo _background;

        [Tooltip("填充区的 token；留空 = 不碰。")]
        [SerializeField] private IconTokenSo _fill;

        [Tooltip("手柄的 token；手柄颜色由 5 态决定，这里通常只填 Sprite。")]
        [SerializeField] private IconTokenSo _handle;

        [Tooltip("勾上才把下面的 5 态颜色写进滑条；不勾 = 不碰。")]
        [SerializeField] private bool _useStates;

        [Tooltip("写进滑条的 5 态颜色（ColorTint）。")]
        [SerializeField] private ColorBlock _states = ColorBlock.defaultColorBlock;

        public override Type TargetType => typeof(KitSlider);

        public override void Apply(Component target)
        {
            if (!EnsureTarget(target))
            {
                return;
            }

            var slider = (KitSlider)target;

            if (_background != null && slider.Background != null)
            {
                _background.Apply(slider.Background);
            }

            if (_fill != null && slider.Fill != null)
            {
                _fill.Apply(slider.Fill);
            }

            if (_handle != null && slider.Handle != null)
            {
                _handle.Apply(slider.Handle);
            }

            if (_useStates && slider.Target != null)
            {
                slider.Target.colors = _states;
            }
        }
    }
}
