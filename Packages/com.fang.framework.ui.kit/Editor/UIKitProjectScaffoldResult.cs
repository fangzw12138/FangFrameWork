using System.Collections.Generic;

namespace Fang.Framework.UI.Kit.Editor
{
    public sealed class UIKitProjectScaffoldResult
    {
        public UIKitProjectScaffoldResult(bool success)
        {
            Success = success;
        }

        public bool Success { get; set; }

        public List<string> Messages { get; } = new List<string>();

        public List<string> CreatedPaths { get; } = new List<string>();

        public void Add(string message)
        {
            Messages.Add(message);
        }

        public string ToStatusMessage()
        {
            return Messages.Count == 0 ? string.Empty : string.Join(" ", Messages);
        }
    }
}
