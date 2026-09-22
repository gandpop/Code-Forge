using System.Collections;
using UnityEngine;
using CodeForge.UI;

namespace CodeForge.Combat
{
    public class EnemyEntity : CombatEntity
    {
        [Header("Enemy Attributes")]
        public float contactDamage = 10f;
        private CombatEntity playerTarget;

        public void Initialize(CombatEntity target)
        {
            playerTarget = target;
            gameObject.SetActive(true);
        }

        public IEnumerator ExecuteEnemyTurn(CombatEntity target)
        {
            if (IsDead || target == null || target.IsDead) yield break;

            // Visual lunge towards player (down-left)
            yield return StartCoroutine(PerformAttackLunge(new Vector3(-0.4f, -0.2f, 0f)));

            if (target != null && !target.IsDead)
            {
                target.TakeDamage(contactDamage);
                ConsoleLogUI.Log($"[Turn] {gameObject.name} attacks Player for {contactDamage} DMG!");
            }

            yield return new WaitForSeconds(0.2f);
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
    }
}
