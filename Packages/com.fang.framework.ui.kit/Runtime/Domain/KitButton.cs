using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Fang.Framework.UI.Kit
{
    public sealed class KitButton : MonoBehaviour
    {
        [Tooltip("按钮底图。纯文字按钮没有底图，留空。")]
        [SerializeField] private Image _background;

        [Tooltip("按钮本体（Selectable），5 态颜色写在它身上。")]
        [SerializeField] private Button _button;

        [Tooltip("按钮图标。没有图标的按钮留空。")]
        [SerializeField] private Image _icon;

        [Tooltip("按钮文字。没有文字的按钮留空。")]
        [SerializeField] private TMP_Text _label;

        [SerializeField] private UiMotionSo _onClick;

        [SerializeField] private UiMotionSo _onLocked;

        public Image Background => _background;

        public Button Target => _button;

        public Image Icon => _icon;

        public TMP_Text Label => _label;

        public UiMotionSo OnClick
        {
            get => _onClick;
            set => _onClick = value;
        }

        public UiMotionSo OnLocked
        {
            get => _onLocked;
            set => _onLocked = value;
        }
    }
}
