using UnityEngine;

namespace CodeForge.Data
{
    [CreateAssetMenu(fileName = "NewCodeToken", menuName = "CodeForge/Code Token")]
    public class CodeTokenSO : ScriptableObject
    {
        public string tokenName;
        public CodeTokenType tokenType;

        [Header("Values (Populate based on TokenType)")]
        public float floatValue;
        public int intValue;
        public bool boolValue;
        public TargetPriority targetPriorityValue;

        public string GetFormattedCodeString()
        {
            return tokenType switch
            {
                CodeTokenType.Float => $"{floatValue:0.0}f",
                CodeTokenType.Int => $"{intValue}",
                CodeTokenType.Bool => boolValue ? "true" : "false",
                CodeTokenType.TargetPriority => $"TargetPriority.{targetPriorityValue}",
                _ => "null"
            };
        }
    }
}
