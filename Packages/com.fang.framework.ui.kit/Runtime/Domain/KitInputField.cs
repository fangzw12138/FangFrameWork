using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Fang.Framework.UI.Kit
{
    public sealed class KitInputField : MonoBehaviour
    {
        [Tooltip("输入框底图。")]
        [SerializeField] private Image _background;

        [Tooltip("输入框本体。")]
        [SerializeField] private TMP_InputField _input;

        [Tooltip("占位文字。")]
        [SerializeField] private TMP_Text _placeholder;

        [Tooltip("前置图标。没有图标的输入框留空。")]
        [SerializeField] private Image _icon;

        public Image Background => _background;

        public TMP_InputField Input => _input;

        public TMP_Text Placeholder => _placeholder;

        public Image Icon => _icon;

        public string Text
        {
            get => _input == null ? string.Empty : _input.text;
            set
            {
                if (_input != null)
                {
                    _input.text = value;
                }
            }
        }

        public void SetPlaceholder(string text)
        {
            if (_placeholder != null)
            {
                _placeholder.text = text;
            }
        }
    }
}
