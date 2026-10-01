using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Fang.Framework.UI.Kit
{
    public sealed class KitDropdown : MonoBehaviour
    {
        [Tooltip("下拉底图。")]
        [SerializeField] private Image _background;

        [Tooltip("下拉本体。")]
        [SerializeField] private TMP_Dropdown _dropdown;

        [Tooltip("右侧箭头。")]
        [SerializeField] private Image _arrow;

        [Tooltip("列表项底图（模板里的 Item Background）。")]
        [SerializeField] private Image _itemBackground;

        [Tooltip("列表项勾（模板里的 Item Checkmark）。")]
        [SerializeField] private Image _itemCheckmark;

        public Image Background => _background;

        public TMP_Dropdown Target => _dropdown;

        public Image Arrow => _arrow;

        public Image ItemBackground => _itemBackground;

        public Image ItemCheckmark => _itemCheckmark;

        public void SetOptions(IEnumerable<string> options)
        {
            if (_dropdown == null)
            {
                return;
            }

            _dropdown.ClearOptions();
            _dropdown.AddOptions(new List<string>(options));
        }

        public void RefreshShownValue()
        {
            if (_dropdown != null)
            {
                _dropdown.RefreshShownValue();
            }
        }
    }
}
