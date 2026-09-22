namespace Fang.Framework.UI.Editor
{
    public sealed class UIPanelCreateOptions
    {
        public string PanelName { get; set; }

        public UIPanelTrack Track { get; set; }

        public string LayerId { get; set; }

        public int SortOrder { get; set; }

        public bool BlocksInput { get; set; }

        public bool ClosePreviousOnOpen { get; set; }
    }
}
