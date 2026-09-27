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
        public int MaxHealth { get; set; } = 20;
        public float DamageMultiplier { get; set; } = 1.2f;
        public int BaseShield { get; set; } = 0;
        [System.Obsolete] public PlayerStance CurrentStance { get; private set; } = PlayerStance.Balanced;
        [System.Obsolete] public StanceTokenSO SlottedStanceToken { get; private set; }
        public float EffectiveDamageMultiplier => Mathf.Max(0.1f, DamageMultiplier);

        private Vector3 initialPosition;

        protected override void Awake()
        {
            base.Awake();
            initialPosition = transform.position;
        }

        public void SetMaxHealth(int amount)
        {
            if (amount <= 0) amount = 20;
            MaxHealth = amount;
            SetMaxHp(amount);
        }

        public void AddStartingShield(int amount)
        {
            if (amount <= 0) return;
            AddShield(amount);
            ConsoleLogUI.Log($"[Start] Executed player.AddStartingShield({amount}) -> Current Shield: {CurrentShield}.");
        }

        [System.Obsolete]
        public void SetStance(StanceTokenSO stanceToken)
        {
            SlottedStanceToken = stanceToken;
            CurrentStance = stanceToken != null ? stanceToken.stance : PlayerStance.Balanced;
        }

        public IEnumerator PerformAttackAnimation()
        {
            yield return PerformAttackLunge(new Vector3(0.5f, 0.3f, 0f));
        }

        /// <summary>
        /// Executes the void Start() block once when encounter initializes before Round 1.
        /// </summary>
        public IEnumerator ExecuteStartBlock(CombatContext context, CodeEditorPanelUI editorUI)
        {
            if (editorUI == null) yield break;

            int maxHealth = editorUI.GetMaxHealth();
            int startingShield = editorUI.GetBaseShield();
            float dmgMult = editorUI.GetDamageMultiplier();

            // Line 25: player.SetMaxHealth(maxHealth);
            editorUI.HighlightLine(25, true);
            SetMaxHealth(maxHealth);
            yield return new WaitForSeconds(0.2f);
            editorUI.HighlightLine(25, false);

            // Line 26: player.AddStartingShield(baseShield);
            editorUI.HighlightLine(26, true);
            AddStartingShield(startingShield);
            yield return new WaitForSeconds(0.2f);
            editorUI.HighlightLine(26, false);

            ConsoleLogUI.Log($"[Start] Initialized Player: MaxHP={maxHealth}, StartingShield={startingShield}, DamageMult={dmgMult:0.0}x");
            yield return new WaitForSeconds(0.2f);
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

            if (editorUI != null)
            {
                DamageMultiplier = editorUI.GetDamageMultiplier();
            }

            // Step 1: Resolve Target (Line 31)
            if (editorUI != null)
            {
                editorUI.HighlightLine(31, true);
            }
            EnemyEntity target = targetingToken != null
                ? targetingToken.ResolveTarget(context.ActiveEnemies)
                : SelectFallbackTarget(context.ActiveEnemies);

            string targetSyntax = targetingToken != null ? targetingToken.GetFormattedCodeString() : "Enemies.Random()";
            ConsoleLogUI.Log($"[Target] Selected {target?.name ?? "None"} via '{targetSyntax}'");

            if (editorUI != null && editorUI.TargetSocketUI != null)
            {
                editorUI.TargetSocketUI.SetHighlight(new Color(0.25f, 0.75f, 1f, 1f), true, 1.0f);
            }
            yield return new WaitForSeconds(0.2f);
            if (editorUI != null)
            {
                editorUI.HighlightLine(31, false);
            }

            // Step 2: Evaluate Condition (Line 33)
            if (editorUI != null)
            {
                editorUI.HighlightLine(33, true);
            }
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

                // Branch Visual Feedback
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
            yield return new WaitForSeconds(0.2f);
            if (editorUI != null)
            {
                editorUI.HighlightLine(33, false);
            }

            // Step 3: Execute Selected Action (Line 35 or Line 39)
            ActionTokenSO chosenAction = evalResult ? thenActionToken : elseActionToken;
            int actionLine = evalResult ? 35 : 39;
            string actionSyntax = chosenAction != null ? chosenAction.GetFormattedCodeString() : "Attack(target)";

            if (editorUI != null)
            {
                editorUI.HighlightLine(actionLine, true);
            }

            if (chosenAction != null)
            {
                ConsoleLogUI.Log($"[Action] Executing '{chosenAction.GetFormattedCodeString()}'");
                
                // Highlight modular method definition if executing Attack or Defend
                int methodLine = 0;
                if (actionSyntax.Contains("Attack")) methodLine = 14;
                else if (actionSyntax.Contains("Defend")) methodLine = 20;

                if (methodLine > 0 && editorUI != null) editorUI.HighlightLine(methodLine, true);

                yield return chosenAction.ExecuteAction(contextWithTarget);

                if (methodLine > 0 && editorUI != null) editorUI.HighlightLine(methodLine, false);
            }
            else
            {
                ConsoleLogUI.Log("[Action] No action slotted in active branch; turn finished.");
                yield return new WaitForSeconds(0.2f);
            }

            if (editorUI != null)
            {
                editorUI.HighlightLine(actionLine, false);
            }
            yield return new WaitForSeconds(0.2f);
            if (editorUI != null)
            {
                editorUI.ResetAllHighlights();
            }
        }

        public IEnumerator HandleIncomingDamageReaction(int incomingDamage, CodeEditorPanelUI editorUI = null, List<EnemyEntity> activeEnemies = null)
        {
            if (IsDead) yield break;

            if (editorUI == null)
            {
                editorUI = FindAnyObjectByType<CodeEditorPanelUI>();
            }

            if (editorUI == null) yield break;

            var reactionCond = editorUI.GetReactionConditionToken();
            var reactionAction = editorUI.GetReactionActionToken();

            if (reactionCond == null && reactionAction == null) yield break;

            var context = new CombatContext(this, activeEnemies, null, 0).WithIncomingDamage(incomingDamage);

            // Step 1: Reaction Condition (Line 45)
            if (editorUI != null)
            {
                editorUI.HighlightLine(45, true);
            }
            bool evalResult = reactionCond != null ? reactionCond.Evaluate(context) : true;
            string condSyntax = reactionCond != null ? reactionCond.GetFormattedCodeString() : "true";

            if (editorUI.ReactionConditionSocketUI != null)
            {
                Color condColor = evalResult ? new Color(0.2f, 0.9f, 0.3f, 1f) : new Color(0.95f, 0.25f, 0.25f, 1f);
                editorUI.ReactionConditionSocketUI.SetHighlight(condColor, true, 1.0f);
            }
            yield return new WaitForSeconds(0.2f);
            if (editorUI != null)
            {
                editorUI.HighlightLine(45, false);
            }

            if (evalResult)
            {
                string actionStr = reactionAction != null ? reactionAction.GetFormattedCodeString() : "Defend()";
                ConsoleLogUI.Log($"[Event] OnTakeDamage({incomingDamage}) triggered! Condition '{condSyntax}' is TRUE -> Executed {actionStr}.");

                // Step 2: Reaction Action (Line 47)
                if (editorUI != null)
                {
                    editorUI.HighlightLine(47, true);
                }

                if (editorUI.ReactionActionSocketUI != null)
                {
                    editorUI.ReactionActionSocketUI.SetHighlight(new Color(0.2f, 0.9f, 0.3f, 1f), true, 1.0f);
                }
                yield return new WaitForSeconds(0.2f);

                if (reactionAction != null)
                {
                    if (actionStr.Contains("Defend") && editorUI != null) editorUI.HighlightLine(20, true);
                    yield return reactionAction.ExecuteAction(context);
                    if (actionStr.Contains("Defend") && editorUI != null) editorUI.HighlightLine(20, false);
                }
                else
                {
                    ConsoleLogUI.Log($"[Event] OnTakeDamage({incomingDamage}): Condition '{condSyntax}' is TRUE, but no reaction action is slotted.");
                    yield return new WaitForSeconds(0.1f);
                }

                if (editorUI != null)
                {
                    editorUI.HighlightLine(47, false);
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

        public void OnTakeDamage(int incomingDamage)
        {
            var editorUI = FindAnyObjectByType<CodeEditorPanelUI>();
            if (editorUI != null)
            {
                StartCoroutine(HandleIncomingDamageReaction(incomingDamage, editorUI));
            }
        }

        public EnemyEntity SelectFallbackTarget(List<EnemyEntity> enemies)
        {
            if (enemies == null || enemies.Count == 0) return null;

            List<EnemyEntity> living = new List<EnemyEntity>();
            for (int i = 0; i < enemies.Count; i++)
            {
                var current = enemies[i];
                if (current != null && !current.IsDead)
                {
                    living.Add(current);
                }
            }
            if (living.Count == 0) return null;
            return living[Random.Range(0, living.Count)];
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
