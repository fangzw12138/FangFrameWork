using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Fang.Framework.Async.LoadingTransition
{
    internal static class UiFactory
    {
        private static readonly Color ButtonColor = new Color(1f, 1f, 1f, 0.18f);

        public static Canvas CreateCanvas(Transform parent, string name)
        {
            var host = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            host.transform.SetParent(parent, false);

            var canvas = host.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = host.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        public static Image CreateImage(Transform parent, string name, Color color)
        {
            var host = new GameObject(name, typeof(RectTransform), typeof(Image));
            host.transform.SetParent(parent, false);

            var image = host.GetComponent<Image>();
            image.color = color;
            return image;
        }

        public static TextMeshProUGUI CreateText(Transform parent, string name, float size, string text)
        {
            var host = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            host.transform.SetParent(parent, false);

            var label = host.GetComponent<TextMeshProUGUI>();
            label.fontSize = size;
            label.alignment = TextAlignmentOptions.Center;
            label.text = text;
            label.raycastTarget = false;
            return label;
        }

        public static Button CreateButton(Transform parent, string name, string caption, Vector2 size, Vector2 position)
        {
            var image = CreateImage(parent, name, ButtonColor);
            var rect = (RectTransform)image.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var label = CreateText(image.transform, "Label", 24f, caption);
            Stretch((RectTransform)label.transform);

            return button;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
