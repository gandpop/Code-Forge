using UnityEngine;

namespace CodeForge.Data
{
    [CreateAssetMenu(fileName = "NewStanceToken", menuName = "CodeForge/Tokens/Stance Token")]
    public class StanceTokenSO : CodeTokenSO
    {
        [Header("Stance Settings")]
        public PlayerStance stance = PlayerStance.Balanced;
        public float damagePercentModifier = 0f; // e.g., +0.40f for Berserk (+40%)
        public float flatDamageModifier = 0f;    // e.g., -2f for Guardian
        public int bonusStartingShield = 0;      // e.g., +15 for Guardian
        public float maxHpModifier = 0f;         // e.g., -20f for Berserk

        private void Reset()
        {
            tokenType = CodeTokenType.Stance;
            SyncSyntax();
        }

        private void OnValidate()
        {
            tokenType = CodeTokenType.Stance;
            SyncSyntax();
        }

        private void SyncSyntax()
        {
            codeDisplaySyntax = $"Stance.{stance}";
            if (string.IsNullOrEmpty(tokenName))
            {
                tokenName = $"Stance.{stance}";
            }
        }

        public override string GetFormattedCodeString()
        {
            return $"Stance.{stance}";
        }
    }
}
