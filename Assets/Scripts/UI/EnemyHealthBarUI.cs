using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CodeForge.Combat;

namespace CodeForge.UI
{
    public class EnemyHealthBarUI : MonoBehaviour
    {
        [SerializeField] private CombatEntity targetEntity;
        [SerializeField] private TextMeshProUGUI nameAndHpText;
        [SerializeField] private Image fillImage;

        private void Start()
        {
            if (targetEntity == null)
            {
                targetEntity = GetComponentInParent<CombatEntity>();
            }

            if (targetEntity != null)
            {
                targetEntity.OnHealthChanged += HandleHealthChanged;
                targetEntity.OnShieldChanged += HandleShieldChanged;
                RefreshDisplay();
            }
        }

        private void OnDestroy()
        {
            if (targetEntity != null)
            {
                targetEntity.OnHealthChanged -= HandleHealthChanged;
                targetEntity.OnShieldChanged -= HandleShieldChanged;
            }
        }

        public void BindEntity(CombatEntity entity)
        {
            if (targetEntity != null)
            {
                targetEntity.OnHealthChanged -= HandleHealthChanged;
                targetEntity.OnShieldChanged -= HandleShieldChanged;
            }

            targetEntity = entity;

            if (targetEntity != null)
            {
                targetEntity.OnHealthChanged += HandleHealthChanged;
                targetEntity.OnShieldChanged += HandleShieldChanged;
                RefreshDisplay();
            }
        }

        private void HandleHealthChanged(float current, float max)
        {
            RefreshDisplay();
        }

        private void HandleShieldChanged(int shield)
        {
            RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            if (targetEntity == null) return;

            float current = targetEntity.CurrentHp;
            float max = targetEntity.MaxHp;
            float ratio = max > 0 ? Mathf.Clamp01(current / max) : 0f;

            if (nameAndHpText != null)
            {
                string shieldBadge = targetEntity.CurrentShield > 0 ? $" <color=#55AAFF>[+{targetEntity.CurrentShield} SHIELD]</color>" : "";
                nameAndHpText.text = $"{targetEntity.name}\n<size=80%>{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)} HP{shieldBadge}</size>";
            }

            if (fillImage != null)
            {
                fillImage.fillAmount = ratio;
                if (ratio > 0.5f)
                    fillImage.color = new Color(0.95f, 0.35f, 0.45f, 1f); // Pink/Red
                else
                    fillImage.color = new Color(0.85f, 0.15f, 0.2f, 1f); // Dark Red
            }
        }
    }
}
