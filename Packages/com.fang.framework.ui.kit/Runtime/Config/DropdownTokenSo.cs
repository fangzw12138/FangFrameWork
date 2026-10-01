using System;
using UnityEngine;
using UnityEngine.UI;

namespace Fang.Framework.UI.Kit
{
    [CreateAssetMenu(fileName = "DropdownToken", menuName = "Fang Framework/UI Kit/下拉 Token")]
    public sealed class DropdownTokenSo : TokenSo
    {
        [Tooltip("下拉底图的 token（Sprite + 颜色）；留空 = 不碰底图。")]
        [SerializeField] private IconTokenSo _background;

        [Tooltip("勾上才把下面的 5 态颜色写进下拉；不勾 = 不碰。")]
        [SerializeField] private bool _useStates;

        [Tooltip("写进下拉的 5 态颜色（ColorTint）。")]
        [SerializeField] private ColorBlock _states = ColorBlock.defaultColorBlock;

        [Tooltip("标题文字的 token；留空 = 不碰。")]
        [SerializeField] private TextTokenSo _caption;

        [Tooltip("右侧箭头的 token；留空 = 不碰。")]
        [SerializeField] private IconTokenSo _arrow;

        [Tooltip("列表项底图的 token；留空 = 不碰。")]
        [SerializeField] private IconTokenSo _itemBackground;

        [Tooltip("列表项文字的 token；留空 = 不碰。")]
        [SerializeField] private TextTokenSo _itemLabel;

        [Tooltip("列表项勾的 token；留空 = 不碰。")]
        [SerializeField] private IconTokenSo _itemCheckmark;

        public override Type TargetType => typeof(KitDropdown);

        public override void Apply(Component target)
        {
            if (!EnsureTarget(target))
            {
                return;
            }

            var dropdown = (KitDropdown)target;

            if (_background != null && dropdown.Background != null)
            {
                _background.Apply(dropdown.Background);
            }

            if (dropdown.Target != null)
            {
                if (_useStates)
                {
                    dropdown.Target.colors = _states;
                }

                if (_caption != null && dropdown.Target.captionText != null)
                {
                    _caption.Apply(dropdown.Target.captionText);
                }

                if (_itemLabel != null && dropdown.Target.itemText != null)
                {
                    _itemLabel.Apply(dropdown.Target.itemText);
                }
            }

            if (_arrow != null && dropdown.Arrow != null)
            {
                _arrow.Apply(dropdown.Arrow);
            }

            if (_itemBackground != null && dropdown.ItemBackground != null)
            {
                _itemBackground.Apply(dropdown.ItemBackground);
            }

            if (_itemCheckmark != null && dropdown.ItemCheckmark != null)
            {
                _itemCheckmark.Apply(dropdown.ItemCheckmark);
            }
        }
    }
}
