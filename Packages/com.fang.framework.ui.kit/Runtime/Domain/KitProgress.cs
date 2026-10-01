using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Fang.Framework.UI.Kit
{
    public sealed class KitProgress : MonoBehaviour
    {
        [Tooltip("填充（Image 的 Type 为 Sliced，靠锚点拉宽）。")]
        [SerializeField] private Image _fill;

        [Tooltip("数值文字。没有数值文字的进度条留空。")]
        [SerializeField] private TMP_Text _value;

        public Image Fill => _fill;

        public TMP_Text Value => _value;

        // 包内预制体的填充是 Sliced 的 9 宫格：Filled 模式只发一个 quad、不认 border，
        // 横向填充会把圆角压扁并把右端切成直角，所以这里改用锚点拉宽（与 Unity 的 Slider 填充同款）。
        // 保留 Filled 分支只为不弄坏别人已经做好的旧预制体。
        public float Normalized
        {
            get
            {
                if (_fill == null)
                {
                    return 0f;
                }

                return _fill.type == Image.Type.Filled ? _fill.fillAmount : _fill.rectTransform.anchorMax.x;
            }
        }

        public void SetValue(float normalized)
        {
            normalized = Mathf.Clamp01(normalized);

            if (_fill != null)
            {
                if (_fill.type == Image.Type.Filled)
                {
                    _fill.fillAmount = normalized;
                }
                else
                {
                    var rect = _fill.rectTransform;
                    rect.anchorMax = new Vector2(normalized, rect.anchorMax.y);
                }
            }

            if (_value != null)
            {
                _value.text = Mathf.RoundToInt(normalized * 100f) + "%";
            }
        }

        public void SetValue(float current, float max, string format = "{0}/{1}")
        {
            SetValue(max <= 0f ? 0f : current / max);

            if (_value != null)
            {
                _value.text = string.Format(format, current, max);
            }
        }
    }
}
