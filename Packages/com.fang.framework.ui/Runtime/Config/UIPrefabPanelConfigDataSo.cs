using UnityEngine;

namespace Fang.Framework.UI
{
    [CreateAssetMenu(fileName = "UIPrefabPanelConfigData", menuName = "Fang Framework/UI/UGUI 面板配置")]
    public class UIPrefabPanelConfigDataSo : UIPanelConfigDataSo
    {
        [SerializeField] private GameObject _prefab;

        public GameObject Prefab => _prefab;
    }
}
