using UnityEngine;

namespace Fang.Framework.UI
{
    internal static class UIPanelFactory
    {
        public static TPanel Create<TPanel, TConfig, TData>(
            TConfig config,
            int sortOrder,
            Transform visualTreeRoot,
            UICanvasPool canvasPool)
            where TPanel : UIPanelController<TConfig, TData>
            where TConfig : UIPanelConfigDataSo
            where TData : UIPanelData<TConfig>
        {
            if (config is UIPrefabPanelConfigDataSo prefabConfig)
            {
                return CreateFromPrefab<TPanel, TConfig, TData>(prefabConfig, sortOrder, canvasPool);
            }

            if (config is UIVisualTreePanelConfigDataSo)
            {
                var host = new GameObject(config.Id);
                if (visualTreeRoot != null)
                {
                    host.transform.SetParent(visualTreeRoot, false);
                }

                return host.AddComponent<TPanel>();
            }

            Debug.LogError($"[UIService] unsupported panel config '{config.GetType().Name}' for panel '{config.Id}'.");
            return null;
        }

        private static TPanel CreateFromPrefab<TPanel, TConfig, TData>(
            UIPrefabPanelConfigDataSo config,
            int sortOrder,
            UICanvasPool canvasPool)
            where TPanel : UIPanelController<TConfig, TData>
            where TConfig : UIPanelConfigDataSo
            where TData : UIPanelData<TConfig>
        {
            if (config.Prefab == null)
            {
                Debug.LogError($"[UIService] prefab is null for panel '{config.Id}'.");
                return null;
            }

            if (config.Prefab.GetComponent<TPanel>() == null)
            {
                Debug.LogError($"[UIService] prefab of panel '{config.Id}' has no {typeof(TPanel).Name} on its root.");
                return null;
            }

            var canvas = canvasPool.GetOrCreate(sortOrder);
            var instance = Object.Instantiate(config.Prefab, canvas.transform, false);
            instance.name = config.Id;
            return instance.GetComponent<TPanel>();
        }
    }
}
