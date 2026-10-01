using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Fang.Framework.UI.Kit
{
    [CreateAssetMenu(fileName = "FieldToken", menuName = "Fang Framework/UI Kit/输入框 Token")]
    public sealed class FieldTokenSo : TokenSo
    {
        [Tooltip("输入框底图的 token（Sprite + 颜色）；留空 = 不碰底图。")]
        [SerializeField] private IconTokenSo _background;

        [Tooltip("勾上才把下面的 5 态颜色写进输入框；不勾 = 不碰。")]
        [SerializeField] private bool _useStates;

        [Tooltip("写进输入框的 5 态颜色（ColorTint）。")]
        [SerializeField] private ColorBlock _states = ColorBlock.defaultColorBlock;

        [Tooltip("输入文字的 token；留空 = 不碰。")]
        [SerializeField] private TextTokenSo _text;

        [Tooltip("占位文字的 token；留空 = 不碰。")]
        [SerializeField] private TextTokenSo _placeholder;

        [Tooltip("勾上才写光标颜色；不勾 = 不碰。")]
        [SerializeField] private bool _useCaretColor;

        [Tooltip("写进输入框的光标颜色。")]
        [SerializeField] private Color _caretColor = Color.white;

        [Tooltip("勾上才写选中颜色；不勾 = 不碰。")]
        [SerializeField] private bool _useSelectionColor;

        [Tooltip("写进输入框的选中颜色。")]
        [SerializeField] private Color _selectionColor = Color.white;

        [Tooltip("前置图标的 token；留空 = 不碰。")]
        [SerializeField] private IconTokenSo _icon;

        public override Type TargetType => typeof(KitInputField);

        public override void Apply(Component target)
        {
            if (!EnsureTarget(target))
            {
                return;
            }

            var field = (KitInputField)target;

            if (_background != null && field.Background != null)
            {
                _background.Apply(field.Background);
            }

            if (field.Input != null)
            {
                if (_useStates)
                {
                    field.Input.colors = _states;
                }

                if (_useCaretColor)
                {
                    field.Input.caretColor = _caretColor;
                }

                if (_useSelectionColor)
                {
                    field.Input.selectionColor = _selectionColor;
                }

                if (_text != null && field.Input.textComponent != null)
                {
                    _text.Apply(field.Input.textComponent);
                }
            }

            if (_placeholder != null && field.Placeholder != null)
            {
                _placeholder.Apply(field.Placeholder);
            }

            if (_icon != null && field.Icon != null)
            {
                _icon.Apply(field.Icon);
            }
        }
    }
}
