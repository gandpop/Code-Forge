using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using CodeForge.Data;

namespace CodeForge.UI
{
    public class CodeSocketUI : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
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
        public CodeEditorPanelUI EditorUI => codeEditorUI != null ? codeEditorUI : (codeEditorUI = GetComponentInParent<CodeEditorPanelUI>() ?? Object.FindFirstObjectByType<CodeEditorPanelUI>());
        private bool isHighlightActive = false;

        private void Awake()
        {
            if (codeEditorUI == null) codeEditorUI = GetComponentInParent<CodeEditorPanelUI>() ?? Object.FindFirstObjectByType<CodeEditorPanelUI>();
            if (socketOutline == null) socketOutline = GetComponent<Outline>();
            if (socketCanvasGroup == null) socketCanvasGroup = GetComponent<CanvasGroup>();

            var layout = GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.minHeight = 20f;
                layout.preferredHeight = 20f;
                layout.preferredWidth = expectedType switch
                {
                    CodeTokenType.Int => 60f,
                    CodeTokenType.Float => 80f,
                    CodeTokenType.Stance => 160f,
                    CodeTokenType.Targeting => 180f,
                    CodeTokenType.Condition => 220f,
                    CodeTokenType.Action => 190f,
                    _ => 150f
                };
                layout.minWidth = layout.preferredWidth;
            }

            if (socketValueText != null)
            {
                socketValueText.textWrappingMode = TextWrappingModes.NoWrap;
                socketValueText.overflowMode = TextOverflowModes.Ellipsis;
            }

            if (socketButton != null)
            {
                socketButton.onClick.RemoveAllListeners();
                socketButton.onClick.AddListener(OnSocketClicked);
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

        public void OnPointerClick(PointerEventData eventData)
        {
            if (Combat.CombatManager.Instance != null && Combat.CombatManager.Instance.currentPhase == Combat.GamePhase.Running)
            {
                return;
            }

            if (eventData.button == PointerEventData.InputButton.Right)
            {
                UnslotToken();
            }
            else
            {
                OnSocketClicked();
            }
        }

        private void OnSocketClicked()
        {
            if (Combat.CombatManager.Instance != null && Combat.CombatManager.Instance.currentPhase == Combat.GamePhase.Running)
            {
                return;
            }

            if (IntelliSensePopoverUI.Instance != null)
            {
                IntelliSensePopoverUI.Instance.Show(this, transform.position);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isHighlightActive && socketOutline != null)
            {
                socketOutline.enabled = true;
                socketOutline.effectColor = new Color(0f, 0.48f, 0.8f, 0.85f); // VS Code Blue
                socketOutline.effectDistance = new Vector2(1f, -1f);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isHighlightActive)
            {
                UpdateIdleOutline();
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (Combat.CombatManager.Instance != null && Combat.CombatManager.Instance.currentPhase == Combat.GamePhase.Running)
            {
                return;
            }

            var draggedCard = eventData.pointerDrag?.GetComponent<DraggableTokenCardUI>();
            if (draggedCard == null || draggedCard.Token == null) return;

            if (draggedCard.Token.tokenType != expectedType)
            {
                LogTypeMismatchError(draggedCard.Token, expectedType);
                return;
            }

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
                string syntax = AssignedToken.GetFormattedCodeString();
                string coloredSyntax = expectedType switch
                {
                    CodeTokenType.Targeting => $"<color=#DCDCAA>{syntax}</color>",
                    CodeTokenType.Condition => $"<color=#9CDCFE>{syntax}</color>",
                    CodeTokenType.Action => $"<color=#DCDCAA>{syntax}</color>",
                    CodeTokenType.Float => $"<color=#B5CEA8>{syntax}</color>",
                    CodeTokenType.Int => $"<color=#B5CEA8>{syntax}</color>",
                    CodeTokenType.Stance => $"<color=#4EC9B0>{syntax}</color>",
                    _ => $"<color=#D4D4D4>{syntax}</color>"
                };

                socketValueText.text = $"<b>{coloredSyntax}</b>";
            }
            else
            {
                string prompt = expectedType switch
                {
                    CodeTokenType.Targeting => "<color=#6E6E6E>[ <color=#C586C0>target</color> ]</color>",
                    CodeTokenType.Condition => "<color=#6E6E6E>[ <color=#9CDCFE>condition</color> ]</color>",
                    CodeTokenType.Action => "<color=#6E6E6E>[ <color=#DCDCAA>action</color> ]</color>",
                    CodeTokenType.Float => "<color=#6E6E6E>[ <color=#B5CEA8>float</color> ]</color>",
                    CodeTokenType.Int => "<color=#6E6E6E>[ <color=#B5CEA8>int</color> ]</color>",
                    CodeTokenType.Stance => "<color=#6E6E6E>[ <color=#4EC9B0>stance</color> ]</color>",
                    _ => "<color=#6E6E6E>[ select ]</color>"
                };

                socketValueText.text = $"<i>{prompt}</i>";
            }

            if (!isHighlightActive)
            {
                UpdateIdleOutline();
            }
        }

        private void UpdateIdleOutline()
        {
            if (socketOutline == null) socketOutline = GetComponent<Outline>();
            if (socketOutline != null)
            {
                if (AssignedToken != null)
                {
                    socketOutline.enabled = true;
                    socketOutline.effectColor = new Color(0.25f, 0.25f, 0.32f, 0.4f);
                    socketOutline.effectDistance = new Vector2(1f, -1f);
                }
                else
                {
                    socketOutline.enabled = true;
                    socketOutline.effectColor = new Color(0.4f, 0.4f, 0.5f, 0.5f);
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
                    socketOutline.effectDistance = new Vector2(2.5f, -2.5f);
                }
                else
                {
                    UpdateIdleOutline();
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
            ConsoleLogUI.Log($"<color=#FF5454>[Compiler Error] CS0029: Cannot implicitly convert type '{token.tokenType.ToString().ToLower()}' to '{targetType.ToString().ToLower()}'</color>");
            StopAllCoroutines();
            StartCoroutine(FlashRedRoutine(0.4f));
        }

        private System.Collections.IEnumerator FlashRedRoutine(float duration)
        {
            SetHighlight(new Color(1f, 0.25f, 0.25f, 1f), true, 1.0f);
            yield return new WaitForSeconds(duration);
            SetHighlight(Color.white, false, 1.0f);
        }

        public void TriggerUnassignedPulsingHighlight()
        {
            StopAllCoroutines();
            StartCoroutine(PulsingRedRoutine(2.0f));
        }

        private System.Collections.IEnumerator PulsingRedRoutine(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = (Mathf.Sin(elapsed * 12f) + 1f) * 0.5f;
                Color pulseColor = Color.Lerp(new Color(1f, 0.2f, 0.2f, 0.3f), new Color(1f, 0.2f, 0.2f, 1f), alpha);
                SetHighlight(pulseColor, true, 1.0f);
                yield return null;
            }
            SetHighlight(Color.white, false, 1.0f);
        }
    }
}
