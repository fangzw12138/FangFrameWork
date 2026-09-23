using System;
using TMPro;
using UnityEngine;

namespace Fang.Framework.UI.Kit
{
    [CreateAssetMenu(fileName = "FontAssetToken", menuName = "Fang Framework/UI Kit/字体 Token")]
    public sealed class FontAssetTokenSo : TokenSo
    {
        [Tooltip("要写进 target 的 TMP 字体资产；Apply 时会连字体材质一起落到预制体上。")]
        [SerializeField] private TMP_FontAsset _font;

        public override Type TargetType => typeof(TMP_Text);

        public TMP_FontAsset Font => _font;

        public override void Apply(Component target)
        {
            if (!EnsureTarget(target))
            {
                return;
            }

            if (_font == null)
            {
                Debug.LogError($"[{GetType().Name}] 字体没填（token id = {Id}）", this);
                return;
            }

            ((TMP_Text)target).font = _font;
        }
    }
}
