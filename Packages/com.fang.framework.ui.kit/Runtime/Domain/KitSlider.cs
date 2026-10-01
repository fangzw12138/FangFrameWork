using UnityEngine;
using UnityEngine.UI;

namespace Fang.Framework.UI.Kit
{
    public sealed class KitSlider : MonoBehaviour
    {
        [Tooltip("滑条本体。")]
        [SerializeField] private Slider _slider;

        [Tooltip("底图。")]
        [SerializeField] private Image _background;

        [Tooltip("填充区。")]
        [SerializeField] private Image _fill;

        [Tooltip("手柄。")]
        [SerializeField] private Image _handle;

        public Slider Target => _slider;

        public Image Background => _background;

        public Image Fill => _fill;

        public Image Handle => _handle;

        public float Normalized => _slider == null ? 0f : _slider.normalizedValue;

        public void SetValue(float normalized)
        {
            if (_slider != null)
            {
                _slider.normalizedValue = Mathf.Clamp01(normalized);
            }
        }
    }
}
