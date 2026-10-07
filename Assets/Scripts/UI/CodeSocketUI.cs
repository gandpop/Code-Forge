using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using CodeForge.Data;
using CodeForge.Combat;

namespace CodeForge.UI
{
    [ExecuteAlways]
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
        public CodeSocketRole SocketRole => socketRole;
        public CodeTokenType ExpectedType => expectedType;
        public CodeEditorPanelUI EditorUI => codeEditorUI != null ? codeEditorUI : (codeEditorUI = GetComponentInParent<CodeEditorPanelUI>() ?? Object.FindFirstObjectByType<CodeEditorPanelUI>());
        private bool isHighlightActive = false;

        private void Awake()
        {
            if (codeEditorUI == null) codeEditorUI = GetComponentInParent<CodeEditorPanelUI>() ?? Object.FindFirstObjectByType<CodeEditorPanelUI>();
            if (socketOutline == null) socketOutline = GetComponent<Outline>();
            if (socketCanvasGroup == null) socketCanvasGroup = GetComponent<CanvasGroup>();

            UpdateSocketWidth();

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
            if (eventData.dragging)
            {
                var draggedCard = eventData.pointerDrag?.GetComponent<DraggableTokenCardUI>();
                if (draggedCard != null && draggedCard.Token != null && socketOutline != null)
                {
                    bool compatible = IsTypeCompatible(draggedCard.Token.tokenType, expectedType);
                    socketOutline.enabled = true;
                    socketOutline.effectColor = compatible ? new Color(0.2f, 0.9f, 0.3f, 1f) : new Color(0.95f, 0.25f, 0.25f, 0.95f);
                    socketOutline.effectDistance = new Vector2(2f, -2f);
                    return;
                }
            }

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
            if (draggedCard == null) return;

            if (draggedCard.Token == null)
            {
                ConsoleLogUI.Log("<color=#FF5454>[Warning] Cannot slot an uninitialized or empty token card.</color>");
                return;
            }

            if (!IsTypeCompatible(draggedCard.Token.tokenType, expectedType))
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

        public static bool IsTypeCompatible(CodeTokenType incomingType, CodeTokenType expected)
        {
            if (incomingType == expected) return true;
            if (expected == CodeTokenType.Bool && incomingType == CodeTokenType.Condition) return true;
            if (expected == CodeTokenType.Condition && incomingType == CodeTokenType.Bool) return true;
            return false;
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
                if (IsStatDisplayRole(socketRole))
                {
                    EditorUI?.UpdateChanceRowDisplays();
                }
                OnTokenAssignedOrRemoved();
                ConsoleLogUI.Log($"[Syntax] Unslotted token from {socketRole}.");
            }
        }

        private static bool IsStatDisplayRole(CodeSocketRole role)
        {
            return role == CodeSocketRole.CritChance || role == CodeSocketRole.CritChancePercent ||
                   role == CodeSocketRole.CritMultiplier || role == CodeSocketRole.CritDamage ||
                   role == CodeSocketRole.EvasionChance || role == CodeSocketRole.EvasionChancePercent ||
                   role == CodeSocketRole.DamageReduction || role == CodeSocketRole.DamageMultiplier;
        }

        public void AssignToken(CodeTokenSO token)
        {
            if (token != null && !IsTypeCompatible(token.tokenType, expectedType))
            {
                LogTypeMismatchError(token, expectedType);
                return;
            }

            AssignedToken = token;
            RefreshDisplay();

            if (IsStatDisplayRole(socketRole))
            {
                EditorUI?.UpdateChanceRowDisplays();
            }
            OnTokenAssignedOrRemoved();
        }

        public void OnTokenAssignedOrRemoved()
        {
            if (socketRole == CodeSocketRole.MaxHealth)
            {
                int newHp = AssignedToken != null ? AssignedToken.intValue : 20;
                var player = Object.FindFirstObjectByType<PlayerCombatController>();
                if (player != null)
                {
                    player.SetMaxHealth(newHp);
                }
            }
            else if (socketRole == CodeSocketRole.BaseShield)
            {
                int newShield = AssignedToken != null ? AssignedToken.intValue : 0;
                var player = Object.FindFirstObjectByType<PlayerCombatController>();
                if (player != null)
                {
                    player.SetShield(newShield);
                }
            }
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
                    CodeTokenType.Bool => $"<color=#569CD6>{syntax}</color>",
                    CodeTokenType.Stance => $"<color=#4EC9B0>{syntax}</color>",
                    CodeTokenType.Operator => $"<color=#D4D4D4>{syntax}</color>",
                    _ => $"<color=#D4D4D4>{syntax}</color>"
                };

