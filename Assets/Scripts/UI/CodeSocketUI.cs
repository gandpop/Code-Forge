using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using CodeForge.Data;

namespace CodeForge.UI
{
    public class CodeSocketUI : MonoBehaviour, IDropHandler
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
        [SerializeField] private CodeEditorPanelUI codeEditorUI;

        public CodeTokenSO AssignedToken { get; private set; }

        private void Awake()
        {
            if (codeEditorUI == null) codeEditorUI = GetComponentInParent<CodeEditorPanelUI>();

            if (socketButton != null)
            {
                socketButton.onClick.AddListener(UnslotToken);
            }
        }

        private void Start()
        {
            RefreshDisplay();
        }

        public void SetEditorReference(CodeEditorPanelUI editorUI)
        {
            codeEditorUI = editorUI;
        }

        public void OnDrop(PointerEventData eventData)
        {
            var draggedCard = eventData.pointerDrag?.GetComponent<DraggableTokenCardUI>();
            if (draggedCard == null || draggedCard.Token == null) return;

            if (draggedCard.Token.tokenType != expectedType)
            {
                LogTypeMismatchError(draggedCard.Token, expectedType);
                return;
            }

            // Valid drop! If we already have a token equipped, return it to the inventory shelf
            if (AssignedToken != null && codeEditorUI != null)
            {
                codeEditorUI.AddTokenToInventory(AssignedToken);
            }

            AssignToken(draggedCard.Token);
            draggedCard.NotifyDropAccepted();
            ConsoleLogUI.Log($"[Syntax] Assigned {draggedCard.Token.GetFormattedCodeString()} to {socketRole}.");
        }

        public void UnslotToken()
        {
            if (AssignedToken != null)
            {
                var oldToken = AssignedToken;
                AssignedToken = null;
                RefreshDisplay();

                if (codeEditorUI != null)
                {
                    codeEditorUI.AddTokenToInventory(oldToken);
                }
                ConsoleLogUI.Log($"[Syntax] Unslotted token from {socketRole}. Reverted to default value.");
            }
        }

        public void AssignToken(CodeTokenSO token)
        {
            if (token != null && token.tokenType != expectedType)
            {
                LogTypeMismatchError(token, expectedType);
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

        private void LogTypeMismatchError(CodeTokenSO token, CodeTokenType targetType)
        {
            CodeTokenType actualType = token.tokenType;

            if (actualType == CodeTokenType.Int && targetType == CodeTokenType.Float)
            {
                ConsoleLogUI.Log($"[Compile Error] CS0029: Cannot implicitly convert type 'int' ({token.intValue}) to 'float'. In C#, integers only store whole numbers, while floats represent fractional/decimal precision. Consider using a float literal (e.g. {token.intValue}.0f).");
            }
            else if (actualType == CodeTokenType.Float && targetType == CodeTokenType.Int)
            {
                ConsoleLogUI.Log($"[Compile Error] CS0266: Cannot implicitly convert type 'float' ({token.floatValue:0.0}f) to 'int'. An explicit conversion exists (are you missing a cast?). Floats hold fractional numbers; casting to integer causes loss of decimal precision.");
            }
            else if (actualType == CodeTokenType.Bool)
            {
                ConsoleLogUI.Log($"[Compile Error] CS0029: Cannot implicitly convert type 'bool' to '{targetType.ToString().ToLower()}'. Booleans only represent truth states (true/false) and cannot be assigned to numerical or enum variables.");
            }
            else
            {
                ConsoleLogUI.Log($"[Compile Error] CS0029: Cannot implicitly convert type '{actualType}' to '{targetType}'. C# is strongly-typed; variables must be assigned matching types.");
            }
        }
    }
}
