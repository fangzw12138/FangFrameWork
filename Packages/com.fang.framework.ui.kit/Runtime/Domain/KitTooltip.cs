using System.Collections;
using TMPro;
using UnityEngine;

namespace Fang.Framework.UI.Kit
{
    public sealed class KitTooltip : MonoBehaviour
    {
        [Tooltip("整条提示的 CanvasGroup（淡入淡出写它的 alpha）。")]
        [SerializeField] private CanvasGroup _group;

        [Tooltip("文字。")]
        [SerializeField] private TMP_Text _text;

        [Tooltip("淡入淡出时长（秒）。")]
        [SerializeField] private float _fadeDuration = 0.1f;

        private Coroutine _fade;

        public CanvasGroup Group => _group;

        public TMP_Text Text => _text;

        public void Show(string text, Vector2 screenPosition)
        {
            if (_text != null)
            {
                _text.text = text;
            }

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            transform.position = new Vector3(screenPosition.x, screenPosition.y, 0f);
            Play(0f, 1f, false);
        }

        public void Hide()
        {
            Play(1f, 0f, true);
        }

        private void Play(float from, float to, bool deactivate)
        {
            StopFade();

            if (!isActiveAndEnabled || _fadeDuration <= 0f)
            {
                SetAlpha(to);

                if (deactivate)
                {
                    gameObject.SetActive(false);
                }

                return;
            }

            _fade = StartCoroutine(Fade(from, to, deactivate));
        }

        private IEnumerator Fade(float from, float to, bool deactivate)
        {
            var time = 0f;

            while (time < _fadeDuration)
            {
                time += Time.unscaledDeltaTime;
                SetAlpha(Mathf.Lerp(from, to, Mathf.Clamp01(time / _fadeDuration)));
                yield return null;
            }

            SetAlpha(to);
            _fade = null;

            if (deactivate)
            {
                gameObject.SetActive(false);
            }
        }

        private void SetAlpha(float alpha)
        {
            if (_group != null)
            {
                _group.alpha = alpha;
            }
        }

        private void StopFade()
        {
            if (_fade == null)
            {
                return;
            }

            StopCoroutine(_fade);
            _fade = null;
        }
    }
}
