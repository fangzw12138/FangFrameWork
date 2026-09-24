using System;
using System.IO;
using UnityEditor;

namespace Fang.Framework.CommandConsole.Editor
{
    /// <summary>新建调试项目时的路径 / 命名校验（纯逻辑，可单测）。</summary>
    public static class DebugScaffoldPaths
    {
        private const string RootFolder = "Assets";

        /// <summary>必须是 Assets 下的工程相对路径；否则返回 null。</summary>
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

            if (!normalized.StartsWith(RootFolder, StringComparison.Ordinal))
            {
                return null;
            }

            if (normalized.Length > RootFolder.Length && normalized[RootFolder.Length] != '/')
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
            var normalized = NormalizeFolderPath(projectFolder);
            if (normalized == null || string.IsNullOrEmpty(projectName))
            {
                return string.Empty;
            }

            return normalized + "/" + projectName + ".asset";
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
