using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Fang.Framework.UI.Kit
{
    public sealed class KitSwitch : MonoBehaviour
    {
        private const float SlideDuration = 0.1f;

        [Tooltip("开关本体（Toggle）。")]
        [SerializeField] private Toggle _toggle;

        [Tooltip("轨道底图。")]
        [SerializeField] private Image _track;

        [Tooltip("滑块，锚在轨道左右两端之间滑动。")]
        [SerializeField] private Image _knob;

        [Tooltip("开关文字。没有文字的开关留空。")]
        [SerializeField] private TMP_Text _label;

        [Tooltip("滑块与轨道边缘的水平间距。")]
        [SerializeField] private float _knobPadding;

        [Tooltip("轨道「开」颜色，由 token 写。")]
        [SerializeField] private Color _trackOnColor = Color.white;

        [Tooltip("轨道「关」颜色，由 token 写。")]
        [SerializeField] private Color _trackOffColor = Color.white;

        [Tooltip("滑块「开」颜色，由 token 写。")]
        [SerializeField] private Color _knobOnColor = Color.white;

        [Tooltip("滑块「关」颜色，由 token 写。")]
        [SerializeField] private Color _knobOffColor = Color.white;

        private Coroutine _slide;

        public Toggle Target => _toggle;

        public Image Track => _track;

        public Image Knob => _knob;

        public TMP_Text Label => _label;

        public bool IsOn => _toggle != null && _toggle.isOn;

        public Color TrackOnColor
        {
            get => _trackOnColor;
            set => _trackOnColor = value;
        }

        public Color TrackOffColor
        {
            get => _trackOffColor;
            set => _trackOffColor = value;
        }

        public Color KnobOnColor
        {
            get => _knobOnColor;
            set => _knobOnColor = value;
        }

        public Color KnobOffColor
        {
            get => _knobOffColor;
            set => _knobOffColor = value;
        }

        public void SetState(bool isOn, bool instant)
        {
            if (_toggle != null)
            {
                _toggle.SetIsOnWithoutNotify(isOn);
            }

            Refresh(instant);
        }

        private void OnEnable()
        {
            if (_toggle != null)
            {
                _toggle.onValueChanged.AddListener(OnToggleValueChanged);
            }

            Refresh(true);
        }

        private void OnDisable()
        {
            if (_toggle != null)
            {
                _toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
            }

            StopSlide();
        }

        private void OnToggleValueChanged(bool value)
        {
            Refresh(false);
        }

        private void Refresh(bool instant)
        {
            var isOn = IsOn;

            if (_track != null)
            {
                _track.color = isOn ? _trackOnColor : _trackOffColor;
            }

            if (_knob == null)
            {
                return;
            }

            _knob.color = isOn ? _knobOnColor : _knobOffColor;

            var target = KnobPosition(isOn);

            if (instant || !isActiveAndEnabled)
            {
                StopSlide();
                _knob.rectTransform.anchoredPosition = target;
                return;
            }

            StopSlide();
            _slide = StartCoroutine(SlideTo(target));
        }

        private Vector2 KnobPosition(bool isOn)
        {
            var rect = _knob.rectTransform;
            var anchor = new Vector2(isOn ? 1f : 0f, 0.5f);

            rect.anchorMin = anchor;
            rect.anchorMax = anchor;

            return new Vector2(isOn ? -_knobPadding : _knobPadding, 0f);
        }

        private void StopSlide()
        {
            if (_slide == null)
            {
                return;
            }

            StopCoroutine(_slide);
            _slide = null;
        }

        private IEnumerator SlideTo(Vector2 target)
        {
            var rect = _knob.rectTransform;
            var from = rect.anchoredPosition;
            var time = 0f;

            while (time < SlideDuration)
            {
                time += Time.unscaledDeltaTime;
                rect.anchoredPosition = Vector2.Lerp(from, target, Mathf.Clamp01(time / SlideDuration));
                yield return null;
            }

            rect.anchoredPosition = target;
            _slide = null;
        }
    }
}
