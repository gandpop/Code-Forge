using System.Collections.Generic;
using CodeForge.Combat;

namespace CodeForge.Data
{
    public class CombatContext
    {
        public PlayerCombatController Player { get; }
        public List<EnemyEntity> ActiveEnemies { get; }
        public EnemyEntity CurrentTarget { get; }
        public int TurnCount { get; }
        public int IncomingDamage { get; }

        public CombatContext(PlayerCombatController player, List<EnemyEntity> activeEnemies, EnemyEntity currentTarget = null, int turnCount = 1, int incomingDamage = 0)
        {
            Player = player;
            ActiveEnemies = activeEnemies ?? new List<EnemyEntity>();
            CurrentTarget = currentTarget;
            TurnCount = turnCount;
            IncomingDamage = incomingDamage;
        }

        public CombatContext WithTarget(EnemyEntity target)
        {
            return new CombatContext(Player, ActiveEnemies, target, TurnCount, IncomingDamage);
        }

        public CombatContext WithIncomingDamage(int incomingDamage)
        {
            return new CombatContext(Player, ActiveEnemies, CurrentTarget, TurnCount, incomingDamage);
        }
    }
}
