using UnityEditor;

namespace Fang.Framework.Editor.Hub
{
    public static class FangHubPageState
    {
        public static string GetString(string scope, string key, string defaultValue = "")
        {
            return EditorPrefs.GetString(FangEditorPrefs.BuildKey(scope, key), defaultValue);
        }

        public static void SetString(string scope, string key, string value)
        {
            EditorPrefs.SetString(FangEditorPrefs.BuildKey(scope, key), value ?? string.Empty);
        }

        public static bool GetBool(string scope, string key, bool defaultValue = false)
        {
            return EditorPrefs.GetBool(FangEditorPrefs.BuildKey(scope, key), defaultValue);
        }

        public static void SetBool(string scope, string key, bool value)
        {
            EditorPrefs.SetBool(FangEditorPrefs.BuildKey(scope, key), value);
        }

        public static int GetInt(string scope, string key, int defaultValue = 0)
        {
            return EditorPrefs.GetInt(FangEditorPrefs.BuildKey(scope, key), defaultValue);
        }

        public static void SetInt(string scope, string key, int value)
        {
            EditorPrefs.SetInt(FangEditorPrefs.BuildKey(scope, key), value);
        }
    }
}
