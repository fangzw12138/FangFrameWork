using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fang.Framework.UI.Async.Tests
{
    internal static class TestConfigs
    {
        private const string AssetsFolder = "Packages/com.fang.framework.ui.async/Tests/Editor/Assets";

        private static readonly List<ScriptableObject> Created = new List<ScriptableObject>();

        private static readonly List<GameObject> Templates = new List<GameObject>();

        public static VisualTreeAsset TestVisualTreeAsset => LoadAsset<VisualTreeAsset>("UIAsyncTestPanel.uxml");

        public static PanelSettings TestPanelSettings => LoadAsset<PanelSettings>("UIAsyncTestPanelSettings.asset");

        public static UILayerConfigDataSo CreateLayer(string id, int baseSortOrder)
        {
            var config = Track(ScriptableObject.CreateInstance<UILayerConfigDataSo>());
            var serialized = new SerializedObject(config);
            serialized.FindProperty("_id").stringValue = id;
            serialized.FindProperty("_baseSortOrder").intValue = baseSortOrder;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        public static UIVisualTreePanelConfigDataSo CreateVisualTreePanel(
            string id,
            string layerId,
            int sortOrder,
            bool closePreviousOnOpen,
            bool blocksInput,
            bool withVisualTreeAsset = true,
            bool withPanelSettings = true)
        {
            var config = Track(ScriptableObject.CreateInstance<UIVisualTreePanelConfigDataSo>());
            var serialized = new SerializedObject(config);
            serialized.FindProperty("_id").stringValue = id;
            ApplyPanelFields(serialized, layerId, sortOrder, closePreviousOnOpen, blocksInput);

            if (withVisualTreeAsset)
            {
                serialized.FindProperty("_visualTreeAsset").objectReferenceValue = TestVisualTreeAsset;
            }

            if (withPanelSettings)
            {
                serialized.FindProperty("_panelSettings").objectReferenceValue = TestPanelSettings;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        public static UIPrefabPanelConfigDataSo CreatePrefabPanel(
            string id,
            string layerId,
            int sortOrder,
            bool closePreviousOnOpen,
            bool blocksInput,
            bool withPrefab = true)
        {
            var config = Track(ScriptableObject.CreateInstance<UIPrefabPanelConfigDataSo>());
            var serialized = new SerializedObject(config);
            serialized.FindProperty("_id").stringValue = id;
            ApplyPanelFields(serialized, layerId, sortOrder, closePreviousOnOpen, blocksInput);

            if (withPrefab)
            {
                serialized.FindProperty("_prefab").objectReferenceValue = CreatePrefabTemplate();
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        public static GameObject CreatePrefabTemplate()
        {
            var template = new GameObject("UIAsyncTestPrefabPanel", typeof(RectTransform));
            template.AddComponent<TestPrefabPanelController>();
            Templates.Add(template);
            return template;
        }

        public static UIProjectConfigDataSo CreateProject(UILayerConfigDataSo[] layers, UIPanelConfigDataSo[] panels)
        {
            var project = Track(ScriptableObject.CreateInstance<UIProjectConfigDataSo>());
            var serialized = new SerializedObject(project);

            serialized.FindProperty("_id").stringValue = "TestUIProject";
            serialized.FindProperty("_rootNamespace").stringValue = "Game.UI";
            serialized.FindProperty("_layerConfigFolder").stringValue = "Assets/Game/UI/Layers";
            serialized.FindProperty("_panelConfigFolder").stringValue = "Assets/Game/UI/Panels/Config";
            serialized.FindProperty("_panelPrefabFolder").stringValue = "Assets/Game/UI/Panels/Prefabs";
            serialized.FindProperty("_panelScriptFolder").stringValue = "Assets/Game/UI/Panels/Scripts";
            serialized.FindProperty("_panelVisualTreeFolder").stringValue = "Assets/Game/UI/Panels/VisualTrees";

            var layersProperty = serialized.FindProperty("_layers");
            layersProperty.arraySize = layers?.Length ?? 0;

            for (var i = 0; i < layersProperty.arraySize; i++)
            {
                layersProperty.GetArrayElementAtIndex(i).objectReferenceValue = layers[i];
            }

            var panelsProperty = serialized.FindProperty("_panels");
            panelsProperty.arraySize = panels?.Length ?? 0;

            for (var i = 0; i < panelsProperty.arraySize; i++)
            {
                panelsProperty.GetArrayElementAtIndex(i).objectReferenceValue = panels[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return project;
        }

        public static void DestroyAll()
        {
            for (var i = Templates.Count - 1; i >= 0; i--)
            {
                if (Templates[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(Templates[i]);
                }
            }

            Templates.Clear();

            for (var i = Created.Count - 1; i >= 0; i--)
            {
                if (Created[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(Created[i]);
                }
            }

            Created.Clear();
        }

        private static TConfig Track<TConfig>(TConfig config) where TConfig : ScriptableObject
        {
            Created.Add(config);
            return config;
        }

        private static TAsset LoadAsset<TAsset>(string fileName) where TAsset : Object
        {
            var path = AssetsFolder + "/" + fileName;
            var asset = AssetDatabase.LoadAssetAtPath<TAsset>(path);

            if (asset == null)
            {
                Debug.LogError($"[TestConfigs] missing test asset '{path}'.");
            }

            return asset;
        }

        private static void ApplyPanelFields(
            SerializedObject serialized,
            string layerId,
            int sortOrder,
            bool closePreviousOnOpen,
            bool blocksInput)
        {
            serialized.FindProperty("_layerId").stringValue = layerId ?? string.Empty;
            serialized.FindProperty("_sortOrder").intValue = sortOrder;
            serialized.FindProperty("_closePreviousOnOpen").boolValue = closePreviousOnOpen;
            serialized.FindProperty("_blocksInput").boolValue = blocksInput;
        }
    }
}
