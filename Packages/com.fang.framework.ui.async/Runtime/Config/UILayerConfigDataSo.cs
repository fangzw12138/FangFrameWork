using Fang.Framework;
using UnityEngine;

namespace Fang.Framework.UI.Async
{
    [CreateAssetMenu(fileName = "UILayerConfigData", menuName = "Fang Framework/UI (Async)/层配置")]
    public sealed class UILayerConfigDataSo : ConfigDataSo
    {
        [SerializeField] private int _baseSortOrder;

        public int BaseSortOrder => _baseSortOrder;
    }
}
