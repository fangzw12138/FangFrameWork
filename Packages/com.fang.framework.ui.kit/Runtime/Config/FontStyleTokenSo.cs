using System;
using TMPro;
using UnityEngine;

namespace Fang.Framework.UI.Kit
{
    [CreateAssetMenu(fileName = "FontStyleToken", menuName = "Fang Framework/UI Kit/样式 Token")]
    public sealed class FontStyleTokenSo : TokenSo
    {
        [Tooltip("写进 target 的字体样式（Normal / Bold / Italic…）。")]
        [SerializeField] private FontStyles _style = FontStyles.Normal;

        public override Type TargetType => typeof(TMP_Text);

        public FontStyles Style => _style;

        public override void Apply(Component target)
        {
            if (!EnsureTarget(target))
            {
                return;
            }

            ((TMP_Text)target).fontStyle = _style;
        }
    }
}
