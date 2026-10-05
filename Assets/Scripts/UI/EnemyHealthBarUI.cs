using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CodeForge.Combat;

namespace CodeForge.UI
{
    [ExecuteAlways]
    public class EnemyHealthBarUI : MonoBehaviour
    {
        [SerializeField] private CombatEntity targetEntity;
        [SerializeField] private TextMeshProUGUI nameAndHpText;
        [SerializeField] private Image fillImage;
        [SerializeField] private TMP_FontAsset pixelFont;

        private string cachedPrefix = "<b>";
        private string cachedSuffix = " HP</b>";
        private bool hasNumberPattern = false;
        private bool isTemplateInitialized = false;

        private void InitializeTextTemplate()
        {
            if (isTemplateInitialized) return;
            if (nameAndHpText == null) return;

            string sourceText = nameAndHpText.text;
            if (string.IsNullOrEmpty(sourceText) || sourceText.Trim() == "Enemy")
            {
                cachedPrefix = "<b>";
                cachedSuffix = " HP</b>";
                hasNumberPattern = true;
                isTemplateInitialized = true;
                return;
            }

            var match = System.Text.RegularExpressions.Regex.Match(sourceText, @"\b\d+(\s*/\s*\d+)?\b");
            if (match.Success)
            {
                cachedPrefix = sourceText.Substring(0, match.Index);
                cachedSuffix = sourceText.Substring(match.Index + match.Length);
                hasNumberPattern = true;
            }
            else
            {
                hasNumberPattern = false;
                cachedPrefix = sourceText + " ";
                cachedSuffix = " HP";
            }

            isTemplateInitialized = true;
        }

        private string FormatHpText(float current, float max, int shield)
        {
            InitializeTextTemplate();
            string shieldBadge = shield > 0 ? $" <color=#55AAFF>[+{shield}]</color>" : "";
            return $"{cachedPrefix}{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}{cachedSuffix}{shieldBadge}";
        }

        private void Awake()
        {
            if (targetEntity == null)
            {
                targetEntity = GetComponentInParent<CombatEntity>();
            }
            if (Application.isPlaying)
            {
                isTemplateInitialized = false;
                InitializeTextTemplate();
            }
            EnsureFontAndFill();
            RefreshDisplay();
        }

        private void OnEnable()
        {
            if (targetEntity == null)
            {
                targetEntity = GetComponentInParent<CombatEntity>();
            }

            if (Application.isPlaying)
            {
                isTemplateInitialized = false;
                InitializeTextTemplate();
            }

            if (targetEntity != null)
            {
                targetEntity.OnHealthChanged -= HandleHealthChanged;
                targetEntity.OnShieldChanged -= HandleShieldChanged;
                targetEntity.OnHealthChanged += HandleHealthChanged;
                targetEntity.OnShieldChanged += HandleShieldChanged;
            }
            RefreshDisplay();
        }

        private void EnsureFontAndFill()
        {
#if UNITY_EDITOR
            if (pixelFont == null)
            {
                pixelFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/BoldPixels_SDF.asset");
            }
#endif
            if (nameAndHpText != null && nameAndHpText.font == null && pixelFont != null)
            {
                nameAndHpText.font = pixelFont;
            }

            if (fillImage != null)
            {
                fillImage.sprite = PlayerHealthPlateUI.GetOrCreateSquareSprite();
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Horizontal;
                fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            }
        }

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

            EnsureFontAndFill();

            float current = targetEntity.CurrentHp;
            float max = targetEntity.MaxHp;
            float ratio = max > 0 ? Mathf.Clamp01(current / max) : 0f;

            if (nameAndHpText != null && Application.isPlaying)
            {
                nameAndHpText.text = FormatHpText(current, max, targetEntity.CurrentShield);
            }

            if (fillImage != null)
            {
                fillImage.sprite = PlayerHealthPlateUI.GetOrCreateSquareSprite();
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Horizontal;
                fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
                fillImage.fillAmount = ratio;
                if (ratio > 0.5f)
                    fillImage.color = new Color(0.95f, 0.35f, 0.45f, 1f); // Pink/Red
                else
                    fillImage.color = new Color(0.85f, 0.15f, 0.2f, 1f); // Dark Red
            }
        }
    }
}
