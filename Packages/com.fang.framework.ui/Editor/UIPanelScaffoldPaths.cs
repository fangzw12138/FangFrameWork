using System;
using System.Collections.Generic;

namespace Fang.Framework.UI.Editor
{
    public sealed class UIPanelScaffoldPaths
    {
        private const string ConfigSuffix = "ConfigData";

        public UIPanelScaffoldPaths(UIProjectConfigDataSo project, string panelName)
        {
            Project = project;
            PanelName = panelName ?? string.Empty;
        }

        public UIProjectConfigDataSo Project { get; }

        public string PanelName { get; }

        public string DataTypeName => PanelName + "Data";

        public string ControllerTypeName => PanelName + "Controller";

        public string PanelConfigFolderPath => ResolveFolder(Project == null ? null : Project.PanelConfigFolder);

        public string PanelPrefabFolderPath => ResolveFolder(Project == null ? null : Project.PanelPrefabFolder);

        public string PanelScriptFolderPath => ResolveFolder(Project == null ? null : Project.PanelScriptFolder);

        public string PanelVisualTreeFolderPath => ResolveFolder(Project == null ? null : Project.PanelVisualTreeFolder);

        public string LayerConfigFolderPath => ResolveFolder(Project == null ? null : Project.LayerConfigFolder);

        public string ConfigPath => CombineFile(PanelConfigFolderPath, PanelName + ConfigSuffix + ".asset");

        public string PrefabPath => CombineFile(PanelPrefabFolderPath, PanelName + ".prefab");

        public string ScriptFolderPath => CombineFolder(PanelScriptFolderPath, PanelName);

        public string DataScriptPath => CombineFile(ScriptFolderPath, DataTypeName + ".cs");

        public string ControllerScriptPath => CombineFile(ScriptFolderPath, ControllerTypeName + ".cs");

        public string VisualTreeFolderPath => CombineFolder(PanelVisualTreeFolderPath, PanelName);

        public string UxmlPath => CombineFile(VisualTreeFolderPath, PanelName + ".uxml");

        public string UssPath => CombineFile(VisualTreeFolderPath, PanelName + ".uss");

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

        public static bool TryValidateProjectFolders(UIProjectConfigDataSo project, out List<string> errors)
        {
            errors = new List<string>();

            if (project == null)
            {
                errors.Add("未选择 UI 项目。");
                return false;
            }

            ValidateFolder(project.PanelConfigFolder, "面板 SO 目录", errors);
            ValidateFolder(project.PanelPrefabFolder, "面板预制体目录", errors);
            ValidateFolder(project.PanelScriptFolder, "控制器脚本目录", errors);
            ValidateFolder(project.PanelVisualTreeFolder, "面板 UXML·USS 目录", errors);
            ValidateFolder(project.LayerConfigFolder, "层 SO 目录", errors);

            return errors.Count == 0;
        }

        public static bool TryValidatePanelName(string rawName, out string panelName, out string error)
        {
            return TryValidateIdentifier(rawName, "面板名称", out panelName, out error);
        }

        public static bool TryValidateProjectName(string rawName, out string projectName, out string error)
        {
            return TryValidateIdentifier(rawName, "项目名称", out projectName, out error);
        }

        private static bool TryValidateIdentifier(string rawName, string label, out string value, out string error)
        {
            value = null;
            error = null;

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

            value = trimmed;
            return true;
        }

        public static bool TryValidateNamespace(string rawNamespace, out string namespaceName, out string error)
        {
            namespaceName = null;
            error = null;

            if (string.IsNullOrWhiteSpace(rawNamespace))
            {
                error = "根命名空间不能为空。";
                return false;
            }

            var trimmed = rawNamespace.Trim();
            var segments = trimmed.Split('.');
            for (var i = 0; i < segments.Length; i++)
            {
                if (!IsValidIdentifier(segments[i]))
                {
                    error = "根命名空间必须是合法标识符（点分段，字母或下划线开头）：" + trimmed;
                    return false;
                }
            }

            namespaceName = trimmed;
            return true;
        }

        private static string ResolveFolder(string rawFolderPath)
        {
            return NormalizeFolderPath(rawFolderPath) ?? string.Empty;
        }

        private static string CombineFile(string folderPath, string fileName)
        {
            return string.IsNullOrEmpty(folderPath) ? string.Empty : folderPath + "/" + fileName;
        }

        private static string CombineFolder(string folderPath, string childFolderName)
        {
            return string.IsNullOrEmpty(folderPath) ? string.Empty : folderPath + "/" + childFolderName;
        }

        private static void ValidateFolder(string rawFolderPath, string label, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(rawFolderPath))
            {
                errors.Add(label + "未填写。");
                return;
            }

            if (NormalizeFolderPath(rawFolderPath) == null)
            {
                errors.Add(label + "必须是 Assets 下的工程相对路径：" + rawFolderPath);
            }
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
