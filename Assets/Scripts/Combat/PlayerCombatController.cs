using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeForge.Data;
using CodeForge.UI;

namespace CodeForge.Combat
{
    public class PlayerCombatController : CombatEntity
    {
        [Header("Script Bound Stats")]
        public float DamageMultiplier { get; set; } = 1.0f;
        public int BaseShield { get; set; } = 0;

        private Vector3 initialPosition;

        protected override void Awake()
        {
            base.Awake();
            initialPosition = transform.position;
        }

        public IEnumerator PerformAttackAnimation()
        {
            yield return PerformAttackLunge(new Vector3(0.5f, 0.3f, 0f));
        }

        public IEnumerator ExecuteTurnPipeline(
            CombatContext context,
            CodeEditorPanelUI editorUI,
            TargetingTokenSO targetingToken,
            ConditionTokenSO conditionToken,
            ActionTokenSO thenActionToken,
            ActionTokenSO elseActionToken)
        {
            if (IsDead || context == null || context.ActiveEnemies == null || context.ActiveEnemies.Count == 0) yield break;

            // Update stats from editor before turn execution
            if (editorUI != null)
            {
                DamageMultiplier = editorUI.GetDamageMultiplier();
            }

            // Step 1: Resolve Target
            EnemyEntity target = targetingToken != null
                ? targetingToken.ResolveTarget(context.ActiveEnemies)
                : SelectFallbackTarget(context.ActiveEnemies);

            string targetSyntax = targetingToken != null ? targetingToken.GetFormattedCodeString() : "Enemies.LowestHP()";
            ConsoleLogUI.Log($"[Target] Selected {target?.name ?? "None"} via '{targetSyntax}'");

            if (editorUI != null && editorUI.TargetSocketUI != null)
            {
                editorUI.TargetSocketUI.SetHighlight(new Color(0.25f, 0.75f, 1f, 1f), true, 1.0f);
            }
            yield return new WaitForSeconds(0.35f);

            // Step 2: Evaluate Condition
            CombatContext contextWithTarget = context.WithTarget(target);
            bool evalResult = conditionToken != null ? conditionToken.Evaluate(contextWithTarget) : true;
            string condSyntax = conditionToken != null ? conditionToken.GetFormattedCodeString() : "true";

            string targetStatus = target != null
                ? $" (HP: {Mathf.RoundToInt(target.HealthPercent * 100f)}% - {Mathf.CeilToInt(target.CurrentHp)}/{Mathf.CeilToInt(target.MaxHp)}{(target.IsShielded ? $", Shield: {target.CurrentShield}" : "")})"
                : "";

            ConsoleLogUI.Log($"[Eval] '{condSyntax}' evaluated to {evalResult.ToString().ToUpper()}{targetStatus}");

            if (editorUI != null)
            {
                Color condColor = evalResult ? new Color(0.2f, 0.9f, 0.3f, 1f) : new Color(0.95f, 0.25f, 0.25f, 1f);
                if (editorUI.ConditionSocketUI != null) editorUI.ConditionSocketUI.SetHighlight(condColor, true, 1.0f);

                // Step 3: Branch Visual Feedback (Glow active branch, Dim rejected branch)
                if (evalResult)
                {
                    if (editorUI.ThenActionSocketUI != null) editorUI.ThenActionSocketUI.SetHighlight(new Color(0.2f, 0.9f, 0.3f, 1f), true, 1.0f);
                    if (editorUI.ElseActionSocketUI != null) editorUI.ElseActionSocketUI.SetHighlight(Color.gray, false, 0.3f);
                }
                else
                {
                    if (editorUI.ElseActionSocketUI != null) editorUI.ElseActionSocketUI.SetHighlight(new Color(0.95f, 0.25f, 0.25f, 1f), true, 1.0f);
                    if (editorUI.ThenActionSocketUI != null) editorUI.ThenActionSocketUI.SetHighlight(Color.gray, false, 0.3f);
                }
            }
            yield return new WaitForSeconds(0.4f);

            // Step 4: Execute Selected Action
            ActionTokenSO chosenAction = evalResult ? thenActionToken : elseActionToken;
            if (chosenAction != null)
            {
                ConsoleLogUI.Log($"[Action] Executing '{chosenAction.GetFormattedCodeString()}'");
                yield return chosenAction.ExecuteAction(contextWithTarget);
            }
            else
            {
                float defaultDmg = 5f * DamageMultiplier;
                string multSuffix = DamageMultiplier != 1.0f ? $" (5 * {DamageMultiplier}x = {defaultDmg} DMG)" : $" for {defaultDmg} DMG";
                ConsoleLogUI.Log($"[Action] No action slotted in active branch; executing default strike on {target?.name ?? "enemy"}{multSuffix}.");
                if (target != null && !target.IsDead)
                {
                    yield return PerformAttackAnimation();
                    target.TakeDamage(defaultDmg);
                }
                yield return new WaitForSeconds(0.2f);
            }

            yield return new WaitForSeconds(0.25f);
            if (editorUI != null)
            {
                editorUI.ResetAllHighlights();
            }
        }

