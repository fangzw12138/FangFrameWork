using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Fang.Framework.UI.Kit
{
    public sealed class KitSkillButton : KitButton
    {
        [Tooltip("冷却遮罩（Image 的 Type 为 Filled / Radial 360）。")]
        [SerializeField] private Image _cooldownMask;

        [Tooltip("冷却剩余秒数文字。")]
        [SerializeField] private TMP_Text _cooldownText;

        [Tooltip("等级文字。")]
        [SerializeField] private TMP_Text _levelText;

        public Image CooldownMask => _cooldownMask;

        public TMP_Text CooldownText => _cooldownText;

        public TMP_Text LevelText => _levelText;

        public bool Ready => _cooldownMask == null || _cooldownMask.fillAmount <= 0f;

        public void SetCooldown(float current, float total)
        {
            var normalized = total <= 0f ? 0f : Mathf.Clamp01(current / total);

            if (_cooldownMask != null)
            {
                _cooldownMask.fillAmount = normalized;
                _cooldownMask.enabled = normalized > 0f;
            }

            if (_cooldownText != null)
            {
                _cooldownText.text = normalized > 0f ? Mathf.CeilToInt(current).ToString() : string.Empty;
            }
        }

        public void SetLevel(int level)
        {
            if (_levelText != null)
            {
                _levelText.text = level > 0 ? level.ToString() : string.Empty;
            }
        }
    }
}
