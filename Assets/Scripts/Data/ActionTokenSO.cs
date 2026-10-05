using System.Collections;
using UnityEngine;
using CodeForge.Combat;
using CodeForge.UI;

namespace CodeForge.Data
{
    [CreateAssetMenu(fileName = "NewActionToken", menuName = "CodeForge/Tokens/Action Token")]
    public class ActionTokenSO : CodeTokenSO, ICombatActionToken
    {
        [Header("Action Parameters")]
        public ActionCategory category = ActionCategory.Attack;
        public float baseValue = 10f;
        public bool isAreaOfEffect = false;
        public bool isPiercing = false;

        private void Reset()
        {
            tokenType = CodeTokenType.Action;
        }

        private void OnValidate()
        {
            tokenType = CodeTokenType.Action;
        }

        public IEnumerator ExecuteAction(CombatContext context)
        {
            if (context == null || context.Player == null) yield break;

            var editorUI = Object.FindFirstObjectByType<CodeEditorPanelUI>();
            float actionValue = baseValue;
            if (codeDisplaySyntax != null && codeDisplaySyntax.Contains("Attack(target)") && editorUI != null)
            {
                actionValue = editorUI.GetAttackDamage();
            }
            else if (codeDisplaySyntax != null && codeDisplaySyntax.Contains("Defend()") && editorUI != null)
            {
                actionValue = editorUI.GetDefendShield();
            }

            if (codeDisplaySyntax != null && codeDisplaySyntax.Contains("Heal"))
            {
                int healAmt = Mathf.RoundToInt(actionValue > 0f ? actionValue : 6f);
                context.Player.Heal(healAmt);
                yield return new WaitForSeconds(0.25f);
                yield break;
            }

            if (codeDisplaySyntax != null && codeDisplaySyntax.Contains("Overcharge"))
            {
                int shieldAmt = Mathf.RoundToInt(actionValue > 0f ? actionValue : 10f);
                context.Player.Overcharge(shieldAmt, 0.5f);
                yield return new WaitForSeconds(0.25f);
                yield break;
            }

            switch (category)
            {
                case ActionCategory.Attack:
                    yield return context.Player.PerformAttackAnimation();

                    bool isCrit = context.Player != null && context.Player.CritChancePercent > 0 && (Random.Range(0, 100) < context.Player.CritChancePercent);
                    float critMult = isCrit ? context.Player.CritMultiplier : 1.0f;
                    float dmgMult = context.Player != null ? context.Player.DamageMultiplier : 1.0f;
                    float baseScaled = actionValue * dmgMult;
                    float finalDamage = baseScaled * critMult;

                    string critSuffix = isCrit ? $" ({critMult:0.0#}x CRIT!)" : "";
                    string multText = dmgMult != 1.0f ? $" (Base {actionValue}{critSuffix} * {dmgMult:0.0#}x = {finalDamage:0.#} DMG)" : (isCrit ? $" for {finalDamage:0.#} DMG{critSuffix}" : $" for {finalDamage:0.#} DMG");

                    if (isCrit)
                    {
                        ConsoleLogUI.Log($"<color=#E5C07B>[Combat] CRITICAL STRIKE! ({actionValue} * {dmgMult:0.0#}x) * {critMult:0.0#}x = {Mathf.RoundToInt(finalDamage)} DMG!</color>");
                    }

                    if (isAreaOfEffect)
                    {
                        ConsoleLogUI.Log($"[Action] Executing AoE Attack '{GetFormattedCodeString()}'{multText}{(isPiercing ? " (Piercing)" : "")}!");
                        for (int i = context.ActiveEnemies.Count - 1; i >= 0; i--)
                        {
                            var enemy = context.ActiveEnemies[i];
                            if (enemy != null && !enemy.IsDead)
                            {
                                enemy.TakeDamage(finalDamage, isPiercing);
                                if (editorUI != null && editorUI.GetApplyBleed())
                                {
                                    enemy.ApplyBleed(editorUI.GetBleedDamage(), editorUI.GetBleedDuration());
                                }
                            }
                        }
                    }
                    else
                    {
                        var target = context.CurrentTarget;
                        if (target == null || target.IsDead || target.CurrentHp <= 0 || !target.gameObject.activeInHierarchy)
                        {
                            target = context.Player != null ? context.Player.SelectFallbackTarget(context.ActiveEnemies) : null;
                        }

                        if (target != null && !target.IsDead && target.CurrentHp > 0 && target.gameObject.activeInHierarchy)
                        {
                            ConsoleLogUI.Log($"[Action] Executing '{GetFormattedCodeString()}' on {target.name}{multText}{(isPiercing ? " (Piercing)" : "")}!");
                            target.TakeDamage(finalDamage, isPiercing);

                            if (editorUI != null && editorUI.GetApplyBleed())
                            {
                                target.ApplyBleed(editorUI.GetBleedDamage(), editorUI.GetBleedDuration());
                            }
                        }
                        else
                        {
                            ConsoleLogUI.Log($"[Action] Target was null or already defeated! Strike missed.");
                        }
                    }
                    break;

                case ActionCategory.Defense:
                    int shieldToAdd = Mathf.RoundToInt(actionValue);
                    context.Player.AddShield(shieldToAdd);
                    ConsoleLogUI.Log($"[Action] Executing '{GetFormattedCodeString()}': Player gained +{shieldToAdd} Shield!");
                    yield return new WaitForSeconds(0.2f);
                    break;

                case ActionCategory.Utility:
                    ConsoleLogUI.Log($"[Action] Executing Utility '{GetFormattedCodeString()}'.");
                    yield return new WaitForSeconds(0.2f);
                    break;
            }

            yield return new WaitForSeconds(0.25f);
        }
    }
}
