using System;
using UnityEngine;
using UnityEngine.UI;

namespace Fang.Framework.UI.Kit
{
    [CreateAssetMenu(fileName = "ColorToken", menuName = "Fang Framework/UI Kit/颜色 Token")]
    public sealed class ColorTokenSo : TokenSo
    {
        [Tooltip("写进 target 的颜色值。target 类型是 Graphic（Image / TMP_Text 等都算）。")]
        [SerializeField] private Color _color = Color.white;

        public override Type TargetType => typeof(Graphic);

        public Color Value => _color;

        public override void Apply(Component target)
        {
            if (!EnsureTarget(target))
            {
                return;
            }

            ((Graphic)target).color = _color;
        }
    }
}
