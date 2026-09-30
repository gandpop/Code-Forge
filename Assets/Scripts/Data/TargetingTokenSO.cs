using System.Collections.Generic;
using UnityEngine;
using CodeForge.Combat;

namespace CodeForge.Data
{
    [CreateAssetMenu(fileName = "NewTargetingToken", menuName = "CodeForge/Tokens/Targeting Token")]
    public class TargetingTokenSO : CodeTokenSO
    {
        [Header("Targeting Parameters")]
        public TargetPriority priority = TargetPriority.LowestHealth;

        private void Reset()
        {
            tokenType = CodeTokenType.Targeting;
        }

        private void OnValidate()
        {
            tokenType = CodeTokenType.Targeting;
        }

        public EnemyEntity ResolveTarget(List<EnemyEntity> activeEnemies, PlayerCombatController player = null)
        {
            if (activeEnemies == null || activeEnemies.Count == 0) return null;
            if (player == null) player = Object.FindFirstObjectByType<PlayerCombatController>();

            EnemyEntity candidate = null;

            for (int i = 0; i < activeEnemies.Count; i++)
            {
                var enemy = activeEnemies[i];
                if (enemy == null || enemy.IsDead) continue;

                if (candidate == null)
                {
                    candidate = enemy;
                    continue;
                }

                switch (priority)
                {
                    case TargetPriority.LowestHealth:
                        if (enemy.CurrentHp < candidate.CurrentHp)
                            candidate = enemy;
                        break;

                    case TargetPriority.HighestHealth:
                        if (enemy.CurrentHp > candidate.CurrentHp)
                            candidate = enemy;
                        break;

                    case TargetPriority.HighestThreat:
                        int candidateThreat = (candidate.CurrentIntent.intentType == EnemyIntentType.Attack || candidate.CurrentIntent.intentType == EnemyIntentType.HeavyHit)
                            ? candidate.CurrentIntent.projectedValue : 0;
                        int enemyThreat = (enemy.CurrentIntent.intentType == EnemyIntentType.Attack || enemy.CurrentIntent.intentType == EnemyIntentType.HeavyHit)
                            ? enemy.CurrentIntent.projectedValue : 0;

                        if (enemyThreat > candidateThreat || (enemyThreat == candidateThreat && enemy.CurrentHp < candidate.CurrentHp))
                        {
                            candidate = enemy;
                        }
                        break;

                    case TargetPriority.Closest:
                    case TargetPriority.FirstInLine:
                        float playerX = player != null ? player.transform.position.x : -2.0f;
                        float enemyDist = Mathf.Abs(enemy.transform.position.x - playerX);
                        float candidateDist = Mathf.Abs(candidate.transform.position.x - playerX);
                        if (enemyDist < candidateDist)
                        {
                            candidate = enemy;
                        }
                        break;

                    case TargetPriority.BossOnly:
                        if (enemy.name.ToLower().Contains("boss") || enemy.name.ToLower().Contains("tank"))
                        {
                            candidate = enemy;
                        }
                        break;

                    case TargetPriority.ChargingEnemy:
                        bool enemyCharging = enemy.CurrentIntent.intentType == EnemyIntentType.Charge;
                        bool candidateCharging = candidate.CurrentIntent.intentType == EnemyIntentType.Charge;
                        if (enemyCharging && !candidateCharging)
                        {
                            candidate = enemy;
                        }
                        else if (enemyCharging && candidateCharging)
                        {
                            if (enemy.CurrentHp < candidate.CurrentHp)
                            {
                                candidate = enemy;
                            }
                        }
                        else if (!candidateCharging && enemy.CurrentHp < candidate.CurrentHp)
                        {
                            candidate = enemy;
                        }
                        break;

                    case TargetPriority.HighestShield:
                        if (enemy.CurrentShield > candidate.CurrentShield)
                        {
                            candidate = enemy;
                        }
                        else if (enemy.CurrentShield == candidate.CurrentShield && enemy.CurrentHp < candidate.CurrentHp)
                        {
                            candidate = enemy;
                        }
                        break;
                }
            }

            return candidate;
        }
    }
}
