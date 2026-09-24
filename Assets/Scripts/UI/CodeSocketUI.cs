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

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI socketValueText;
        [SerializeField] private Button socketButton;
        [SerializeField] private Image highlightBorder;
        [SerializeField] private Outline socketOutline;
        [SerializeField] private CanvasGroup socketCanvasGroup;
        [SerializeField] private CodeEditorPanelUI codeEditorUI;

        public CodeTokenSO AssignedToken { get; private set; }
        private bool isHighlightActive = false;

        private void Awake()
        {
            if (codeEditorUI == null) codeEditorUI = GetComponentInParent<CodeEditorPanelUI>();
            if (socketOutline == null) socketOutline = GetComponent<Outline>();
            if (socketCanvasGroup == null) socketCanvasGroup = GetComponent<CanvasGroup>();

            var layout = GetComponent<LayoutElement>();
            if (layout != null)
            {
                // Horizontal inline pill ergonomics matching ~32-36px height and min 160px width
                layout.minHeight = 32f;
                layout.preferredHeight = 36f;
                layout.minWidth = 160f;
                layout.preferredWidth = Mathf.Max(layout.preferredWidth, 220f);
            }

            if (socketValueText != null)
            {
                socketValueText.textWrappingMode = TextWrappingModes.NoWrap;
                socketValueText.overflowMode = TextOverflowModes.Ellipsis;
            }

            if (socketButton != null)
            {
                socketButton.onClick.AddListener(UnslotToken);
            }
        }

        private void Start()
        {
            RefreshDisplay();
            SetHighlight(Color.white, false, 1.0f);
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
                ConsoleLogUI.Log($"[Syntax] Unslotted token from {socketRole}.");
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
                socketValueText.text = $"<color=#4EC9B0><b>{AssignedToken.GetFormattedCodeString()}</b></color>";
                if (!isHighlightActive && socketOutline != null)
                {
                    socketOutline.enabled = true;
                    socketOutline.effectColor = new Color(0.25f, 0.45f, 0.55f, 0.5f);
                    socketOutline.effectDistance = new Vector2(1.5f, -1.5f);
                }
            }
            else
            {
                // Clean hollow placeholder prompt (fixes "phantom default" confusion)
                string placeholder = expectedType switch
                {
                    CodeTokenType.Targeting => "[ Drop Target Rule ]",
                    CodeTokenType.Condition => "[ Drop Condition ]",
                    CodeTokenType.Action => "[ Drop Action ]",
                    CodeTokenType.Float => "[ Drop Float ]",
                    CodeTokenType.Int => "[ Drop Int ]",
                    _ => "[ Drop Token ]"
                };

                socketValueText.text = $"<color=#707070><i>{placeholder}</i></color>";
                if (!isHighlightActive && socketOutline != null)
                {
                    socketOutline.enabled = true;
                    socketOutline.effectColor = new Color(0.4f, 0.4f, 0.4f, 0.35f);
                    socketOutline.effectDistance = new Vector2(1f, -1f);
                }
            }
        }

        public void SetHighlight(Color color, bool active, float alpha = 1.0f)
        {
            isHighlightActive = active;

            if (socketOutline == null) socketOutline = GetComponent<Outline>();
            if (socketOutline != null)
            {
                if (active)
                {
                    socketOutline.enabled = true;
                    socketOutline.effectColor = color;
                    socketOutline.effectDistance = new Vector2(3f, -3f);
                }
                else
                {
                    // Revert to neutral state
                    if (AssignedToken != null)
                    {
                        socketOutline.enabled = true;
                        socketOutline.effectColor = new Color(0.25f, 0.45f, 0.55f, 0.5f);
                        socketOutline.effectDistance = new Vector2(1.5f, -1.5f);
                    }
                    else
                    {
                        socketOutline.enabled = true;
                        socketOutline.effectColor = new Color(0.4f, 0.4f, 0.4f, 0.35f);
                        socketOutline.effectDistance = new Vector2(1f, -1f);
                    }
                }
            }

            if (highlightBorder != null)
            {
                highlightBorder.gameObject.SetActive(active);
                if (active) highlightBorder.color = color;
            }

            if (socketCanvasGroup == null) socketCanvasGroup = GetComponent<CanvasGroup>();
            if (socketCanvasGroup != null)
            {
                socketCanvasGroup.alpha = alpha;
            }
        }

        private void LogTypeMismatchError(CodeTokenSO token, CodeTokenType targetType)
        {
            CodeTokenType actualType = token.tokenType;
            ConsoleLogUI.Log($"[Compile Error] CS0029: Cannot implicitly convert type '{actualType}' to '{targetType}'. Slot [{socketRole}] strictly requires a '{targetType}' token.");
        }
    }
}
