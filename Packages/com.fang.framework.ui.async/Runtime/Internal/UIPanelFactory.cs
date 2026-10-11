using UnityEngine;
using UnityEngine.UIElements;

namespace Fang.Framework.UI.Async
{
    internal static class UIPanelFactory
    {
        private const string LogTag = "[Fang.Framework.UI.Async]";

        public static TPanel Create<TPanel, TConfig, TData>(TConfig config, Transform parent, int sortOrder)
            where TPanel : UIPanelController<TConfig, TData>
            where TConfig : UIPanelConfigDataSo
            where TData : UIPanelData<TConfig>
        {
            if (config is UIPrefabPanelConfigDataSo prefabConfig)
            {
                return CreateFromPrefab<TPanel, TConfig, TData>(prefabConfig, parent);
            }

            if (config is UIVisualTreePanelConfigDataSo visualTreeConfig)
            {
                return CreateFromVisualTree<TPanel, TConfig, TData>(visualTreeConfig, parent, sortOrder);
            }

            Debug.LogError($"{LogTag} Panel config type '{config?.GetType().FullName}' is not supported.");
            return null;
        }

        private static TPanel CreateFromPrefab<TPanel, TConfig, TData>(UIPrefabPanelConfigDataSo config, Transform parent)
            where TPanel : UIPanelController<TConfig, TData>
            where TConfig : UIPanelConfigDataSo
            where TData : UIPanelData<TConfig>
        {
            if (config.Prefab == null)
            {
                Debug.LogError($"{LogTag} UGUI panel '{config.Id}' has no prefab assigned.");
                return null;
            }

            var instance = Object.Instantiate(config.Prefab, parent, false);
            instance.name = config.Id;

            if (!instance.TryGetComponent(out TPanel panel))
            {
                Debug.LogError(
                    $"{LogTag} UGUI panel '{config.Id}' has no '{typeof(TPanel).FullName}' component on the prefab root.");
                DestroyInstance(instance);
                return null;
            }

            instance.SetActive(false);
            return panel;
        }

        private static TPanel CreateFromVisualTree<TPanel, TConfig, TData>(
            UIVisualTreePanelConfigDataSo config,
            Transform parent,
            int sortOrder)
            where TPanel : UIPanelController<TConfig, TData>
            where TConfig : UIPanelConfigDataSo
            where TData : UIPanelData<TConfig>
        {
            var host = new GameObject(config.Id, typeof(RectTransform));
            host.transform.SetParent(parent, false);

            var panel = host.AddComponent<TPanel>();

            if (!(panel is UIVisualTreePanelController<TConfig, TData> visualTreePanel))
            {
                Debug.LogError(
                    $"{LogTag} UI Toolkit panel '{config.Id}' needs '{typeof(TPanel).FullName}' to derive from '{typeof(UIVisualTreePanelController<TConfig, TData>).FullName}'.");
                DestroyInstance(host);
                return null;
            }

            var root = UIVisualTreeBinder
                .Bind<UIVisualTreePanelController<TConfig, TData>, TConfig, TData>(visualTreePanel, config, sortOrder);

            if (root == null)
            {
                DestroyInstance(host);
                return null;
            }

            visualTreePanel.SetRoot(root);
            return panel;
        }

        private static void DestroyInstance(GameObject instance)
        {
            if (Application.isPlaying)
            {
                Object.Destroy(instance);
            }
            else
            {
                Object.DestroyImmediate(instance);
            }
        }
    }
}
