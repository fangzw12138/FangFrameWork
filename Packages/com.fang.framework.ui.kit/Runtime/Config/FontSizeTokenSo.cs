using System;
using TMPro;
using UnityEngine;

namespace Fang.Framework.UI.Kit
{
    [CreateAssetMenu(fileName = "FontSizeToken", menuName = "Fang Framework/UI Kit/字号 Token")]
    public sealed class FontSizeTokenSo : TokenSo
    {
        [Tooltip("写进 target 的字号（点）。")]
        [SerializeField] private float _fontSize = 32f;

        public override Type TargetType => typeof(TMP_Text);

        public float FontSize => _fontSize;

        public override void Apply(Component target)
        {
            if (!EnsureTarget(target))
            {
                return;
            }

            ((TMP_Text)target).fontSize = _fontSize;
        }
    }
}
