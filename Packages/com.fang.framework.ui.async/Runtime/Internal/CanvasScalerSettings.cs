using UnityEngine;
using UnityEngine.UI;

namespace Fang.Framework.UI.Async
{
    internal readonly struct CanvasScalerSettings
    {
        public CanvasScalerSettings(
            Vector2 referenceResolution,
            CanvasScaler.ScreenMatchMode screenMatchMode,
            float matchWidthOrHeight)
        {
            ReferenceResolution = referenceResolution;
            ScreenMatchMode = screenMatchMode;
            MatchWidthOrHeight = matchWidthOrHeight;
        }

        public Vector2 ReferenceResolution { get; }

        public CanvasScaler.ScreenMatchMode ScreenMatchMode { get; }

        public float MatchWidthOrHeight { get; }
    }

    internal delegate CanvasScalerSettings CanvasScalerSettingsProvider();
}
