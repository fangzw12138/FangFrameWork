namespace Fang.Framework.UI.Async
{
    internal readonly struct CanvasRenderSettings
    {
        public CanvasRenderSettings(UICanvasRenderMode mode, string worldCameraName, float planeDistance)
        {
            Mode = mode;
            WorldCameraName = worldCameraName;
            PlaneDistance = planeDistance;
        }

        public UICanvasRenderMode Mode { get; }

        public string WorldCameraName { get; }

        public float PlaneDistance { get; }

        public static CanvasRenderSettings Overlay
        {
            get { return new CanvasRenderSettings(UICanvasRenderMode.ScreenSpaceOverlay, null, 1f); }
        }
    }

    internal delegate CanvasRenderSettings CanvasRenderSettingsProvider();
}
