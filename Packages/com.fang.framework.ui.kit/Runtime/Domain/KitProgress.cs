using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Fang.Framework.UI.Kit
{
    public sealed class KitProgress : MonoBehaviour
    {
        [Tooltip("填充（Image 的 Type 为 Filled）。")]
        [SerializeField] private Image _fill;

        [Tooltip("数值文字。没有数值文字的进度条留空。")]
        [SerializeField] private TMP_Text _value;

        public Image Fill => _fill;

        public TMP_Text Value => _value;

        public float Normalized => _fill == null ? 0f : _fill.fillAmount;

        public void SetValue(float normalized)
        {
            normalized = Mathf.Clamp01(normalized);

            if (_fill != null)
            {
                _fill.fillAmount = normalized;
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
