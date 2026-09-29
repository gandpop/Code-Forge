using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CodeForge.Combat;

namespace CodeForge.UI
{
    [ExecuteAlways]
    public class PlayerHealthPlateUI : MonoBehaviour
    {
        [SerializeField] private PlayerCombatController player;
        [SerializeField] private TextMeshProUGUI hpText;
        [SerializeField] private Image healthFillImage;
        [SerializeField] private Image shieldFillImage;

        private float targetHealthFill = 1f;
        private float targetShieldFill = 0f;

        private void Awake()
        {
            EnsureShieldFill();
            EnsureValidSprites();
            if (hpText != null)
            {
                hpText.fontSize = 20f;
            }
            if (player == null)
            {
                player = FindFirstObjectByType<PlayerCombatController>();
            }
            RefreshDisplay();
        }

        private void OnEnable()
        {
            EnsureShieldFill();
            EnsureValidSprites();
            if (player == null)
            {
                player = FindFirstObjectByType<PlayerCombatController>();
            }
            RefreshDisplay();
        }

        private static Sprite squareSprite;

        public static Sprite GetOrCreateSquareSprite()
        {
            if (squareSprite != null) return squareSprite;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.SetPixels(new Color[] { Color.white, Color.white, Color.white, Color.white });
            tex.Apply();
            squareSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
            squareSprite.name = "FlatSquareSprite";
            return squareSprite;
        }

        public void EnsureValidSprites()
        {
            var flat = GetOrCreateSquareSprite();

            if (healthFillImage != null)
            {
                if (healthFillImage.sprite == null || healthFillImage.sprite.name == "UISprite")
                {
                    healthFillImage.sprite = flat;
                }
                healthFillImage.type = Image.Type.Filled;
                healthFillImage.fillMethod = Image.FillMethod.Horizontal;
                healthFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            }

            if (shieldFillImage != null)
            {
                if ((shieldFillImage.sprite == null || shieldFillImage.sprite.name == "UISprite") && healthFillImage != null)
                {
                    shieldFillImage.sprite = flat;
                }
                shieldFillImage.type = Image.Type.Filled;
                shieldFillImage.fillMethod = Image.FillMethod.Horizontal;
                shieldFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            }
        }

        public void EnsureShieldFill()
        {
            if (shieldFillImage == null && healthFillImage != null)
            {
                var barBg = healthFillImage.transform.parent;
                if (barBg != null)
                {
                    var existingShield = barBg.Find("ShieldFill");
                    if (existingShield != null)
                    {
                        shieldFillImage = existingShield.GetComponent<Image>();
                    }
                    else
                    {
                        var shieldObj = Instantiate(healthFillImage.gameObject, barBg);
                        shieldObj.name = "ShieldFill";
                        shieldObj.transform.SetSiblingIndex(healthFillImage.transform.GetSiblingIndex() + 1);
                        shieldFillImage = shieldObj.GetComponent<Image>();
                        shieldFillImage.color = new Color(0.22f, 0.73f, 1.0f, 0.80f); // Electric Cyan Shield
                        shieldFillImage.fillAmount = 0f;
                    }
                }
            }
            EnsureValidSprites();
        }

        private void Start()
        {
            EnsureShieldFill();
            EnsureValidSprites();
            if (hpText != null)
            {
                hpText.fontSize = 20f;
            }

            if (player == null)
            {
                player = FindFirstObjectByType<PlayerCombatController>();
            }

            if (player != null)
            {
                var editor = FindFirstObjectByType<CodeEditorPanelUI>();
                if (editor != null && editor.GetMaxHealth() > 0)
                {
                    player.SetMaxHealth(editor.GetMaxHealth());
                }
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

        public void RefreshDisplay()
        {
            if (player == null)
            {
                player = FindFirstObjectByType<PlayerCombatController>();
            }

            float current = 20f;
            float max = 20f;
            int shield = 0;

            if (player != null)
            {
                max = player.MaxHp > 0 ? player.MaxHp : (player.MaxHealth > 0 ? player.MaxHealth : 20f);
                current = player.CurrentHp > 0 ? player.CurrentHp : max;
                shield = player.CurrentShield;
            }

            targetHealthFill = max > 0 ? Mathf.Clamp01(current / max) : 1f;
            targetShieldFill = max > 0 ? Mathf.Clamp01((float)shield / max) : 0f;

            // Instant snap for immediate visual feedback
            if (healthFillImage != null)
            {
                healthFillImage.fillAmount = targetHealthFill;

                // Color transition: Green -> Yellow -> Red
                if (targetHealthFill > 0.5f)
                    healthFillImage.color = new Color(0.2f, 0.78f, 0.35f, 1f); // Green
                else if (targetHealthFill > 0.2f)
                    healthFillImage.color = new Color(0.95f, 0.75f, 0.15f, 1f); // Yellow
                else
                    healthFillImage.color = new Color(0.92f, 0.25f, 0.25f, 1f); // Red
            }

            if (shieldFillImage != null)
            {
                shieldFillImage.fillAmount = targetShieldFill;
                shieldFillImage.gameObject.SetActive(targetShieldFill > 0f);
            }

            if (hpText != null)
            {
                string shieldBadge = shield > 0 ? $"  <color=#55AAFF><b>(+{shield} SHIELD)</b></color>" : "";
                hpText.text = $"<size=115%><b>PLAYER</b></size>    <b>{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)} HP</b>{shieldBadge}";
            }
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (!Application.isPlaying)
            {
                EnsureShieldFill();
                EnsureValidSprites();
                RefreshDisplay();
            }
        }
#endif
    }
}
