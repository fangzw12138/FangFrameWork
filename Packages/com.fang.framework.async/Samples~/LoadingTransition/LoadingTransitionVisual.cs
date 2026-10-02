using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Fang.Framework.Async.LoadingTransition
{
    internal sealed class LoadingTransitionVisual : MonoBehaviour
    {
        private const float BarWidth = 600f;
        private const float BarHeight = 24f;
        private const float BarPadding = 2f;
        private const float LabelHeight = 44f;
        private const float LabelOffset = 72f;
        private const float FadeOutDuration = 0.3f;
        private const int ProgressLogSteps = 4;

        private CanvasGroup _group;
        private Image _fill;
        private TextMeshProUGUI _label;
        private AsyncLifecycleBehaviour _root;
        private int _version;
        private int _progressLogStep;

        public void Initialize()
        {
            UiFactory.Stretch((RectTransform)transform);

            _group = gameObject.AddComponent<CanvasGroup>();

            var mask = UiFactory.CreateImage(transform, "Mask", new Color(0.03f, 0.04f, 0.06f, 1f));
            UiFactory.Stretch((RectTransform)mask.transform);

            var bar = UiFactory.CreateImage(transform, "BarBackground", new Color(1f, 1f, 1f, 0.16f));
            var barRect = (RectTransform)bar.transform;
            barRect.anchorMin = new Vector2(0.5f, 0.5f);
            barRect.anchorMax = new Vector2(0.5f, 0.5f);
            barRect.sizeDelta = new Vector2(BarWidth, BarHeight);
            barRect.anchoredPosition = Vector2.zero;

            _fill = UiFactory.CreateImage(bar.transform, "BarFill", new Color(0.34f, 0.74f, 1f, 1f));
            var fillRect = (RectTransform)_fill.transform;
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.anchoredPosition = new Vector2(BarPadding, 0f);
            fillRect.sizeDelta = new Vector2(0f, -BarPadding * 2f);

            _label = UiFactory.CreateText(transform, "ProgressLabel", 30f, "0%");
            var labelRect = (RectTransform)_label.transform;
            labelRect.anchorMin = new Vector2(0.5f, 0.5f);
            labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.sizeDelta = new Vector2(BarWidth, LabelHeight);
            labelRect.anchoredPosition = new Vector2(0f, LabelOffset);

            SetProgress(0f);
            SetVisible(false);
        }

        public void Watch(AsyncLifecycleBehaviour root)
        {
            Unsubscribe();

            _version++;
            _progressLogStep = 0;
            _root = root;

            root.InitStarted += OnInitStarted;
            root.InitProgressChanged += OnInitProgressChanged;
            root.InitCompleted += OnInitCompleted;

            SetVisible(true);

            Debug.Log($"LoadingTransition: overlay shown, watching {root.name}");

            if (root.IsInitialized)
            {
                SetProgress(1f);
                FadeOutAsync(_version).Forget();
                return;
            }

            SetProgress(root.InitProgress);
        }

        public void OnDispose()
        {
            _version++;
            Unsubscribe();
        }

        private void OnInitStarted(AsyncLifecycleBehaviour root)
        {
            if (!IsCurrent(root))
            {
                return;
            }

            _version++;
            _progressLogStep = 0;

            Debug.Log($"LoadingTransition: {root.name} init started");

            SetVisible(true);
            SetProgress(0f);
        }

        private void OnInitProgressChanged(AsyncLifecycleBehaviour root, float progress)
        {
            if (!IsCurrent(root))
            {
                return;
            }

            SetProgress(progress);

            var step = Mathf.FloorToInt(progress * ProgressLogSteps);

            if (step <= _progressLogStep)
            {
                return;
            }

            _progressLogStep = step;
            Debug.Log($"LoadingTransition: {root.name} progress {Mathf.RoundToInt(progress * 100f)}%");
        }

        private void OnInitCompleted(AsyncLifecycleBehaviour root)
        {
            if (!IsCurrent(root))
            {
                return;
            }

            Debug.Log($"LoadingTransition: {root.name} init completed");

            SetProgress(1f);
            FadeOutAsync(_version).Forget();
        }

        private bool IsCurrent(AsyncLifecycleBehaviour root)
        {
            return ReferenceEquals(root, _root);
        }

        private void Unsubscribe()
        {
            if (_root == null)
            {
                return;
            }

            _root.InitStarted -= OnInitStarted;
            _root.InitProgressChanged -= OnInitProgressChanged;
            _root.InitCompleted -= OnInitCompleted;
            _root = null;
        }

        private void SetProgress(float value)
        {
            value = Mathf.Clamp01(value);
            _fill.rectTransform.sizeDelta = new Vector2((BarWidth - BarPadding * 2f) * value, -BarPadding * 2f);
            _label.text = Mathf.RoundToInt(value * 100f) + "%";
        }

        private void SetVisible(bool visible)
        {
            _group.alpha = visible ? 1f : 0f;
            _group.blocksRaycasts = visible;
        }

        private async UniTaskVoid FadeOutAsync(int version)
        {
            for (var elapsed = 0f; elapsed < FadeOutDuration; elapsed += Time.unscaledDeltaTime)
            {
                if (_version != version)
                {
                    return;
                }

                _group.alpha = 1f - elapsed / FadeOutDuration;
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            if (_version != version)
            {
                return;
            }

            SetVisible(false);
            Debug.Log("LoadingTransition: overlay hidden");
        }
    }
}
