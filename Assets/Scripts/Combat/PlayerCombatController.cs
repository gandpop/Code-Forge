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
        public float DamageMultiplier { get; set; } = 1.0f;
        public int BaseShield { get; set; } = 0;
        public int CritChancePercent { get; set; } = 0;
        public float CritMultiplier { get; set; } = 1.1f;
        public int EvasionChancePercent { get; set; } = 0;
        public int DamageReduction { get; set; } = 0;

        [System.Obsolete] public float CritChance => CritChancePercent / 100f;
        [System.Obsolete] public int CritDamage => Mathf.RoundToInt(CritMultiplier);
        [System.Obsolete] public float EvasionChance => EvasionChancePercent / 100f;
        [System.Obsolete] public PlayerStance CurrentStance { get; private set; } = PlayerStance.Balanced;
        [System.Obsolete] public StanceTokenSO SlottedStanceToken { get; private set; }
        public float EffectiveDamageMultiplier => Mathf.Max(0.1f, DamageMultiplier);

        private Vector3 initialPosition;

        protected override void Awake()
        {
            maxHp = 20f;
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
            SetShield(amount);
            ConsoleLogUI.Log($"[Start] Executed player.AddStartingShield({amount}) -> Current Shield: {CurrentShield}.");
        }

        public void Heal(int amount)
        {
            base.Heal(amount);
            ConsoleLogUI.Log($"<color=#98C379>[Action] Player restored +{amount} HP (Current HP: {Mathf.CeilToInt(CurrentHp)}/{Mathf.CeilToInt(MaxHp)})!</color>");
        }

        public void Overcharge(int shieldAmount, float bonusMultiplier = 0.5f)
        {
            AddShield(shieldAmount);
            DamageMultiplier += bonusMultiplier;
            ConsoleLogUI.Log($"<color=#E5C07B>[Action] Player OVERCHARGED: +{shieldAmount} Shield, +{bonusMultiplier:0.0#}x Next Attack Multiplier (Total: {DamageMultiplier:0.0#}x)!</color>");
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
            CritChancePercent = editorUI.GetCritChancePercent();
            CritMultiplier = editorUI.GetCritMultiplier();
            EvasionChancePercent = editorUI.GetEvasionChancePercent();
            DamageReduction = editorUI.GetDamageReduction();
            DamageMultiplier = dmgMult;

            // Highlight line: player.SetMaxHealth(maxHealth);
            editorUI.HighlightSocketRow(editorUI.MaxHealthSocketUI, true);
            editorUI.HighlightLine(25, true);
            SetMaxHealth(maxHealth);
            yield return new WaitForSeconds(0.2f);
            editorUI.HighlightSocketRow(editorUI.MaxHealthSocketUI, false);
            editorUI.HighlightLine(25, false);

            // Highlight line: player.AddStartingShield(baseShield);
            editorUI.HighlightSocketRow(editorUI.BaseShieldSocketUI, true);
            editorUI.HighlightLine(26, true);
            SetShield(startingShield);
            yield return new WaitForSeconds(0.2f);
            editorUI.HighlightSocketRow(editorUI.BaseShieldSocketUI, false);
            editorUI.HighlightLine(26, false);

            if (EvasionChancePercent > 0)
            {
                ConsoleLogUI.Log($"[Start] Evasion calculated as {EvasionChancePercent}% (Max Cap: 50%).");
            }
            if (CritChancePercent > 0)
            {
                ConsoleLogUI.Log($"[Start] Critical Strike Chance calculated as {CritChancePercent}% ({CritMultiplier:0.0#}x Multiplier).");
            }
            if (DamageReduction > 0)
            {
                ConsoleLogUI.Log($"[Start] Flat Armor (Damage Reduction) calculated as {DamageReduction} DMG.");
            }

            string critStr = CritChancePercent > 0 ? $", Crit={CritChancePercent}% ({CritMultiplier:0.0#}x)" : "";
            string evaStr = EvasionChancePercent > 0 ? $", Evasion={EvasionChancePercent}%" : "";
            string redStr = DamageReduction > 0 ? $", Armor={DamageReduction}" : "";
            ConsoleLogUI.Log($"[Start] Initialized Player: MaxHP={maxHealth}, StartingShield={startingShield}, DamageMult={dmgMult:0.0}x{critStr}{evaStr}{redStr}");
            yield return new WaitForSeconds(0.2f);
        }

        public override void TakeDamage(float amount, bool isPiercing = false)
        {
            if (IsDead) return;

            // Check Evasion
            if (EvasionChancePercent > 0 && UnityEngine.Random.Range(0, 100) < EvasionChancePercent)
            {
                ConsoleLogUI.Log($"<color=#98C379>[Combat] Player EVADED the attack! ({EvasionChancePercent}% Evasion)</color>");
                return;
            }

            // Apply Damage Reduction (Flat Armor)
            float incoming = amount;
            float finalAmount = incoming;
            if (DamageReduction > 0 && !isPiercing)
            {
                float reduced = Mathf.Max(1f, incoming - DamageReduction);
                ConsoleLogUI.Log($"[Combat] Armor mitigated {incoming - reduced:0} DMG ({DamageReduction} flat Armor). Incoming: {reduced:0} DMG.");
                finalAmount = reduced;
            }

            base.TakeDamage(finalAmount, isPiercing);
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
                editorUI.HighlightSocketRow(editorUI.TargetSocketUI, true);
                editorUI.HighlightLine(31, true);
            }
            EnemyEntity target = targetingToken != null
                ? targetingToken.ResolveTarget(context.ActiveEnemies, this)
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
                editorUI.HighlightSocketRow(editorUI.TargetSocketUI, false);
                editorUI.HighlightLine(31, false);
            }

            // Step 2: Evaluate Condition (Line 33)
            if (editorUI != null)
            {
                editorUI.HighlightSocketRow(editorUI.ConditionSocketUI, true);
                editorUI.HighlightLine(33, true);
            }
            CombatContext contextWithTarget = context.WithTarget(target);
            bool evalResult = editorUI != null ? editorUI.EvaluateCondition(contextWithTarget) : (conditionToken != null ? conditionToken.Evaluate(contextWithTarget) : true);
            string condSyntax = editorUI != null ? editorUI.GetConditionSyntax() : (conditionToken != null ? conditionToken.GetFormattedCodeString() : "true");

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
                editorUI.HighlightSocketRow(editorUI.ConditionSocketUI, false);
                editorUI.HighlightLine(33, false);
            }

            // Step 3: Execute Selected Action (Line 35 or Line 39)
            ActionTokenSO chosenAction = evalResult ? thenActionToken : elseActionToken;
            var chosenSocket = evalResult ? editorUI?.ThenActionSocketUI : editorUI?.ElseActionSocketUI;
            int actionLine = evalResult ? 35 : 39;
            string actionSyntax = chosenAction != null ? chosenAction.GetFormattedCodeString() : "Attack(target)";

            if (editorUI != null)
            {
                editorUI.HighlightSocketRow(chosenSocket, true);
                editorUI.HighlightLine(actionLine, true);
            }

            if (chosenAction != null)
            {
                ConsoleLogUI.Log($"[Action] Executing '{chosenAction.GetFormattedCodeString()}'");
                
                // Highlight modular method definition if executing Attack or Defend
                int methodLine = 0;
                if (actionSyntax.Contains("Attack")) methodLine = 14;
                else if (actionSyntax.Contains("Defend")) methodLine = 20;

                if (editorUI != null)
                {
                    if (actionSyntax.Contains("Attack")) editorUI.HighlightSocketRow(editorUI.AttackDamageSocketUI, true);
                    else if (actionSyntax.Contains("Defend")) editorUI.HighlightSocketRow(editorUI.DefendShieldSocketUI, true);
                    if (methodLine > 0) editorUI.HighlightLine(methodLine, true);
                }

                yield return chosenAction.ExecuteAction(contextWithTarget);

                if (editorUI != null)
                {
                    if (actionSyntax.Contains("Attack")) editorUI.HighlightSocketRow(editorUI.AttackDamageSocketUI, false);
                    else if (actionSyntax.Contains("Defend")) editorUI.HighlightSocketRow(editorUI.DefendShieldSocketUI, false);
                    if (methodLine > 0) editorUI.HighlightLine(methodLine, false);
                }
            }
            else
            {
                ConsoleLogUI.Log("[Action] No action slotted in active branch; turn finished.");
                yield return new WaitForSeconds(0.2f);
            }

            if (editorUI != null)
            {
                editorUI.HighlightSocketRow(chosenSocket, false);
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
            var reactionThenAction = editorUI.GetReactionActionToken();
            var reactionElseAction = editorUI.GetReactionElseActionToken();

            if (reactionCond == null && reactionThenAction == null && reactionElseAction == null) yield break;

            var context = new CombatContext(this, activeEnemies, null, 0).WithIncomingDamage(incomingDamage);

            // Step 1: Reaction Condition (Line 45)
            if (editorUI != null)
            {
                editorUI.HighlightSocketRow(editorUI.ReactionConditionSocketUI, true);
                editorUI.HighlightLine(45, true);
            }
            bool evalResult = editorUI != null ? editorUI.EvaluateReactionCondition(context) : (reactionCond != null ? reactionCond.Evaluate(context) : true);
            string condSyntax = editorUI != null ? editorUI.GetReactionConditionSyntax() : (reactionCond != null ? reactionCond.GetFormattedCodeString() : "true");

            if (editorUI.ReactionConditionSocketUI != null)
            {
                Color condColor = evalResult ? new Color(0.2f, 0.9f, 0.3f, 1f) : new Color(0.95f, 0.25f, 0.25f, 1f);
                editorUI.ReactionConditionSocketUI.SetHighlight(condColor, true, 1.0f);
            }
            yield return new WaitForSeconds(0.2f);
            if (editorUI != null)
            {
                editorUI.HighlightSocketRow(editorUI.ReactionConditionSocketUI, false);
                editorUI.HighlightLine(45, false);
            }

            ActionTokenSO chosenAction = evalResult ? reactionThenAction : reactionElseAction;
            var reactionSocket = evalResult ? editorUI?.ReactionActionSocketUI : editorUI?.ReactionElseActionSocketUI;
            int actionLine = evalResult ? 47 : 51;
            string branchName = evalResult ? "THEN" : "ELSE";

            if (evalResult)
            {
                if (editorUI.ReactionActionSocketUI != null) editorUI.ReactionActionSocketUI.SetHighlight(new Color(0.2f, 0.9f, 0.3f, 1f), true, 1.0f);
                if (editorUI.ReactionElseActionSocketUI != null) editorUI.ReactionElseActionSocketUI.SetHighlight(Color.gray, false, 0.3f);
            }
            else
            {
                if (editorUI.ReactionElseActionSocketUI != null) editorUI.ReactionElseActionSocketUI.SetHighlight(new Color(0.95f, 0.25f, 0.25f, 1f), true, 1.0f);
                if (editorUI.ReactionActionSocketUI != null) editorUI.ReactionActionSocketUI.SetHighlight(Color.gray, false, 0.3f);
            }

            if (chosenAction != null)
            {
                string actionStr = chosenAction.GetFormattedCodeString();
                ConsoleLogUI.Log($"[Event] OnTakeDamage({incomingDamage}): '{condSyntax}' is {evalResult.ToString().ToUpper()} -> Executing {branchName} branch '{actionStr}'.");

                if (editorUI != null)
                {
                    editorUI.HighlightSocketRow(reactionSocket, true);
                    editorUI.HighlightLine(actionLine, true);
                }
                yield return new WaitForSeconds(0.2f);

                int methodLine = 0;
                if (actionStr.Contains("Defend")) methodLine = 20;
                else if (actionStr.Contains("Attack")) methodLine = 14;

                if (editorUI != null)
                {
                    if (actionStr.Contains("Attack")) editorUI.HighlightSocketRow(editorUI.AttackDamageSocketUI, true);
                    else if (actionStr.Contains("Defend")) editorUI.HighlightSocketRow(editorUI.DefendShieldSocketUI, true);
                    if (methodLine > 0) editorUI.HighlightLine(methodLine, true);
                }
                yield return chosenAction.ExecuteAction(context);
                if (editorUI != null)
                {
                    if (actionStr.Contains("Attack")) editorUI.HighlightSocketRow(editorUI.AttackDamageSocketUI, false);
                    else if (actionStr.Contains("Defend")) editorUI.HighlightSocketRow(editorUI.DefendShieldSocketUI, false);
                    if (methodLine > 0) editorUI.HighlightLine(methodLine, false);
                }

                if (editorUI != null)
                {
                    editorUI.HighlightSocketRow(reactionSocket, false);
                    editorUI.HighlightLine(actionLine, false);
                }
            }
            else
            {
                ConsoleLogUI.Log($"[Event] OnTakeDamage({incomingDamage}): '{condSyntax}' is {evalResult.ToString().ToUpper()} -> No action slotted in {branchName} branch; skipped.");
                yield return new WaitForSeconds(0.15f);
            }

            if (editorUI != null)
            {
                if (editorUI.ReactionConditionSocketUI != null) editorUI.ReactionConditionSocketUI.SetHighlight(Color.white, false, 1.0f);
                if (editorUI.ReactionActionSocketUI != null) editorUI.ReactionActionSocketUI.SetHighlight(Color.white, false, 1.0f);
                if (editorUI.ReactionElseActionSocketUI != null) editorUI.ReactionElseActionSocketUI.SetHighlight(Color.white, false, 1.0f);
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
