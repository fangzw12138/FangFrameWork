using UnityEngine;

namespace Fang.Framework.UI.Async
{
    [CreateAssetMenu(fileName = "UIPrefabPanelConfigData", menuName = "Fang Framework/UI (Async)/UGUI 面板配置")]
    public sealed class UIPrefabPanelConfigDataSo : UIPanelConfigDataSo
    {
        [SerializeField] private GameObject _prefab;

        public GameObject Prefab => _prefab;
    }
}
