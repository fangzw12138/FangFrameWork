using UnityEngine;

namespace Fang.Framework.UI
{
    [CreateAssetMenu(fileName = "UILayerConfigData", menuName = "Fang Framework/UI/层配置")]
    public sealed class UILayerConfigDataSo : ConfigDataSo
    {
        [SerializeField] private int _baseSortOrder;

        public int BaseSortOrder => _baseSortOrder;
    }
}
