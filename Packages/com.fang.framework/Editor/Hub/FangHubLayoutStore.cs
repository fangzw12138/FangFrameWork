using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Fang.Framework.Editor.Hub
{
    public static class FangHubLayoutStore
    {
        private const string Scope = "FangHub";
        private const string LayoutKey = "Layout";

        public static FangHubLayout Load(IReadOnlyList<FangHubPageDescriptor> pages)
        {
            if (pages == null)
            {
                throw new ArgumentNullException(nameof(pages));
            }

            var json = EditorPrefs.GetString(FangEditorPrefs.BuildKey(Scope, LayoutKey), string.Empty);
            var layout = Parse(json);
            if (layout == null)
            {
                layout = FangHubLayout.CreateDefault(pages);
            }

            layout.Normalize();
            layout.Reconcile(pages);
            return layout;
        }

        public static void Save(FangHubLayout layout)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            EditorPrefs.SetString(FangEditorPrefs.BuildKey(Scope, LayoutKey), JsonUtility.ToJson(layout));
        }

        private static FangHubLayout Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return JsonUtility.FromJson<FangHubLayout>(json);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
    }
}
