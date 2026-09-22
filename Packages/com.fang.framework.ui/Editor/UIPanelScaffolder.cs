using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fang.Framework.UI.Editor
{
    public static class UIPanelScaffolder
    {
        private const string PrefabConfigTypeName = nameof(UIPrefabPanelConfigDataSo);
        private const string VisualTreeConfigTypeName = nameof(UIVisualTreePanelConfigDataSo);
        private const string PrefabPanelBaseTypeName = "UIPanelController";
        private const string VisualTreePanelBaseTypeName = "UIVisualTreePanelController";
        private const string NamespaceSuffix = ".UI.Panels";
        private const string ConfigSuffix = "ConfigData";

        public static readonly string[] DefaultProjectFolderNames = { "Panels", "Prefabs", "Scripts", "VisualTrees", "Layers" };

        public static readonly string[] DefaultProjectFolderLabels = { "面板 SO 目录", "面板预制体目录", "控制器脚本目录", "UXML·USS 目录", "层 SO 目录" };

        public static UIPanelScaffoldResult CreateProject(UIProjectCreateOptions options)
        {
            var result = new UIPanelScaffoldResult(false);

            if (options == null)
            {
                result.Add("缺少 UI 项目参数。");
                return result;
            }

            var folder = UIPanelScaffoldPaths.NormalizeFolderPath(options.ProjectFolder);
            if (folder == null)
            {
                result.Add("项目位置必须是 Assets 下的工程相对路径：" + options.ProjectFolder);
                return result;
            }

            if (!UIPanelScaffoldPaths.TryValidateProjectName(options.ProjectName, out var validatedName, out var nameError))
            {
                result.Add(nameError);
                return result;
            }

            if (!UIPanelScaffoldPaths.TryValidateNamespace(options.RootNamespace, out var validatedNamespace, out var namespaceError))
            {
                result.Add(namespaceError);
                return result;
            }

            var rawFolders = new[]
            {
                options.PanelConfigFolder,
                options.PanelPrefabFolder,
                options.PanelScriptFolder,
                options.PanelVisualTreeFolder,
                options.LayerConfigFolder,
            };

            var folders = new string[rawFolders.Length];
            for (var i = 0; i < rawFolders.Length; i++)
            {
                var normalized = UIPanelScaffoldPaths.NormalizeFolderPath(rawFolders[i]);
                if (normalized == null)
                {
                    result.Add(DefaultProjectFolderLabels[i] + "必须是 Assets 下的工程相对路径：" + rawFolders[i]);
                    return result;
                }

                folders[i] = normalized;
            }

            var projectPath = folder + "/" + validatedName + ".asset";
            if (AssetDatabase.LoadAssetAtPath<UIProjectConfigDataSo>(projectPath) != null)
            {
                result.Add("UI 项目已存在：" + projectPath);
                return result;
            }

            for (var i = 0; i < folders.Length; i++)
            {
                EnsureFolder(folders[i]);
            }

            var project = ScriptableObject.CreateInstance<UIProjectConfigDataSo>();
            var serialized = new SerializedObject(project);
            serialized.FindProperty("_id").stringValue = validatedName;
            serialized.FindProperty("_rootNamespace").stringValue = validatedNamespace;
            serialized.FindProperty("_panelConfigFolder").stringValue = folders[0];
            serialized.FindProperty("_panelPrefabFolder").stringValue = folders[1];
            serialized.FindProperty("_panelScriptFolder").stringValue = folders[2];
            serialized.FindProperty("_panelVisualTreeFolder").stringValue = folders[3];
            serialized.FindProperty("_layerConfigFolder").stringValue = folders[4];
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(project, projectPath);
            AssetDatabase.SaveAssets();

            result.CreatedPaths.Add(projectPath);
            result.Success = true;
            result.Add("UI 项目已创建：" + projectPath + "。");
            return result;
        }

        public static UIPanelScaffoldResult CreatePanel(
            UIProjectConfigDataSo project,
            string panelName,
            UIPanelTrack track)
        {
            return CreatePanel(project, new UIPanelCreateOptions
            {
                PanelName = panelName,
                Track = track,
            });
        }

        public static UIPanelScaffoldResult CreatePanel(
            UIProjectConfigDataSo project,
            UIPanelCreateOptions options)
        {
            var result = new UIPanelScaffoldResult(false);

            if (options == null)
            {
                result.Add("缺少面板参数。");
                return result;
            }

            if (!TryResolvePaths(project, options.PanelName, result, out var paths))
            {
                return result;
            }

            if (!UIPanelScaffoldPaths.TryValidateNamespace(project.RootNamespace, out var validatedNamespace, out var namespaceError))
            {
                result.Add(namespaceError);
                return result;
            }

            if (AssetDatabase.LoadAssetAtPath<UIPanelConfigDataSo>(paths.ConfigPath) != null)
            {
                result.Add("面板配置已存在：" + paths.ConfigPath);
                return result;
            }

            var visualTree = options.Track == UIPanelTrack.VisualTree;
            var configTypeName = visualTree ? VisualTreeConfigTypeName : PrefabConfigTypeName;
            var baseTypeName = visualTree ? VisualTreePanelBaseTypeName : PrefabPanelBaseTypeName;
            var namespaceName = validatedNamespace + NamespaceSuffix;

            EnsureFolder(paths.ScriptFolderPath);

            if (visualTree)
            {
                EnsureFolder(paths.VisualTreeFolderPath);
                WriteText(paths.UxmlPath, UIPanelTemplateSource.BuildPanelUxml(paths.PanelName), result);
                WriteText(paths.UssPath, UIPanelTemplateSource.BuildPanelUss(paths.PanelName), result);
                AssetDatabase.ImportAsset(paths.UxmlPath);
                AssetDatabase.ImportAsset(paths.UssPath);
            }

            WriteText(paths.DataScriptPath, UIPanelTemplateSource.BuildDataScript(namespaceName, paths.DataTypeName, configTypeName), result);
            WriteText(
                paths.ControllerScriptPath,
                UIPanelTemplateSource.BuildControllerScript(
                    namespaceName,
                    paths.ControllerTypeName,
                    paths.DataTypeName,
                    baseTypeName,
                    configTypeName,
                    visualTree),
                result);

            AssetDatabase.ImportAsset(paths.DataScriptPath);
            AssetDatabase.ImportAsset(paths.ControllerScriptPath);

            CreateConfigAsset(paths, options, result);
            RegisterPanel(project, paths, result);

            AssetDatabase.SaveAssets();

            result.Success = true;
            result.Add("面板已生成：" + paths.PanelName + "（" + (visualTree ? "UITK" : "UGUI") + "）。");
            if (!visualTree)
            {
                UIPanelPrefabQueue.Enqueue(project, paths.PanelName);
                result.Add("脚本编译完成后自动创建预制体。");
            }

            return result;
        }

        public static UIPanelScaffoldResult CreatePrefab(UIProjectConfigDataSo project, string panelName)
        {
            var result = new UIPanelScaffoldResult(false);

            if (!TryResolvePaths(project, panelName, result, out var paths))
            {
                return result;
            }

            var config = AssetDatabase.LoadAssetAtPath<UIPrefabPanelConfigDataSo>(paths.ConfigPath);
            if (config == null)
            {
                result.Add("未找到 UGUI 面板配置：" + paths.ConfigPath);
                return result;
            }

            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(paths.ControllerScriptPath);
            var controllerType = script == null ? null : script.GetClass();
            if (controllerType == null)
            {
                result.Add("控制器脚本尚未编译完成，等编译结束后重试：" + paths.ControllerScriptPath);
                return result;
            }

            EnsureFolder(paths.PanelPrefabFolderPath);

            var root = new GameObject(paths.PanelName, typeof(RectTransform));
            try
            {
                var rectTransform = root.GetComponent<RectTransform>();
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;

                root.AddComponent(controllerType);

                var prefab = PrefabUtility.SaveAsPrefabAsset(root, paths.PrefabPath);
                if (prefab == null)
                {
                    result.Add("预制体保存失败：" + paths.PrefabPath);
                    return result;
                }

                result.CreatedPaths.Add(paths.PrefabPath);

                var serialized = new SerializedObject(config);
                serialized.FindProperty("_prefab").objectReferenceValue = prefab;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(config);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            AssetDatabase.SaveAssets();

            result.Success = true;
            result.Add("预制体已生成：" + paths.PrefabPath);
            return result;
        }

        public static List<string> GetDeletionPreview(UIProjectConfigDataSo project, UIPanelConfigDataSo config)
        {
            var preview = new List<string>();
            if (project == null || config == null)
            {
                return preview;
            }

            var configPath = AssetDatabase.GetAssetPath(config);
            var paths = new UIPanelScaffoldPaths(project, GetPanelNameFromConfigPath(configPath));

            AddFolderPreview(preview, paths.VisualTreeFolderPath);
            AddFolderPreview(preview, paths.ScriptFolderPath);

            if (AssetDatabase.LoadAssetAtPath<GameObject>(paths.PrefabPath) != null)
            {
                preview.Add("预制体：" + paths.PrefabPath);
            }

            if (!string.IsNullOrEmpty(configPath))
            {
                preview.Add("配置：" + configPath);
            }

            preview.Add("从 UI 项目中摘除：" + config.Id);
            return preview;
        }

        public static UIPanelScaffoldResult DeletePanel(UIProjectConfigDataSo project, UIPanelConfigDataSo config)
        {
            var result = new UIPanelScaffoldResult(false);

            if (project == null)
            {
                result.Add("未选择 UI 项目。");
                return result;
            }

            if (config == null)
            {
                result.Add("面板配置为空。");
                return result;
            }

            var configPath = AssetDatabase.GetAssetPath(config);
            var paths = new UIPanelScaffoldPaths(project, GetPanelNameFromConfigPath(configPath));

            UnregisterPanel(project, config, result);

            DeleteAsset(paths.PrefabPath, result);
            DeleteAsset(paths.VisualTreeFolderPath, result);
            DeleteAsset(paths.ScriptFolderPath, result);

            if (!string.IsNullOrEmpty(configPath))
            {
                DeleteAsset(configPath, result);
            }

            AssetDatabase.SaveAssets();

            result.Success = true;
            result.Add("面板已删除：" + paths.PanelName);
            return result;
        }

        public static List<UIPanelScaffoldInfo> ScanPanels(UIProjectConfigDataSo project)
        {
            var infos = new List<UIPanelScaffoldInfo>();
            if (project == null)
            {
                return infos;
            }

            for (var i = 0; i < project.Panels.Count; i++)
            {
                var config = project.Panels[i];
                if (config == null)
                {
                    continue;
                }

                var configPath = AssetDatabase.GetAssetPath(config);
                var paths = new UIPanelScaffoldPaths(project, GetPanelNameFromConfigPath(configPath));
                var visualTreeConfig = config as UIVisualTreePanelConfigDataSo;

                infos.Add(new UIPanelScaffoldInfo(
                    config,
                    configPath,
                    paths,
                    true,
                    visualTreeConfig != null && AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(paths.UxmlPath) != null,
                    visualTreeConfig != null && AssetDatabase.LoadAssetAtPath<StyleSheet>(paths.UssPath) != null,
                    AssetDatabase.LoadAssetAtPath<MonoScript>(paths.ControllerScriptPath) != null,
                    visualTreeConfig == null && AssetDatabase.LoadAssetAtPath<GameObject>(paths.PrefabPath) != null,
                    visualTreeConfig != null && visualTreeConfig.PanelSettings != null));
            }

            infos.Sort(CompareInfos);
            return infos;
        }

        public static string ValidateProject(UIProjectConfigDataSo project)
        {
            if (project == null)
            {
                return "未选择 UI 项目。";
            }

            var builder = new StringBuilder();

            if (!UIPanelScaffoldPaths.TryValidateProjectFolders(project, out var folderErrors))
            {
                for (var i = 0; i < folderErrors.Count; i++)
                {
                    builder.Append("✗ ").Append(folderErrors[i]).Append('\n');
                }
            }

            if (!UIPanelScaffoldPaths.TryValidateNamespace(project.RootNamespace, out _, out var namespaceError))
            {
                builder.Append("✗ ").Append(namespaceError).Append('\n');
            }

            for (var i = 0; i < project.Panels.Count; i++)
            {
                if (project.Panels[i] == null)
                {
                    builder.Append("✗ 清单第 ").Append(i + 1).Append(" 项为空引用（资产丢失或未指定）。\n");
                }
            }

            var infos = ScanPanels(project);
            var idCounts = new Dictionary<string, int>();
            var layerIds = CollectLayerIds(project);

            for (var i = 0; i < infos.Count; i++)
            {
                var info = infos[i];
                var id = info.PanelId;
                idCounts[id] = idCounts.TryGetValue(id, out var count) ? count + 1 : 1;

                var missing = new List<string>(info.GetMissingItems());
                AddLayerIssue(info, layerIds, missing);

                if (missing.Count == 0)
                {
                    builder.Append("✓ ").Append(info.DisplayName).Append("（").Append(id).Append("）：完整");
                }
                else
                {
                    builder.Append("✗ ").Append(info.DisplayName).Append("（").Append(id).Append("）：").Append(string.Join("、", missing));
                }

                builder.Append('\n');
            }

            foreach (var pair in idCounts)
            {
                if (pair.Value > 1)
                {
                    builder.Append("✗ Id 重复：").Append(pair.Key).Append("（").Append(pair.Value).Append(" 个）\n");
                }
            }

            AppendUnregisteredConfigs(project, builder);

            if (builder.Length == 0)
            {
                return infos.Count == 0 ? "没有面板配置。" : "全部面板完整。";
            }

            return builder.ToString();
        }

        private static int CompareInfos(UIPanelScaffoldInfo left, UIPanelScaffoldInfo right)
        {
            return string.CompareOrdinal(left.PanelId, right.PanelId);
        }

        private static bool TryResolvePaths(
            UIProjectConfigDataSo project,
            string panelName,
            UIPanelScaffoldResult result,
            out UIPanelScaffoldPaths paths)
        {
            paths = null;

            if (project == null)
            {
                result.Add("未选择 UI 项目。");
                return false;
            }

            if (!UIPanelScaffoldPaths.TryValidateProjectFolders(project, out var folderErrors))
            {
                for (var i = 0; i < folderErrors.Count; i++)
                {
                    result.Add(folderErrors[i]);
                }

                return false;
            }

            if (!UIPanelScaffoldPaths.TryValidatePanelName(panelName, out var validatedName, out var nameError))
            {
                result.Add(nameError);
                return false;
            }

            paths = new UIPanelScaffoldPaths(project, validatedName);
            return true;
        }

        private static void CreateConfigAsset(UIPanelScaffoldPaths paths, UIPanelCreateOptions options, UIPanelScaffoldResult result)
        {
            EnsureFolder(paths.PanelConfigFolderPath);

            UIPanelConfigDataSo config;
            if (options.Track == UIPanelTrack.VisualTree)
            {
                var visualTreeConfig = ScriptableObject.CreateInstance<UIVisualTreePanelConfigDataSo>();
                var serialized = new SerializedObject(visualTreeConfig);
                serialized.FindProperty("_visualTreeAsset").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(paths.UxmlPath);

                var styleSheets = serialized.FindProperty("_styleSheets");
                styleSheets.arraySize = 1;
                styleSheets.GetArrayElementAtIndex(0).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<StyleSheet>(paths.UssPath);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                config = visualTreeConfig;
            }
            else
            {
                config = ScriptableObject.CreateInstance<UIPrefabPanelConfigDataSo>();
            }

            var configSerialized = new SerializedObject(config);
            configSerialized.FindProperty("_id").stringValue = paths.PanelName;
            configSerialized.FindProperty("_displayName").stringValue = paths.PanelName;
            configSerialized.FindProperty("_layerId").stringValue = options.LayerId ?? string.Empty;
            configSerialized.FindProperty("_sortOrder").intValue = options.SortOrder;
            configSerialized.FindProperty("_closePreviousOnOpen").boolValue = options.ClosePreviousOnOpen;
            configSerialized.FindProperty("_blocksInput").boolValue = options.BlocksInput;
            configSerialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(config, paths.ConfigPath);
            result.CreatedPaths.Add(paths.ConfigPath);
        }

        private static void RegisterPanel(UIProjectConfigDataSo project, UIPanelScaffoldPaths paths, UIPanelScaffoldResult result)
        {
            if (project == null)
            {
                result.Add("未选择 UI 项目，面板未登记。");
                return;
            }

            var config = AssetDatabase.LoadAssetAtPath<UIPanelConfigDataSo>(paths.ConfigPath);
            if (config == null)
            {
                result.Add("面板配置未导入，未登记。");
                return;
            }

            var serialized = new SerializedObject(project);
            var panels = serialized.FindProperty("_panels");

            for (var i = 0; i < panels.arraySize; i++)
            {
                if (panels.GetArrayElementAtIndex(i).objectReferenceValue == config)
                {
                    return;
                }
            }

            panels.InsertArrayElementAtIndex(panels.arraySize);
            panels.GetArrayElementAtIndex(panels.arraySize - 1).objectReferenceValue = config;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(project);
            result.Add("已登记到 UI 项目。");
        }

        private static void UnregisterPanel(UIProjectConfigDataSo project, UIPanelConfigDataSo config, UIPanelScaffoldResult result)
        {
            if (project == null)
            {
                return;
            }

            var serialized = new SerializedObject(project);
            var panels = serialized.FindProperty("_panels");

            for (var i = 0; i < panels.arraySize; i++)
            {
                if (panels.GetArrayElementAtIndex(i).objectReferenceValue != config)
                {
                    continue;
                }

                panels.DeleteArrayElementAtIndex(i);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(project);
                result.Add("已从 UI 项目中摘除。");
                return;
            }
        }

        private static HashSet<string> CollectLayerIds(UIProjectConfigDataSo project)
        {
            var layerIds = new HashSet<string>();
            for (var i = 0; i < project.Layers.Count; i++)
            {
                var layer = project.Layers[i];
                if (layer != null && !string.IsNullOrWhiteSpace(layer.Id))
                {
                    layerIds.Add(layer.Id);
                }
            }

            return layerIds;
        }

        private static void AddLayerIssue(UIPanelScaffoldInfo info, HashSet<string> layerIds, List<string> missing)
        {
            var layerId = info.LayerId;
            if (!string.IsNullOrWhiteSpace(layerId) && !layerIds.Contains(layerId))
            {
                missing.Add("层 '" + layerId + "' 不在 UI 项目的 Layers 里");
            }
        }

        private static void AppendUnregisteredConfigs(UIProjectConfigDataSo project, StringBuilder builder)
        {
            var folder = UIPanelScaffoldPaths.NormalizeFolderPath(project.PanelConfigFolder);
            if (folder == null || !AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            var registered = new HashSet<UIPanelConfigDataSo>();
            for (var i = 0; i < project.Panels.Count; i++)
            {
                if (project.Panels[i] != null)
                {
                    registered.Add(project.Panels[i]);
                }
            }

            var guids = new List<string>();
            guids.AddRange(AssetDatabase.FindAssets("t:" + PrefabConfigTypeName, new[] { folder }));
            guids.AddRange(AssetDatabase.FindAssets("t:" + VisualTreeConfigTypeName, new[] { folder }));

            var seen = new HashSet<string>();
            for (var i = 0; i < guids.Count; i++)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(assetPath) || !seen.Add(assetPath))
                {
                    continue;
                }

                var config = AssetDatabase.LoadAssetAtPath<UIPanelConfigDataSo>(assetPath);
                if (config == null || registered.Contains(config))
                {
                    continue;
                }

                builder.Append("✗ 未登记（目录里存在但不在清单）：").Append(assetPath).Append('\n');
            }
        }

        private static void WriteText(string assetPath, string content, UIPanelScaffoldResult result)
        {
            File.WriteAllText(Path.GetFullPath(assetPath), content);
            result.CreatedPaths.Add(assetPath);
        }

        private static void DeleteAsset(string assetPath, UIPanelScaffoldResult result)
        {
            if (string.IsNullOrEmpty(assetPath) || AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) == null)
            {
                return;
            }

            AssetDatabase.DeleteAsset(assetPath);
            result.Add("已删除：" + assetPath);
        }

        private static void AddFolderPreview(List<string> preview, string folderPath)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            preview.Add("目录：" + folderPath);
        }

        private static void EnsureFolder(string assetFolderPath)
        {
            if (string.IsNullOrEmpty(assetFolderPath) || AssetDatabase.IsValidFolder(assetFolderPath))
            {
                return;
            }

            var parent = Path.GetDirectoryName(assetFolderPath);
            if (string.IsNullOrEmpty(parent))
            {
                return;
            }

            parent = parent.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(assetFolderPath));
        }

        private static string GetPanelNameFromConfigPath(string configPath)
        {
            var fileName = Path.GetFileNameWithoutExtension(configPath);
            if (fileName.EndsWith(ConfigSuffix, StringComparison.Ordinal) && fileName.Length > ConfigSuffix.Length)
            {
                return fileName.Substring(0, fileName.Length - ConfigSuffix.Length);
            }

            return fileName;
        }
    }
}