        /// <summary>
        /// Handles the event-driven defensive reaction when incoming damage is detected during enemy turns.
        /// </summary>
        public IEnumerator HandleIncomingDamageReaction(int incomingDamage, CodeEditorPanelUI editorUI = null, List<EnemyEntity> activeEnemies = null)
        {
            if (IsDead) yield break;

            if (editorUI == null)
            {
                editorUI = FindAnyObjectByType<CodeEditorPanelUI>();
            }

            if (editorUI == null || !editorUI.IsSection3Unlocked) yield break;

            var reactionCond = editorUI.GetReactionConditionToken();
            var reactionAction = editorUI.GetReactionActionToken();

            if (reactionCond == null && reactionAction == null) yield break;

            var context = new CombatContext(this, activeEnemies, null, 0).WithIncomingDamage(incomingDamage);

            bool evalResult = reactionCond != null ? reactionCond.Evaluate(context) : true;
            string condSyntax = reactionCond != null ? reactionCond.GetFormattedCodeString() : "true";

            if (editorUI.ReactionConditionSocketUI != null)
            {
                Color condColor = evalResult ? new Color(0.2f, 0.9f, 0.3f, 1f) : new Color(0.95f, 0.25f, 0.25f, 1f);
                editorUI.ReactionConditionSocketUI.SetHighlight(condColor, true, 1.0f);
            }

            if (evalResult)
            {
                string actionSyntax = reactionAction != null ? reactionAction.GetFormattedCodeString() : "player.AddShield(10)";
                ConsoleLogUI.Log($"[Event] OnTakeDamage({incomingDamage}) triggered! Condition '{condSyntax}' is TRUE -> Executed {actionSyntax}.");

                if (editorUI.ReactionActionSocketUI != null)
                {
                    editorUI.ReactionActionSocketUI.SetHighlight(new Color(0.2f, 0.9f, 0.3f, 1f), true, 1.0f);
                }
                yield return new WaitForSeconds(0.35f);

                if (reactionAction != null)
                {
                    yield return reactionAction.ExecuteAction(context);
                }
                else
                {
                    // Default reaction action if slot empty
                    AddShield(10);
                    ConsoleLogUI.Log($"[Event] Executed default defensive reaction: +10 Shield absorbed incoming hit!");
                    yield return new WaitForSeconds(0.2f);
                }
            }
            else
            {
                ConsoleLogUI.Log($"[Event] OnTakeDamage({incomingDamage}): Condition '{condSyntax}' is FALSE -> Reaction skipped.");
                if (editorUI.ReactionActionSocketUI != null)
                {
                    editorUI.ReactionActionSocketUI.SetHighlight(Color.gray, false, 0.3f);
                }
                yield return new WaitForSeconds(0.25f);
            }

            if (editorUI != null)
            {
                if (editorUI.ReactionConditionSocketUI != null) editorUI.ReactionConditionSocketUI.SetHighlight(Color.white, false, 1.0f);
                if (editorUI.ReactionActionSocketUI != null) editorUI.ReactionActionSocketUI.SetHighlight(Color.white, false, 1.0f);
            }
        }

        /// <summary>
        /// Educational script method signature matching the PlayerCombat.cs editor scaffold.
        /// </summary>
        public void OnTakeDamage(int incomingDamage)
        {
            var editorUI = FindAnyObjectByType<CodeEditorPanelUI>();
            if (editorUI != null && editorUI.IsSection3Unlocked)
            {
                StartCoroutine(HandleIncomingDamageReaction(incomingDamage, editorUI));
            }
        }

        public EnemyEntity SelectFallbackTarget(List<EnemyEntity> enemies)
        {
            if (enemies == null || enemies.Count == 0) return null;

            EnemyEntity selected = null;
            for (int i = 0; i < enemies.Count; i++)
            {
                var current = enemies[i];
                if (current == null || current.IsDead) continue;
                if (selected == null || current.CurrentHp < selected.CurrentHp)
                {
                    selected = current;
                }
            }
            return selected;
        }

        private IEnumerator PerformAttackLunge(Vector3 offset, float duration = 0.12f)
        {
            Vector3 startPos = transform.position;
            Vector3 targetPos = startPos + offset;

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
