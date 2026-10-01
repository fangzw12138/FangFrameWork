using System.Collections;
using TMPro;
using UnityEngine;

namespace Fang.Framework.UI.Kit
{
    public sealed class KitToast : MonoBehaviour
    {
        [Tooltip("整条提示的 CanvasGroup（淡入淡出写它的 alpha）。")]
        [SerializeField] private CanvasGroup _group;

        [Tooltip("消息文字。")]
        [SerializeField] private TMP_Text _message;

        [Tooltip("淡入淡出时长（秒）。")]
        [SerializeField] private float _fadeDuration = 0.15f;

        [Tooltip("Show 不传时长时的停留时长（秒）。")]
        [SerializeField] private float _duration = 2f;

        private Coroutine _play;

        public CanvasGroup Group => _group;

        public TMP_Text Message => _message;

        public bool IsVisible => gameObject.activeSelf && (_group == null || _group.alpha > 0f);

        public void Show(string message)
        {
            Show(message, _duration);
        }

        public void Show(string message, float seconds)
        {
            if (_message != null)
            {
                _message.text = message;
            }

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            StopPlay();
            _play = StartCoroutine(Play(seconds));
        }

        public void Hide()
        {
            StopPlay();
            SetAlpha(0f);
            gameObject.SetActive(false);
        }

        private IEnumerator Play(float seconds)
        {
            yield return Fade(0f, 1f);

            if (seconds > 0f)
            {
                yield return new WaitForSecondsRealtime(seconds);
            }

            yield return Fade(1f, 0f);

            _play = null;
            gameObject.SetActive(false);
        }

        private IEnumerator Fade(float from, float to)
        {
            if (_group == null || _fadeDuration <= 0f)
            {
                SetAlpha(to);
                yield break;
            }

            var time = 0f;

            while (time < _fadeDuration)
            {
                time += Time.unscaledDeltaTime;
                SetAlpha(Mathf.Lerp(from, to, Mathf.Clamp01(time / _fadeDuration)));
                yield return null;
            }

            SetAlpha(to);
        }

        private void SetAlpha(float alpha)
        {
            if (_group != null)
            {
                _group.alpha = alpha;
            }
        }

        private void StopPlay()
        {
            if (_play == null)
            {
                return;
            }

            StopCoroutine(_play);
            _play = null;
        }
    }
}
