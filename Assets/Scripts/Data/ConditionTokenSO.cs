using UnityEngine;
using CodeForge.Combat;

namespace CodeForge.Data
{
    [CreateAssetMenu(fileName = "NewConditionToken", menuName = "CodeForge/Tokens/Condition Token")]
    public class ConditionTokenSO : CodeTokenSO, IConditionToken
    {
        [Header("Condition Parameters")]
        public ConditionSubject subject;
        public ConditionOperator op;
        public float comparisonValue;

        private void Reset()
        {
            tokenType = CodeTokenType.Condition;
        }

        private void OnValidate()
        {
            tokenType = CodeTokenType.Condition;
        }

        public bool Evaluate(CombatContext context)
        {
            if (context == null) return false;

            return subject switch
            {
                ConditionSubject.TargetHealthPercent =>
                    CompareFloats(context.CurrentTarget != null ? context.CurrentTarget.HealthPercent : 0f, op, comparisonValue),

                ConditionSubject.TargetIsShielded =>
                    CompareBools(context.CurrentTarget != null && context.CurrentTarget.IsShielded, op, comparisonValue > 0.5f),

                ConditionSubject.TargetIntendingAttack =>
                    CompareBools(context.CurrentTarget != null && (context.CurrentTarget.CurrentIntent.intentType == EnemyIntentType.Attack || context.CurrentTarget.CurrentIntent.intentType == EnemyIntentType.HeavyHit), op, comparisonValue > 0.5f),

                ConditionSubject.PlayerHealthPercent =>
                    CompareFloats(context.Player != null ? context.Player.HealthPercent : 0f, op, comparisonValue),

                ConditionSubject.TurnCountIsEven =>
                    CompareBools(context.TurnCount % 2 == 0, op, comparisonValue > 0.5f),

                ConditionSubject.IncomingDamage =>
                    CompareFloats(context.IncomingDamage, op, comparisonValue),

                _ => false
            };
        }

        private bool CompareFloats(float actual, ConditionOperator oper, float threshold)
        {
            return oper switch
            {
                ConditionOperator.LessThan => actual < threshold,
                ConditionOperator.GreaterThanOrEqual => actual >= threshold,
                ConditionOperator.Equals => Mathf.Approximately(actual, threshold),
                ConditionOperator.NotEquals => !Mathf.Approximately(actual, threshold),
                _ => false
            };
        }

        private bool CompareBools(bool actual, ConditionOperator oper, bool expected)
        {
            return oper switch
            {
                ConditionOperator.Equals => actual == expected,
                ConditionOperator.NotEquals => actual != expected,
                _ => actual == expected
            };
        }
    }
}
