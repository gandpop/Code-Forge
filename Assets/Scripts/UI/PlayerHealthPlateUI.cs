using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CodeForge.Combat;

namespace CodeForge.UI
{
    public class PlayerHealthPlateUI : MonoBehaviour
    {
        [SerializeField] private PlayerCombatController player;
        [SerializeField] private TextMeshProUGUI hpText;
        [SerializeField] private Image healthFillImage;

        private void Start()
        {
            if (player == null)
            {
                player = FindFirstObjectByType<PlayerCombatController>();
            }

            if (player != null)
            {
                player.OnHealthChanged += HandleHealthChanged;
                player.OnShieldChanged += HandleShieldChanged;
                RefreshDisplay();
            }
        }

        private void OnDestroy()
        {
            if (player != null)
            {
                player.OnHealthChanged -= HandleHealthChanged;
                player.OnShieldChanged -= HandleShieldChanged;
            }
        }

        public void BindPlayer(PlayerCombatController targetPlayer)
        {
            if (player != null)
            {
                player.OnHealthChanged -= HandleHealthChanged;
                player.OnShieldChanged -= HandleShieldChanged;
            }

            player = targetPlayer;

            if (player != null)
            {
                player.OnHealthChanged += HandleHealthChanged;
                player.OnShieldChanged += HandleShieldChanged;
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
            if (player == null) return;

            float current = player.CurrentHp;
            float max = player.MaxHp;
            float ratio = max > 0 ? Mathf.Clamp01(current / max) : 0f;

            if (hpText != null)
            {
                string shieldBadge = player.CurrentShield > 0 ? $" <color=#55AAFF>[+{player.CurrentShield} SHIELD]</color>" : "";
                hpText.text = $"<b>PLAYER</b>   <size=85%>{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)} HP{shieldBadge}</size>";
            }

            if (healthFillImage != null)
            {
                healthFillImage.fillAmount = ratio;
                if (ratio > 0.5f)
                    healthFillImage.color = new Color(0.2f, 0.78f, 0.35f, 1f); // Green
                else if (ratio > 0.2f)
                    healthFillImage.color = new Color(0.95f, 0.75f, 0.15f, 1f); // Yellow
                else
                    healthFillImage.color = new Color(0.92f, 0.25f, 0.25f, 1f); // Red
            }
        }
    }
}