                socketValueText.text = $"<b>{coloredSyntax}</b>";
            }
            else
            {
                string prompt = expectedType switch
                {
                    CodeTokenType.Targeting => "<color=#6E6E6E>[ <color=#C586C0>target</color> ]</color>",
                    CodeTokenType.Condition => "<color=#6E6E6E>[ <color=#9CDCFE>condition</color> / <color=#569CD6>bool</color> ]</color>",
                    CodeTokenType.Action => "<color=#6E6E6E>[ <color=#DCDCAA>action</color> ]</color>",
                    CodeTokenType.Float => "<color=#6E6E6E>[ <color=#B5CEA8>float</color> ]</color>",
                    CodeTokenType.Int => "<color=#6E6E6E>[ <color=#B5CEA8>int</color> ]</color>",
                    CodeTokenType.Bool => "<color=#6E6E6E>[ <color=#569CD6>bool</color> ]</color>",
                    CodeTokenType.Stance => "<color=#6E6E6E>[ <color=#4EC9B0>stance</color> ]</color>",
                    CodeTokenType.Operator => "<color=#6E6E6E>[ <color=#D4D4D4>op</color> ]</color>",
                    _ => "<color=#6E6E6E>[ select ]</color>"
                };

                socketValueText.text = $"<i>{prompt}</i>";
            }

            if (!isHighlightActive)
            {
                UpdateIdleOutline();
            }

            UpdateSocketWidth();
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
            var row = GetComponentInParent<EditorLineRowUI>();
            int lineNum = row != null ? row.lineNumber : 0;
            string lineLocation = lineNum > 0 ? $"PlayerCombat.cs({lineNum}): error " : "";
            string onLine = lineNum > 0 ? $" on line {lineNum}" : "";
            ConsoleLogUI.Log($"<color=#FF5454>[Compiler Error] {lineLocation}CS0029: Cannot implicitly convert type '{token.tokenType.ToString().ToLower()}' to '{targetType.ToString().ToLower()}'{onLine}.</color>");
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

        public void InitializeRuntime(CodeSocketRole role, CodeTokenType expected, CodeEditorPanelUI editor)
        {
            socketRole = role;
            expectedType = expected;
            codeEditorUI = editor;

            if (socketOutline == null) socketOutline = GetComponent<Outline>();
            if (socketCanvasGroup == null) socketCanvasGroup = GetComponent<CanvasGroup>();
            if (socketValueText == null) socketValueText = GetComponentInChildren<TextMeshProUGUI>();
            if (socketButton == null) socketButton = GetComponent<Button>();

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

            RefreshDisplay();
            UpdateSocketWidth();
            SetHighlight(Color.white, false, 1.0f);
        }

        public void UpdateSocketWidth()
        {
            if (socketValueText == null) socketValueText = GetComponentInChildren<TextMeshProUGUI>();
            if (socketValueText == null) return;

            socketValueText.textWrappingMode = TextWrappingModes.NoWrap;
            socketValueText.overflowMode = TextOverflowModes.Ellipsis;
            socketValueText.alignment = TextAlignmentOptions.Center;
            socketValueText.margin = Vector4.zero;
            socketValueText.ForceMeshUpdate();

            float textWidth = socketValueText.preferredWidth;
            float minWidth = expectedType switch
            {
                CodeTokenType.Int => 55f,
                CodeTokenType.Float => 72f,
                CodeTokenType.Bool => 68f,
                CodeTokenType.Stance => 85f,
                CodeTokenType.Targeting => 95f,
                CodeTokenType.Action => 110f,
                CodeTokenType.Condition => 115f,
                _ => 55f
            };

            float targetWidth = Mathf.Max(minWidth, textWidth + 24f);

            var layout = GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.minWidth = targetWidth;
                layout.preferredWidth = targetWidth;
                layout.minHeight = 20f;
                layout.preferredHeight = 20f;
            }

            var rt = GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(targetWidth, 20f);
            }

            if (transform.parent != null)
            {
                var parentRt = transform.parent as RectTransform;
                if (parentRt != null)
                {
                    LayoutRebuilder.MarkLayoutForRebuild(parentRt);
                }
            }
        }
    }
}
