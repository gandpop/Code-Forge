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

            switch (category)
            {
                case ActionCategory.Attack:
                    yield return context.Player.PerformAttackAnimation();

                    float mult = context.Player != null ? context.Player.DamageMultiplier : 1.0f;
                    float effectiveDamage = actionValue * mult;
                    string multText = mult != 1.0f ? $" (Base {actionValue} * {mult}x = {effectiveDamage} DMG)" : $" for {effectiveDamage} DMG";

                    if (isAreaOfEffect)
                    {
                        ConsoleLogUI.Log($"[Action] Executing AoE Attack '{GetFormattedCodeString()}'{multText}{(isPiercing ? " (Piercing)" : "")}!");
                        for (int i = context.ActiveEnemies.Count - 1; i >= 0; i--)
                        {
                            var enemy = context.ActiveEnemies[i];
                            if (enemy != null && !enemy.IsDead)
                            {
                                enemy.TakeDamage(effectiveDamage, isPiercing);
                            }
                        }
                    }
                    else
                    {
                        var target = context.CurrentTarget;
                        if (target != null && !target.IsDead)
                        {
                            ConsoleLogUI.Log($"[Action] Executing '{GetFormattedCodeString()}' on {target.name}{multText}{(isPiercing ? " (Piercing)" : "")}!");
                            target.TakeDamage(effectiveDamage, isPiercing);
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
