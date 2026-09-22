using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CodeForge.Data;

namespace CodeForge.UI
{
    public class CodeSocketUI : MonoBehaviour
    {
        public CodeSocketRole socketRole;
        public CodeTokenType expectedType;

        [Header("Default Values (Used when no token slotted)")]
        public float defaultFloat = 1.0f;
        public int defaultInt = 10;
        public bool defaultBool = false;
        public TargetPriority defaultTargetPriority = TargetPriority.LowestHealth;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI socketValueText;
        [SerializeField] private Button socketButton;

        public CodeTokenSO AssignedToken { get; private set; }

        private void Awake()
        {
            if (socketButton != null)
            {
                socketButton.onClick.AddListener(() =>
                {
                    AssignedToken = null;
                    RefreshDisplay();
                });
            }
        }

        private void Start()
        {
            RefreshDisplay();
        }

        public void AssignToken(CodeTokenSO token)
        {
            if (token != null && token.tokenType != expectedType)
            {
                ConsoleLogUI.Log($"[Compile Error] Cannot assign token of type {token.tokenType} to socket expecting {expectedType}.");
                return;
            }

            AssignedToken = token;
            RefreshDisplay();
        }

        public void RefreshDisplay()
        {
            if (socketValueText == null) return;

            if (AssignedToken != null)
            {
                socketValueText.text = $"<color=#4EC9B0>{AssignedToken.GetFormattedCodeString()}</color>";
            }
            else
            {
                string defaultValStr = expectedType switch
                {
                    CodeTokenType.Float => $"{defaultFloat:0.0}f",
                    CodeTokenType.Int => $"{defaultInt}",
                    CodeTokenType.Bool => defaultBool ? "true" : "false",
                    CodeTokenType.TargetPriority => $"TargetPriority.{defaultTargetPriority}",
                    _ => "null"
                };

                socketValueText.text = $"<color=#9E9E9E>{defaultValStr}</color> <size=70%><color=#6E6E6E>(default)</color></size>";
            }
        }
    }
}
