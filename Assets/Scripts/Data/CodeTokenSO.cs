using UnityEngine;

namespace CodeForge.Data
{
    [CreateAssetMenu(fileName = "NewCodeToken", menuName = "CodeForge/Code Token")]
    public class CodeTokenSO : ScriptableObject
    {
        public string tokenName;
        public CodeTokenType tokenType;

        [Header("Rarity Settings (Spawn chance is linked to Rarity)")]
        public TokenRarity rarity = TokenRarity.Common;
        public bool canSpawnAsReward = true;

        [Header("Optional Chance Override")]
        [Tooltip("Leave false to automatically use the rarity's spawn chance. Enable to set a custom chance score.")]
        public bool overrideSpawnChance = false;
        [Range(0f, 100f)] public float customSpawnChance = 100f;

        [Header("Values (Populate based on TokenType)")]
        public float floatValue;
        public int intValue;
        public bool boolValue;
        public TargetPriority targetPriorityValue;

        /// <summary>
        /// Spawn chance linked directly to rarity, unless overridden or disabled.
        /// </summary>
        public float spawnChance
        {
            get => GetSpawnChance();
            set
            {
                overrideSpawnChance = true;
                customSpawnChance = Mathf.Clamp(value, 0f, 100f);
            }
        }

        public float GetSpawnChance()
        {
            if (!canSpawnAsReward) return 0f;
            if (overrideSpawnChance) return customSpawnChance;
            return rarity.GetDefaultSpawnChance();
        }

        public Color GetRarityColor()
        {
            return rarity switch
            {
                TokenRarity.Common => new Color(0.62f, 0.62f, 0.62f, 1f),     // Grey
                TokenRarity.Uncommon => new Color(0.30f, 0.69f, 0.31f, 1f),   // Green
                TokenRarity.Rare => new Color(0.13f, 0.59f, 0.95f, 1f),       // Blue
                TokenRarity.Epic => new Color(0.90f, 0.22f, 0.21f, 1f),       // Red
                TokenRarity.Legendary => new Color(1.0f, 0.84f, 0.0f, 1f),     // Yellow
                _ => Color.white
            };
        }

        public string GetRarityHexColor()
        {
            return rarity switch
            {
                TokenRarity.Common => "#9E9E9E",
                TokenRarity.Uncommon => "#4CAF50",
                TokenRarity.Rare => "#2196F3",
                TokenRarity.Epic => "#E53935",
                TokenRarity.Legendary => "#FFD700",
                _ => "#FFFFFF"
            };
        }

        public string GetFormattedCodeString()
        {
            return tokenType switch
            {
                CodeTokenType.Float => $"{floatValue:0.0}f",
                CodeTokenType.Int => $"{intValue}",
                CodeTokenType.Bool => boolValue ? "true" : "false",
                CodeTokenType.TargetPriority => $"TargetPriority.{targetPriorityValue}",
                _ => "null"
            };
        }
    }
}
