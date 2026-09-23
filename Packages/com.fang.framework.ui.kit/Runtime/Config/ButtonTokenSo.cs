using System;
using UnityEngine;
using UnityEngine.UI;

namespace Fang.Framework.UI.Kit
{
    [CreateAssetMenu(fileName = "ButtonToken", menuName = "Fang Framework/UI Kit/按钮 Token")]
    public sealed class ButtonTokenSo : TokenSo
    {
        [Tooltip("写进按钮底图的 Sprite；留空 = 不碰底图。")]
        [SerializeField] private Sprite _backgroundSprite;

        [Tooltip("勾上才把下面的交互态颜色写进按钮；不勾 = 不碰。")]
        [SerializeField] private bool _useStates;

        [Tooltip("写进按钮的 5 态颜色（ColorTint）。")]
        [SerializeField] private ColorBlock _states = ColorBlock.defaultColorBlock;

        [Tooltip("按钮文字的 token；留空 = 不碰文字。")]
        [SerializeField] private TextTokenSo _label;

        [Tooltip("按钮图标的 token；留空 = 不碰图标。")]
        [SerializeField] private IconTokenSo _icon;

        [Tooltip("点击时播放的动效资产；留空 = 不碰。")]
        [SerializeField] private UiMotionSo _onClick;

        [Tooltip("锁定态播放的动效资产；留空 = 不碰。")]
        [SerializeField] private UiMotionSo _onLocked;

        public override Type TargetType => typeof(KitButton);

        public override void Apply(Component target)
        {
            if (!EnsureTarget(target))
            {
                return;
            }

            var kitButton = (KitButton)target;

            if (_backgroundSprite != null && kitButton.Background != null)
            {
                kitButton.Background.sprite = _backgroundSprite;
            }

            if (_useStates && kitButton.Target != null)
            {
                kitButton.Target.colors = _states;
            }

            if (_label != null && kitButton.Label != null)
            {
                _label.Apply(kitButton.Label);
            }

            if (_icon != null && kitButton.Icon != null)
            {
                _icon.Apply(kitButton.Icon);
            }

            if (_onClick != null)
            {
                kitButton.OnClick = _onClick;
            }

            if (_onLocked != null)
            {
                kitButton.OnLocked = _onLocked;
            }
        }
    }
}
