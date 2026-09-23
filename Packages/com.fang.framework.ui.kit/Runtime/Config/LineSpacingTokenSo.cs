using System;
using TMPro;
using UnityEngine;

namespace Fang.Framework.UI.Kit
{
    [CreateAssetMenu(fileName = "LineSpacingToken", menuName = "Fang Framework/UI Kit/行距 Token")]
    public sealed class LineSpacingTokenSo : TokenSo
    {
        [Tooltip("写进 target 的行距（点，可负）。")]
        [SerializeField] private float _lineSpacing;

        public override Type TargetType => typeof(TMP_Text);

        public float LineSpacing => _lineSpacing;

        public override void Apply(Component target)
        {
            if (!EnsureTarget(target))
            {
                return;
            }

            ((TMP_Text)target).lineSpacing = _lineSpacing;
        }
    }
}
