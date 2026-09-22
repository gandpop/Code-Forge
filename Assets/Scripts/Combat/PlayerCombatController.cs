using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeForge.Data;
using CodeForge.UI;

namespace CodeForge.Combat
{
    public class PlayerCombatController : CombatEntity
    {
        [Header("Runtime Script Configuration")]
        public int attacksPerTurn = 1;
        public float attackDamage = 5f;
        public float damageMultiplier = 1.0f;
        public bool isPiercing = false;
        public TargetPriority targetingPriority = TargetPriority.LowestHealth;

        private Vector3 initialPosition;

        protected override void Awake()
        {
            base.Awake();
            initialPosition = transform.position;
        }

        public void Configure(int attacks, float damage, float multiplier, bool pierce, TargetPriority priority)
        {
            attacksPerTurn = Mathf.Max(1, attacks);
            attackDamage = damage;
            damageMultiplier = Mathf.Max(0.1f, multiplier);
            isPiercing = pierce;
            targetingPriority = priority;
        }

        public IEnumerator ExecutePlayerTurn(List<EnemyEntity> activeEnemies)
        {
            if (IsDead || activeEnemies == null || activeEnemies.Count == 0) yield break;

            for (int strike = 1; strike <= attacksPerTurn; strike++)
            {
                // Clean dead enemies before selecting target
                activeEnemies.RemoveAll(e => e == null || e.IsDead);
                if (activeEnemies.Count == 0) yield break;

                EnemyEntity target = SelectTarget(activeEnemies);
                if (target == null) yield break;

                float finalDamage = Mathf.Round(attackDamage * damageMultiplier);

                // Snappy visual lunge toward enemy stage (up-right)
                yield return StartCoroutine(PerformAttackLunge(new Vector3(0.5f, 0.3f, 0f)));

                if (isPiercing)
                {
                    ConsoleLogUI.Log($"[Turn] Player attack {strike}/{attacksPerTurn}: Piercing strike hits all for {finalDamage} DMG! (Mult: {damageMultiplier:0.0}x)");
                    for (int i = activeEnemies.Count - 1; i >= 0; i--)
                    {
                        if (activeEnemies[i] != null && !activeEnemies[i].IsDead)
                        {
                            activeEnemies[i].TakeDamage(finalDamage);
                        }
                    }
                }
                else
                {
                    ConsoleLogUI.Log($"[Turn] Player attack {strike}/{attacksPerTurn}: Strike on {target.name} for {finalDamage} DMG! (Targeting: {targetingPriority})");
                    target.TakeDamage(finalDamage);
                }

                yield return new WaitForSeconds(0.3f);
            }
        }

        private IEnumerator PerformAttackLunge(Vector3 offset)
        {
            Vector3 startPos = transform.position;
            Vector3 targetPos = startPos + offset;
            float duration = 0.12f;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(targetPos, startPos, elapsed / duration);
                yield return null;
            }

            transform.position = startPos;
        }

        private EnemyEntity SelectTarget(List<EnemyEntity> enemies)
        {
            if (enemies == null || enemies.Count == 0) return null;

            EnemyEntity selected = null;
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != null && !enemies[i].IsDead)
                {
                    selected = enemies[i];
                    break;
                }
            }

            if (selected == null) return null;

            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyEntity current = enemies[i];
                if (current == null || current.IsDead) continue;

                switch (targetingPriority)
                {
                    case TargetPriority.LowestHealth:
                        if (current.CurrentHp < selected.CurrentHp)
                            selected = current;
                        break;
                    case TargetPriority.HighestHealth:
                        if (current.CurrentHp > selected.CurrentHp)
                            selected = current;
                        break;
                }
            }
            return selected;
        }
    }
}
