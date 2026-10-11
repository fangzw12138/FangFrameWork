using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Fang.Framework.UI.Async
{
    internal sealed class UICanvasPool
    {
        private const string CanvasNamePrefix = "Canvas_";

        private const string LogTag = "[Fang.Framework.UI.Async]";

        private static readonly Vector2 DefaultReferenceResolution = new Vector2(1920f, 1080f);

        private readonly Dictionary<int, Canvas> _canvases = new Dictionary<int, Canvas>();
        private readonly Transform _root;
        private readonly CanvasScalerSettingsProvider _settings;
        private readonly CanvasRenderSettingsProvider _renderSettings;

        public UICanvasPool(
            Transform root,
            CanvasScalerSettingsProvider settings,
            CanvasRenderSettingsProvider renderSettings)
        {
            _root = root;
            _settings = settings;
            _renderSettings = renderSettings;
        }

        public Transform Get(int sortingOrder)
        {
            if (_canvases.TryGetValue(sortingOrder, out var pooled) && pooled != null)
            {
                return pooled.transform;
            }

            var host = new GameObject($"{CanvasNamePrefix}{sortingOrder}", typeof(RectTransform));
            host.transform.SetParent(_root, false);

            var canvas = host.AddComponent<Canvas>();
            canvas.sortingOrder = sortingOrder;
            ApplyRenderSettings(canvas);

            var scaler = host.AddComponent<CanvasScaler>();
            ApplySettings(scaler);

            host.AddComponent<GraphicRaycaster>();

            _canvases[sortingOrder] = canvas;
            return canvas.transform;
        }

        private void ApplyRenderSettings(Canvas canvas)
        {
            var settings = _renderSettings != null ? _renderSettings() : CanvasRenderSettings.Overlay;
            var camera = settings.Mode == UICanvasRenderMode.ScreenSpaceCamera
                ? ResolveCamera(settings.WorldCameraName)
                : null;

            if (settings.Mode == UICanvasRenderMode.ScreenSpaceCamera && camera == null)
            {
                var wanted = string.IsNullOrEmpty(settings.WorldCameraName) ? "Camera.main" : settings.WorldCameraName;

                Debug.LogWarning(
                    $"{LogTag} Canvas '{canvas.name}' requested camera render mode but camera '{wanted}' was not found; falling back to Screen Space - Overlay.");
            }

            if (camera == null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                return;
            }

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;

            var near = camera.nearClipPlane + 0.01f;
            var far = Mathf.Max(near + 0.01f, camera.farClipPlane - 0.01f);

            canvas.planeDistance = Mathf.Clamp(settings.PlaneDistance, near, far);
        }

        private static Camera ResolveCamera(string cameraName)
        {
            if (string.IsNullOrEmpty(cameraName))
            {
                return Camera.main;
            }

            var cameras = Camera.allCameras;

            for (var i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] != null && cameras[i].name == cameraName)
                {
                    return cameras[i];
                }
            }

            return null;
        }

        private void ApplySettings(CanvasScaler scaler)
        {
            var referenceResolution = DefaultReferenceResolution;
            var screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var matchWidthOrHeight = 1f;

            if (_settings != null)
            {
                var current = _settings();
                referenceResolution = current.ReferenceResolution;
                screenMatchMode = current.ScreenMatchMode;
                matchWidthOrHeight = current.MatchWidthOrHeight;
            }

            if (referenceResolution.x <= 0f || referenceResolution.y <= 0f)
            {
                referenceResolution = DefaultReferenceResolution;
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.screenMatchMode = screenMatchMode;
            scaler.matchWidthOrHeight = Mathf.Clamp01(matchWidthOrHeight);
        }
    }
}
