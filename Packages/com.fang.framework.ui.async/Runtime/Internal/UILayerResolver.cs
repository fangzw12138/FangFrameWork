using System.Collections.Generic;

namespace Fang.Framework.UI.Async
{
    internal static class UILayerResolver
    {
        public static int Resolve(IReadOnlyList<UILayerConfigDataSo> layers, UIPanelConfigDataSo config)
        {
            if (config == null)
            {
                return 0;
            }

            return ResolveBaseSortOrder(layers, config.LayerId) + config.SortOrder;
        }

        public static UILayerConfigDataSo FindLayer(IReadOnlyList<UILayerConfigDataSo> layers, string layerId)
        {
            if (layers == null || string.IsNullOrEmpty(layerId))
            {
                return null;
            }

            for (var i = 0; i < layers.Count; i++)
            {
                var layer = layers[i];

                if (layer != null && layer.Id == layerId)
                {
                    return layer;
                }
            }

            return null;
        }

        private static int ResolveBaseSortOrder(IReadOnlyList<UILayerConfigDataSo> layers, string layerId)
        {
            var layer = FindLayer(layers, layerId);
            return layer == null ? 0 : layer.BaseSortOrder;
        }
    }
}
