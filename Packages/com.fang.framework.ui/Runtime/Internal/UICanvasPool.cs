using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Fang.Framework.UI
{
    internal sealed class UICanvasPool
    {
        private const string CanvasNamePrefix = "UICanvas";
        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        private readonly Dictionary<int, Canvas> _canvases = new Dictionary<int, Canvas>();
        private Transform _root;

        public void SetRoot(Transform root)
        {
            _root = root;
        }

        public Canvas GetOrCreate(int sortingOrder)
        {
            if (_canvases.TryGetValue(sortingOrder, out var existing) && existing != null)
            {
                return existing;
            }

            var host = new GameObject(CanvasNamePrefix + "_" + sortingOrder);
            if (_root != null)
            {
                host.transform.SetParent(_root, false);
            }

            var canvas = host.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = host.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.referenceResolution = ReferenceResolution;

            host.AddComponent<GraphicRaycaster>();

            _canvases[sortingOrder] = canvas;
            return canvas;
        }
    }
}
