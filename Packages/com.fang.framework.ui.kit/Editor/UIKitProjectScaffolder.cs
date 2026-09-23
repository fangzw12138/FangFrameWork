using System.IO;
using UnityEditor;
using UnityEngine;

namespace Fang.Framework.UI.Kit.Editor
{
    public static class UIKitProjectScaffolder
    {
        public static UIKitProjectScaffoldResult CreateProject(UIKitProjectCreateOptions options)
        {
            var result = new UIKitProjectScaffoldResult(false);

            if (options == null)
            {
                result.Add("缺少项目参数。");
                return result;
            }

            var folder = UIKitScaffoldPaths.NormalizeFolderPath(options.ProjectFolder);
            if (folder == null)
            {
                result.Add("项目位置必须是 Assets 下的工程相对路径：" + options.ProjectFolder);
                return result;
            }

            if (!UIKitScaffoldPaths.TryValidateProjectName(options.ProjectName, out var validatedName, out var nameError))
            {
                result.Add(nameError);
                return result;
            }

            var projectPath = folder + "/" + validatedName + ".asset";
            if (AssetDatabase.LoadAssetAtPath<UIKitProjectSo>(projectPath) != null)
            {
                result.Add("项目配置已存在：" + projectPath);
                return result;
            }

            EnsureFolder(folder);

            var displayName = string.IsNullOrWhiteSpace(options.DisplayName) ? validatedName : options.DisplayName.Trim();

            var project = ScriptableObject.CreateInstance<UIKitProjectSo>();
            var serialized = new SerializedObject(project);
            serialized.FindProperty("_id").stringValue = validatedName;
            serialized.FindProperty("_displayName").stringValue = displayName;
            serialized.FindProperty("_description").stringValue = options.Description == null ? string.Empty : options.Description.Trim();
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(project, projectPath);
            AssetDatabase.SaveAssets();

            result.CreatedPaths.Add(projectPath);
            result.Success = true;
            result.Add("项目配置已创建：" + projectPath + "。");
            return result;
        }

        /// <summary>
        /// 造一个 UI 空壳预制体（根节点 = RectTransform + TokenMatch），并登记进项目 SO 的预制体列表。
        /// 尺寸不填，由你自己在预制体里摆。
        /// </summary>
        public static UIKitProjectScaffoldResult CreatePrefabShell(UIKitProjectSo project, string prefabFolder, string prefabName)
        {
            var result = new UIKitProjectScaffoldResult(false);

            if (project == null)
            {
                result.Add("缺少项目配置。");
                return result;
            }

            var folder = UIKitScaffoldPaths.NormalizeFolderPath(prefabFolder);
            if (folder == null)
            {
                result.Add("创建位置必须是 Assets 下的工程相对路径：" + prefabFolder);
                return result;
            }

            if (!UIKitScaffoldPaths.TryValidateAssetName(prefabName, "预制体名", out var validatedName, out var nameError))
            {
                result.Add(nameError);
                return result;
            }

            var prefabPath = UIKitScaffoldPaths.PrefabAssetPath(folder, validatedName);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                result.Add("预制体已存在：" + prefabPath);
                return result;
            }

            EnsureFolder(folder);

            var root = new GameObject(validatedName, typeof(RectTransform));
            try
            {
                root.AddComponent<TokenMatch>();
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Append(project, "_prefabs", prefab);
                result.CreatedPaths.Add(prefabPath);
                result.Success = true;
                result.Add("预制体已创建：" + prefabPath + "。");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            return result;
        }

        /// <summary>
        /// 把草稿 token 落成正式资产：资产名写进 <c>_id</c>，<paramref name="matchId"/> 写进 <c>_matchId</c>
        /// （匹配 id 就是手填的字符串），并登记进项目 SO 的 token 库。
        /// </summary>
        public static UIKitProjectScaffoldResult CreateToken(UIKitProjectSo project, TokenSo draft, string tokenFolder, string assetName, string matchId)
        {
            var result = new UIKitProjectScaffoldResult(false);

            if (project == null)
            {
                result.Add("缺少项目配置。");
                return result;
            }

            if (draft == null)
            {
                result.Add("缺少 token 草稿。");
                return result;
            }

            var folder = UIKitScaffoldPaths.NormalizeFolderPath(tokenFolder);
            if (folder == null)
            {
                result.Add("创建位置必须是 Assets 下的工程相对路径：" + tokenFolder);
                return result;
            }

            if (!UIKitScaffoldPaths.TryValidateAssetName(assetName, "资产名", out var validatedName, out var nameError))
            {
                result.Add(nameError);
                return result;
            }

            var tokenPath = UIKitScaffoldPaths.TokenAssetPath(folder, validatedName);
            if (AssetDatabase.LoadAssetAtPath<TokenSo>(tokenPath) != null)
            {
                result.Add("资产已存在：" + tokenPath);
                return result;
            }

            EnsureFolder(folder);

            var token = ScriptableObject.CreateInstance(draft.GetType());
            EditorUtility.CopySerialized(draft, token);

            var serialized = new SerializedObject(token);
            serialized.FindProperty("_id").stringValue = validatedName;
            serialized.FindProperty("_matchId").stringValue = matchId == null ? string.Empty : matchId.Trim();
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(token, tokenPath);
            AssetDatabase.SaveAssets();

            Append(project, "_tokens", token);

            result.CreatedPaths.Add(tokenPath);
            result.Success = true;
            result.Add("token 已创建：" + tokenPath + "。");
            return result;
        }

        private static void Append(UIKitProjectSo project, string listField, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(project);
            var list = serialized.FindProperty(listField);
            var index = list.arraySize;
            list.arraySize = index + 1;
            list.GetArrayElementAtIndex(index).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(project);
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            var parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            var leaf = Path.GetFileName(folderPath);

            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf))
            {
                return;
            }

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
