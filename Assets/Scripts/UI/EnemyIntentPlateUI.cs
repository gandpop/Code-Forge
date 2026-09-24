using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CodeForge.Combat;

namespace CodeForge.UI
{
    public class EnemyIntentPlateUI : MonoBehaviour
    {
        [SerializeField] private EnemyEntity enemyEntity;
        [SerializeField] private TextMeshProUGUI intentText;
        [SerializeField] private Image intentIcon;
        [SerializeField] private Image backgroundPlate;

        private void Start()
        {
            if (enemyEntity == null)
            {
                enemyEntity = GetComponentInParent<EnemyEntity>();
            }

            if (intentText != null)
            {
                intentText.textWrappingMode = TextWrappingModes.NoWrap;
            }

            if (enemyEntity != null)
            {
                enemyEntity.OnIntentChanged += UpdateIntentDisplay;
                UpdateIntentDisplay(enemyEntity.CurrentIntent);
            }
        }

        private void OnDestroy()
        {
            if (enemyEntity != null)
            {
                enemyEntity.OnIntentChanged -= UpdateIntentDisplay;
            }
        }

        public void BindEnemy(EnemyEntity entity)
        {
            if (enemyEntity != null) enemyEntity.OnIntentChanged -= UpdateIntentDisplay;
            enemyEntity = entity;
            if (enemyEntity != null)
            {
                enemyEntity.OnIntentChanged += UpdateIntentDisplay;
                UpdateIntentDisplay(enemyEntity.CurrentIntent);
            }
        }

        public void InitializeAtRuntime(EnemyEntity entity, TextMeshProUGUI text, Image background)
        {
            if (enemyEntity != null) enemyEntity.OnIntentChanged -= UpdateIntentDisplay;
            enemyEntity = entity;
            intentText = text;
            backgroundPlate = background;

            if (intentText != null)
            {
                intentText.textWrappingMode = TextWrappingModes.NoWrap;
            }

            if (enemyEntity != null)
            {
                enemyEntity.OnIntentChanged += UpdateIntentDisplay;
                UpdateIntentDisplay(enemyEntity.CurrentIntent);
            }
        }

        public void UpdateIntentDisplay(EnemyIntent intent)
        {
            if (intentText != null)
            {
                string coloredBadge = intent.intentType switch
                {
                    EnemyIntentType.Attack => $"<color=#FF6B6B><b>{intent.GetBadgeText()}</b></color>",
                    EnemyIntentType.HeavyHit => $"<color=#FF3333><b>{intent.GetBadgeText()}</b></color>",
                    EnemyIntentType.Shield => $"<color=#4DABF7><b>{intent.GetBadgeText()}</b></color>",
                    EnemyIntentType.Charge => $"<color=#FFD43B><b>{intent.GetBadgeText()}</b></color>",
                    EnemyIntentType.Buff => $"<color=#FFA94D><b>{intent.GetBadgeText()}</b></color>",
                    _ => $"<color=#CCCCCC><b>{intent.GetBadgeText()}</b></color>"
                };

                intentText.text = coloredBadge;
            }

            if (backgroundPlate != null)
            {
                backgroundPlate.color = intent.intentType switch
                {
                    EnemyIntentType.HeavyHit => new Color(0.4f, 0.1f, 0.1f, 0.9f),
                    EnemyIntentType.Shield => new Color(0.1f, 0.2f, 0.4f, 0.9f),
                    EnemyIntentType.Charge => new Color(0.35f, 0.25f, 0.05f, 0.9f),
                    _ => new Color(0.12f, 0.12f, 0.16f, 0.9f)
                };
            }
        }
    }
}
