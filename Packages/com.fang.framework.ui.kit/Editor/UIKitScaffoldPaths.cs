using System;
using System.IO;
using UnityEditor;

namespace Fang.Framework.UI.Kit.Editor
{
    public static class UIKitScaffoldPaths
    {
        private const string RootFolder = "Assets";

        public static string NormalizeFolderPath(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return null;
            }

            var normalized = folderPath.Replace('\\', '/').Trim();
            while (normalized.EndsWith("/", StringComparison.Ordinal))
            {
                normalized = normalized.Substring(0, normalized.Length - 1);
            }

            if (!normalized.StartsWith("Assets", StringComparison.Ordinal))
            {
                return null;
            }

            if (normalized.Length > "Assets".Length && normalized["Assets".Length] != '/')
            {
                return null;
            }

            return normalized;
        }

        public static bool TryValidateProjectName(string rawName, out string projectName, out string error)
        {
            return TryValidateAssetName(rawName, "项目名称", out projectName, out error);
        }

        public static bool TryValidateAssetName(string rawName, string what, out string assetName, out string error)
        {
            assetName = null;
            error = null;

            var label = string.IsNullOrEmpty(what) ? "名称" : what;

            if (string.IsNullOrWhiteSpace(rawName))
            {
                error = label + "不能为空。";
                return false;
            }

            var trimmed = rawName.Trim();
            if (!IsValidIdentifier(trimmed))
            {
                error = label + "必须是合法标识符（字母或下划线开头，只含字母、数字、下划线）：" + trimmed;
                return false;
            }

            assetName = trimmed;
            return true;
        }

        public static string ProjectAssetPath(string projectFolder, string projectName)
        {
            return AssetPath(projectFolder, projectName, ".asset");
        }

        public static string TokenAssetPath(string tokenFolder, string tokenName)
        {
            return AssetPath(tokenFolder, tokenName, ".asset");
        }

        public static string PrefabAssetPath(string prefabFolder, string prefabName)
        {
            return AssetPath(prefabFolder, prefabName, ".prefab");
        }

        /// <summary>新建时的默认位置：当前选中资产所在目录，取不到就用 Assets。</summary>
        public static string ResolveDefaultFolder()
        {
            var selected = Selection.activeObject;
            if (selected == null)
            {
                return RootFolder;
            }

            var path = AssetDatabase.GetAssetPath(selected);
            if (string.IsNullOrEmpty(path))
            {
                return RootFolder;
            }

            if (AssetDatabase.IsValidFolder(path))
            {
                return NormalizeFolderPath(path) ?? RootFolder;
            }

            var folder = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(folder))
            {
                return RootFolder;
            }

            return NormalizeFolderPath(folder) ?? RootFolder;
        }

        private static string AssetPath(string folder, string name, string extension)
        {
            var normalized = NormalizeFolderPath(folder);
            if (normalized == null || string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }

            return normalized + "/" + name + extension;
        }

        private static bool IsValidIdentifier(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            if (!char.IsLetter(value[0]) && value[0] != '_')
            {
                return false;
            }

            for (var i = 1; i < value.Length; i++)
            {
                if (!char.IsLetterOrDigit(value[i]) && value[i] != '_')
                {
                    return false;
                }
            }

            return true;
        }
    }
}
