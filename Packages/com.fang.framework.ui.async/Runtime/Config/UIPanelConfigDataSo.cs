using Fang.Framework;
using UnityEngine;

namespace Fang.Framework.UI.Async
{
    public abstract class UIPanelConfigDataSo : ConfigDataSo
    {
        [SerializeField] private string _layerId;
        [SerializeField] private int _sortOrder;
        [SerializeField] private bool _closePreviousOnOpen;
        [SerializeField] private bool _blocksInput;
        [SerializeField] private bool _closeOnBack;

        public string LayerId => _layerId;

        public int SortOrder => _sortOrder;

        public bool ClosePreviousOnOpen => _closePreviousOnOpen;

        public bool BlocksInput => _blocksInput;

        public bool CloseOnBack => _closeOnBack;
    }
}
