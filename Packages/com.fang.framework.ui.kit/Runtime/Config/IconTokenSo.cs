using System;
using UnityEngine;
using UnityEngine.UI;

namespace Fang.Framework.UI.Kit
{
    [CreateAssetMenu(fileName = "IconToken", menuName = "Fang Framework/UI Kit/图标 Token")]
    public sealed class IconTokenSo : TokenSo
    {
        [Tooltip("写进 target 的图标 Sprite；留空 = 不碰图标。")]
        [SerializeField] private Sprite _sprite;

        [Tooltip("勾上才把下面的颜色写进 target；不勾 = 不碰颜色。")]
        [SerializeField] private bool _useColor;

        [Tooltip("写进 target 的颜色。")]
        [SerializeField] private Color _color = Color.white;

        public override Type TargetType => typeof(Image);

        public override void Apply(Component target)
        {
            if (!EnsureTarget(target))
            {
                return;
            }

            var image = (Image)target;

            if (_sprite != null)
            {
                image.sprite = _sprite;
            }

            if (_useColor)
            {
                image.color = _color;
            }
        }
    }
}
