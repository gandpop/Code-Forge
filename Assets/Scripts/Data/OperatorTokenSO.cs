using UnityEngine;

namespace CodeForge.Data
{
    public enum LogicOperatorType
    {
        And, // &&
        Or,  // ||
        Not  // !
    }

    [CreateAssetMenu(fileName = "Operator_New", menuName = "CodeForge/Tokens/Operator Token")]
    public class OperatorTokenSO : CodeTokenSO
    {
        public LogicOperatorType operatorType;

        private void Reset()
        {
            tokenType = CodeTokenType.Operator;
        }

        private void OnEnable()
        {
            tokenType = CodeTokenType.Operator;
        }

        private void OnValidate()
        {
            tokenType = CodeTokenType.Operator;
        }

        public override string GetFormattedCodeString()
        {
            return operatorType switch
            {
                LogicOperatorType.And => "&&",
                LogicOperatorType.Or  => "||",
                LogicOperatorType.Not => "!",
                _ => "&&"
            };
        }

        public bool Evaluate(bool left, bool right)
        {
            return operatorType switch
            {
                LogicOperatorType.And => left && right,
                LogicOperatorType.Or  => left || right,
                LogicOperatorType.Not => !left,
                _ => left && right
            };
        }
    }
}
