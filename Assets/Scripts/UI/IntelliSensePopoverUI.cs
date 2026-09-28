using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CodeForge.Data;

namespace CodeForge.UI
{
    public class IntelliSensePopoverUI : MonoBehaviour
    {
        private static IntelliSensePopoverUI instance;
        public static IntelliSensePopoverUI Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<IntelliSensePopoverUI>(FindObjectsInactive.Include);
                }
                return instance;
            }
            private set
            {
                instance = value;
            }
        }

        [Header("UI References")]
        [SerializeField] private GameObject rootContainer;
        [SerializeField] private RectTransform popoverRect;
        [SerializeField] private Transform optionsContainer;
        [SerializeField] private GameObject optionItemPrefab;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI documentationText;
        [SerializeField] private Button closeButton;

        [Header("Token Sources")]
        [SerializeField] private List<CodeTokenSO> availableTokens = new List<CodeTokenSO>();

        private CodeSocketUI activeSocket;
        private GameObject backdropObj;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(gameObject);

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Hide);
            }

            EnsureBackdrop();

            if (rootContainer != null)
            {
                rootContainer.SetActive(false);
            }
        }

        private void EnsureBackdrop()
        {
            if (backdropObj != null) return;

            // Must be parented to the ROOT CANVAS, not the local popover rect!
            Canvas rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas != null)
            {
                rootCanvas = rootCanvas.rootCanvas;
            }
            Transform parent = rootCanvas != null ? rootCanvas.transform : transform.parent;

            backdropObj = new GameObject("IntelliSenseBackdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            backdropObj.transform.SetParent(parent, false);

            // Position directly behind popover in hierarchy
            int popoverIndex = transform.GetSiblingIndex();
            backdropObj.transform.SetSiblingIndex(Mathf.Max(0, popoverIndex));

            var rect = backdropObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            var img = backdropObj.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.001f); // Invisible raycast shield
            img.raycastTarget = true;

            var btn = backdropObj.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(Hide);

            backdropObj.SetActive(false);
        }

        private void Update()
        {
            if (rootContainer != null && rootContainer.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    Hide();
                    return;
                }

                if (Input.GetMouseButtonDown(0))
                {
                    RectTransform rect = popoverRect != null ? popoverRect : GetComponent<RectTransform>();
                    if (rect != null && !RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition))
                    {
                        if (activeSocket == null || !RectTransformUtility.RectangleContainsScreenPoint(activeSocket.GetComponent<RectTransform>(), Input.mousePosition))
                        {
                            Hide();
                        }
                    }
                }
            }
        }

        public void RegisterAvailableTokens(IEnumerable<CodeTokenSO> tokens)
        {
            foreach (var t in tokens)
            {
                if (t != null && !availableTokens.Contains(t))
                {
                    availableTokens.Add(t);
                }
            }
        }

        public void Show(CodeSocketUI socket, Vector3 position)
        {
            if (socket == null) return;
            activeSocket = socket;

            EnsureBackdrop();
            if (backdropObj != null)
            {
                int popoverIndex = transform.GetSiblingIndex();
                backdropObj.transform.SetSiblingIndex(Mathf.Max(0, popoverIndex));
                backdropObj.SetActive(true);
            }

            if (rootContainer != null)
            {
                rootContainer.SetActive(true);
            }

            if (popoverRect != null)
            {
                // Scale up by 20%
                popoverRect.localScale = Vector3.one * 1.2f;

                // Offset position so it doesn't cover the clicked socket
                Vector3 targetPos = position + new Vector3(35f, -20f, 0f);
                popoverRect.position = targetPos;
                ClampToScreen(popoverRect);
            }

            if (titleText != null)
            {
                titleText.text = $"<color=#569CD6>IntelliSense</color> <color=#858585>|</color> Expected: <color=#4EC9B0>{socket.expectedType}</color>";
            }

            PopulateOptions(socket.expectedType);
        }

        private void ClampToScreen(RectTransform rect)
        {
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            // corners: 0=bottom-left, 1=top-left, 2=top-right, 3=bottom-right
            float minX = corners[0].x;
            float maxX = corners[2].x;
            float minY = corners[0].y;
            float maxY = corners[1].y;

            Vector3 shift = Vector3.zero;
            if (minX < 10f) shift.x = 10f - minX;
            if (maxX > Screen.width - 10f) shift.x = (Screen.width - 10f) - maxX;
            if (minY < 10f) shift.y = 10f - minY;
            if (maxY > Screen.height - 10f) shift.y = (Screen.height - 10f) - maxY;

            rect.position += shift;
        }

        public void Hide()
        {
            if (backdropObj != null)
            {
                backdropObj.SetActive(false);
            }
            if (rootContainer != null)
            {
                rootContainer.SetActive(false);
            }
            activeSocket = null;
        }

        private void PopulateOptions(CodeTokenType expectedType)
        {
            if (optionsContainer == null) return;

            for (int i = optionsContainer.childCount - 1; i >= 0; i--)
            {
                var child = optionsContainer.GetChild(i);
                child.SetParent(null);
                Destroy(child.gameObject);
            }

            var editorUI = activeSocket != null ? activeSocket.EditorUI : null;
            List<CodeTokenSO> candidateTokens = new List<CodeTokenSO>();

            // Source tokens from player inventory
            if (editorUI != null)
            {
                var invTokens = editorUI.GetInventoryTokens();
                for (int i = 0; i < invTokens.Count; i++)
                {
                    var t = invTokens[i];
                    if (t != null && CodeSocketUI.IsTypeCompatible(t.tokenType, expectedType) && !candidateTokens.Contains(t))
                    {
                        candidateTokens.Add(t);
                    }
                }
            }

            if (candidateTokens.Count == 0)
            {
                if (documentationText != null)
                {
                    documentationText.text = $"<color=#858585>No tokens of type '{expectedType}' in inventory.\nClear rooms or draft rewards to acquire more tokens.</color>";
                }
                return;
            }

            for (int i = 0; i < candidateTokens.Count; i++)
            {
                var token = candidateTokens[i];
                GameObject itemObj = null;

                if (optionItemPrefab != null)
                {
                    itemObj = Instantiate(optionItemPrefab, optionsContainer);
                }
                else
                {
                    itemObj = CreateDefaultItem(token);
                    itemObj.transform.SetParent(optionsContainer, false);
                }

                BindOptionItem(itemObj, token);
            }

            // Preview first item documentation
            if (candidateTokens.Count > 0)
            {
                ShowDocumentation(candidateTokens[0]);
            }
        }

        private void BindOptionItem(GameObject itemObj, CodeTokenSO token)
        {
            var btn = itemObj.GetComponent<Button>();
            if (btn == null) btn = itemObj.AddComponent<Button>();

            var text = itemObj.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                string badge = token.tokenType switch
                {
                    CodeTokenType.Targeting => "<color=#C586C0>[target]</color>",
                    CodeTokenType.Condition => "<color=#DCDCAA>[bool]</color>",
                    CodeTokenType.Action => "<color=#4EC9B0>[action]</color>",
                    CodeTokenType.Float => "<color=#B5CEA8>[float]</color>",
                    CodeTokenType.Int => "<color=#B5CEA8>[int]</color>",
                    CodeTokenType.Stance => "<color=#9CDCFE>[stance]</color>",
                    _ => "<color=#858585>[token]</color>"
                };

                text.text = $"{badge} <color=#D4D4D4>{token.GetFormattedCodeString()}</color>";
            }

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                if (activeSocket != null)
                {
                    var editorUI = activeSocket.EditorUI;
                    var oldToken = activeSocket.AssignedToken;

                    if (oldToken != null && oldToken != token && editorUI != null)
                    {
                        editorUI.AddTokenToInventory(oldToken);
                    }

                    if (editorUI != null)
                    {
                        editorUI.RemoveTokenFromInventory(token);
                    }

                    activeSocket.AssignToken(token);
                    ConsoleLogUI.Log($"[IntelliSense] Autocompleted '{token.GetFormattedCodeString()}' into [{activeSocket.socketRole}].");
                }
                Hide();
            });

            // Hover preview documentation
            var trigger = itemObj.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (trigger == null) trigger = itemObj.AddComponent<UnityEngine.EventSystems.EventTrigger>();

            var entry = new UnityEngine.EventSystems.EventTrigger.Entry();
            entry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
            entry.callback.AddListener((data) => { ShowDocumentation(token); });
            trigger.triggers.Add(entry);
        }

        public void ShowDocumentation(CodeTokenSO token)
        {
            if (documentationText == null || token == null) return;

            string desc = !string.IsNullOrEmpty(token.description) ? token.description : $"Code definition: {token.GetFormattedCodeString()}";
            documentationText.text = $"<b><color=#4EC9B0>{token.tokenName}</color></b>\n<color=#D4D4D4>{desc}</color>\n<size=80%><color=#858585>Type: {token.tokenType} | Rarity: {token.rarity}</color></size>";
        }

        private GameObject CreateDefaultItem(CodeTokenSO token)
        {
            var item = new GameObject($"Option_{token.name}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
            var img = item.GetComponent<Image>();
            img.color = new Color(0.18f, 0.18f, 0.22f, 0.95f);

            var le = item.GetComponent<LayoutElement>();
            le.minHeight = 28f;
            le.preferredHeight = 28f;

            var txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(item.transform, false);
            var rect = txtObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.05f, 0f);
            rect.anchorMax = new Vector2(0.95f, 1f);
            rect.sizeDelta = Vector2.zero;

            var tmp = txtObj.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = 12f;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;

            return item;
        }
    }
}
