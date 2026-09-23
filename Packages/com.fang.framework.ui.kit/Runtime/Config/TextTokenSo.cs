using System;
using TMPro;
using UnityEngine;

namespace Fang.Framework.UI.Kit
{
    [CreateAssetMenu(fileName = "TextToken", menuName = "Fang Framework/UI Kit/文本 Token")]
    public sealed class TextTokenSo : TokenSo
    {
        [Tooltip("写进 target 的 TMP 字体资产；留空 = 不碰字体。")]
        [SerializeField] private TMP_FontAsset _font;

        [Tooltip("勾上才把下面的字号写进 target；不勾 = 不碰字号。")]
        [SerializeField] private bool _useFontSize;

        [Tooltip("写进 target 的字号（点）。")]
        [SerializeField] private float _fontSize = 32f;

        [Tooltip("勾上才把下面的样式写进 target；不勾 = 不碰样式。")]
        [SerializeField] private bool _useStyle;

        [Tooltip("写进 target 的字体样式（Normal / Bold / Italic…）。")]
        [SerializeField] private FontStyles _style = FontStyles.Normal;

        [Tooltip("勾上才把下面的行距写进 target；不勾 = 不碰行距。")]
        [SerializeField] private bool _useLineSpacing;

        [Tooltip("写进 target 的行距（点，可负）。")]
        [SerializeField] private float _lineSpacing;

        [Tooltip("勾上才把下面的颜色写进 target；不勾 = 不碰颜色。")]
        [SerializeField] private bool _useColor;

        [Tooltip("写进 target 的颜色。")]
        [SerializeField] private Color _color = Color.white;

        public override Type TargetType => typeof(TMP_Text);

        public override void Apply(Component target)
        {
            if (!EnsureTarget(target))
            {
                return;
            }

            var text = (TMP_Text)target;

            if (_font != null)
            {
                text.font = _font;
            }

            if (_useFontSize)
            {
                text.fontSize = _fontSize;
            }

            if (_useStyle)
            {
                text.fontStyle = _style;
            }

            if (_useLineSpacing)
            {
                text.lineSpacing = _lineSpacing;
            }

            if (_useColor)
            {
                text.color = _color;
            }
        }
    }
}
