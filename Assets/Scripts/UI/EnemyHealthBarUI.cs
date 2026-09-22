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
                targetEntity.OnHealthChanged += UpdateHealth;
                UpdateHealth(targetEntity.CurrentHp, targetEntity.MaxHp);
            }
        }

        private void OnDestroy()
        {
            if (targetEntity != null)
            {
                targetEntity.OnHealthChanged -= UpdateHealth;
            }
        }

        public void BindEntity(CombatEntity entity)
        {
            if (targetEntity != null) targetEntity.OnHealthChanged -= UpdateHealth;
            targetEntity = entity;
            if (targetEntity != null)
            {
                targetEntity.OnHealthChanged += UpdateHealth;
                UpdateHealth(targetEntity.CurrentHp, targetEntity.MaxHp);
            }
        }

        private void UpdateHealth(float current, float max)
        {
            float ratio = max > 0 ? Mathf.Clamp01(current / max) : 0f;

            if (nameAndHpText != null && targetEntity != null)
            {
                nameAndHpText.text = $"{targetEntity.name}\n<size=80%>{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)} HP</size>";
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
