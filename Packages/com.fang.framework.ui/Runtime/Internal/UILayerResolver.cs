using System.Collections.Generic;

namespace Fang.Framework.UI
{
    internal static class UILayerResolver
    {
        public static int Resolve(IReadOnlyList<UILayerConfigDataSo> layers, string layerId, int panelSortOrder)
        {
            if (layers == null || string.IsNullOrEmpty(layerId))
            {
                return panelSortOrder;
            }

            for (var i = 0; i < layers.Count; i++)
            {
                var layer = layers[i];
                if (layer != null && layer.Id == layerId)
                {
                    return layer.BaseSortOrder + panelSortOrder;
                }
            }

            return panelSortOrder;
        }
    }
}
