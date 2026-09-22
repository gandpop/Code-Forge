namespace CodeForge.Data
{
    public enum TokenRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    public static class TokenRarityExtensions
    {
        public static float GetDefaultSpawnChance(this TokenRarity rarity)
        {
            return rarity switch
            {
                TokenRarity.Common => 100f,
                TokenRarity.Uncommon => 60f,
                TokenRarity.Rare => 30f,
                TokenRarity.Epic => 15f,
                TokenRarity.Legendary => 5f,
                _ => 100f
            };
        }
    }
}
