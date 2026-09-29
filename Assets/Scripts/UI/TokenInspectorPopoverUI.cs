using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CodeForge.Data;

namespace CodeForge.UI
{
    public class TokenInspectorPopoverUI : MonoBehaviour
    {
        private static TokenInspectorPopoverUI instance;
        public static TokenInspectorPopoverUI Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<TokenInspectorPopoverUI>(FindObjectsInactive.Include);
                    if (instance == null)
                    {
                        var canvas = FindFirstObjectByType<Canvas>();
                        if (canvas != null)
                        {
                            var go = new GameObject("TokenInspectorPopover", typeof(RectTransform), typeof(CanvasRenderer));
                            go.transform.SetParent(canvas.transform, false);
                            instance = go.AddComponent<TokenInspectorPopoverUI>();
                        }
                    }
                }
                return instance;
            }
            private set => instance = value;
        }

        [Header("UI Containers")]
        [SerializeField] private GameObject rootContainer;
        [SerializeField] private RectTransform popoverRect;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI typeBadgeText;
        [SerializeField] private TextMeshProUGUI syntaxText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI placementText;
        [SerializeField] private Button closeButton;

        private GameObject backdropObj;

        private void Awake()
        {
            if (instance == null) instance = this;
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }

            BuildUIHierarchyIfNeeded();
            ApplyLayoutDimensions();
            EnsureBackdrop();

            if (closeButton == null)
            {
                closeButton = transform.Find("CloseBtn")?.GetComponent<Button>() ?? GetComponentInChildren<Button>(true);
            }
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Hide);
                var childTmps = closeButton.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var t in childTmps)
                {
                    t.raycastTarget = false;
                }
            }

            if (rootContainer != null)
            {
                rootContainer.SetActive(false);
            }
        }

        private void EnsureBackdrop()
        {
            if (backdropObj != null) return;

            Canvas rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas != null) rootCanvas = rootCanvas.rootCanvas;
            Transform parent = rootCanvas != null ? rootCanvas.transform : transform.parent;

            backdropObj = new GameObject("TokenInspectorBackdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            backdropObj.transform.SetParent(parent, false);

            int index = transform.GetSiblingIndex();
            backdropObj.transform.SetSiblingIndex(Mathf.Max(0, index));

            var rect = backdropObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            var img = backdropObj.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.25f); // Soft dimmed backdrop
            img.raycastTarget = true;

            var btn = backdropObj.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(Hide);

            backdropObj.SetActive(false);
        }

        public void BuildUIHierarchyIfNeeded()
        {
            if (popoverRect != null && rootContainer != null) return;

            var rect = GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(340f, 260f);

            rootContainer = gameObject;
            popoverRect = rect;

            // Background Image
            var bgImg = GetComponent<Image>();
            if (bgImg == null) bgImg = gameObject.AddComponent<Image>();
            bgImg.color = new Color(0.12f, 0.14f, 0.18f, 0.98f);

            var outline = GetComponent<Outline>();
            if (outline == null) outline = gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.3f, 0.5f, 0.8f, 0.8f);
            outline.effectDistance = new Vector2(2f, -2f);

            // Close button in top-right
            var closeGo = new GameObject("CloseBtn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(transform, false);
            var closeRect = closeGo.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-8f, -8f);
            closeRect.sizeDelta = new Vector2(24f, 24f);
            closeGo.GetComponent<Image>().color = new Color(0.3f, 0.35f, 0.45f, 0.8f);
            closeButton = closeGo.GetComponent<Button>();
            closeButton.onClick.AddListener(Hide);

            var closeTxtGo = new GameObject("Txt", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            closeTxtGo.transform.SetParent(closeGo.transform, false);
            var closeTxt = closeTxtGo.GetComponent<TextMeshProUGUI>();
            closeTxt.text = "X";
            closeTxt.fontSize = 14f;
            closeTxt.alignment = TextAlignmentOptions.Center;
            closeTxt.color = Color.white;
            closeTxt.raycastTarget = false;
            closeTxtGo.GetComponent<RectTransform>().sizeDelta = new Vector2(24f, 24f);

            // Title Text
            var titleGo = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(transform, false);
            var titleR = titleGo.GetComponent<RectTransform>();
            titleR.anchorMin = new Vector2(0f, 1f);
            titleR.anchorMax = new Vector2(1f, 1f);
            titleR.pivot = new Vector2(0f, 1f);
            titleR.anchoredPosition = new Vector2(16f, -12f);
            titleR.sizeDelta = new Vector2(-48f, 28f);
            titleText = titleGo.GetComponent<TextMeshProUGUI>();
            titleText.fontSize = 17f;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = Color.white;

            // Type & Syntax Tag
            var syntaxGo = new GameObject("SyntaxText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            syntaxGo.transform.SetParent(transform, false);
            var synR = syntaxGo.GetComponent<RectTransform>();
            synR.anchorMin = new Vector2(0f, 1f);
            synR.anchorMax = new Vector2(1f, 1f);
            synR.pivot = new Vector2(0f, 1f);
            synR.anchoredPosition = new Vector2(16f, -44f);
            synR.sizeDelta = new Vector2(-32f, 24f);
            syntaxText = syntaxGo.GetComponent<TextMeshProUGUI>();
            syntaxText.fontSize = 13.5f;

            // Description Body
            var descGo = new GameObject("DescText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            descGo.transform.SetParent(transform, false);
            var descR = descGo.GetComponent<RectTransform>();
            descR.anchorMin = new Vector2(0f, 0f);
            descR.anchorMax = new Vector2(1f, 1f);
            descR.pivot = new Vector2(0f, 1f);
            descR.offsetMin = new Vector2(16f, 50f);
            descR.offsetMax = new Vector2(-16f, -76f);
            descriptionText = descGo.GetComponent<TextMeshProUGUI>();
            descriptionText.fontSize = 13f;
            descriptionText.color = new Color(0.85f, 0.88f, 0.92f, 1f);
            descriptionText.enableWordWrapping = true;
            descriptionText.lineSpacing = 2f;

            // Placement Footer
            var placeGo = new GameObject("PlacementText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            placeGo.transform.SetParent(transform, false);
            var placeR = placeGo.GetComponent<RectTransform>();
            placeR.anchorMin = new Vector2(0f, 0f);
            placeR.anchorMax = new Vector2(1f, 0f);
            placeR.pivot = new Vector2(0f, 0f);
            placeR.anchoredPosition = new Vector2(16f, 12f);
            placeR.sizeDelta = new Vector2(-32f, 34f);
            placementText = placeGo.GetComponent<TextMeshProUGUI>();
            placementText.fontSize = 11.5f;
            placementText.color = new Color(0.6f, 0.75f, 0.65f, 1f);
            placementText.enableWordWrapping = true;

            ApplyLayoutDimensions();
        }

        public void ApplyLayoutDimensions()
        {
            if (titleText != null)
            {
                var titleR = titleText.rectTransform;
                titleR.anchorMin = new Vector2(0f, 1f);
                titleR.anchorMax = new Vector2(1f, 1f);
                titleR.pivot = new Vector2(0f, 1f);
                titleR.anchoredPosition = new Vector2(16f, -12f);
                titleR.sizeDelta = new Vector2(-48f, 28f);
            }

            if (syntaxText != null)
            {
                var synR = syntaxText.rectTransform;
                synR.anchorMin = new Vector2(0f, 1f);
                synR.anchorMax = new Vector2(1f, 1f);
                synR.pivot = new Vector2(0f, 1f);
                synR.anchoredPosition = new Vector2(16f, -44f);
                synR.sizeDelta = new Vector2(-32f, 24f);
            }

            if (descriptionText != null)
            {
                var descR = descriptionText.rectTransform;
                descR.anchorMin = new Vector2(0f, 0f);
                descR.anchorMax = new Vector2(1f, 1f);
                descR.pivot = new Vector2(0f, 1f);
                descR.offsetMin = new Vector2(16f, 50f);
                descR.offsetMax = new Vector2(-16f, -76f);
                descriptionText.enableWordWrapping = true;
                descriptionText.lineSpacing = 2f;
            }

            if (placementText != null)
            {
                var placeR = placementText.rectTransform;
                placeR.anchorMin = new Vector2(0f, 0f);
                placeR.anchorMax = new Vector2(1f, 0f);
                placeR.pivot = new Vector2(0f, 0f);
                placeR.anchoredPosition = new Vector2(16f, 12f);
                placeR.sizeDelta = new Vector2(-32f, 34f);
                placementText.enableWordWrapping = true;
            }
        }

        public void Show(CodeTokenSO token, Vector3 originScreenPos)
        {
            if (token == null) return;

            gameObject.SetActive(true);
            BuildUIHierarchyIfNeeded();
            ApplyLayoutDimensions();
            EnsureBackdrop();

            string rarityHex = token.GetRarityHexColor();
            string rarityName = token.rarity.ToString().ToUpper();
            string typeName = token.tokenType.ToString().ToLower();

            if (titleText != null)
            {
                titleText.text = $"<color={rarityHex}>[{rarityName}]</color> {token.tokenName}";
            }

            if (syntaxText != null)
            {
                syntaxText.text = $"Type: <color=#569CD6>{typeName}</color>  |  Syntax: <color=#4EC9B0>{token.GetFormattedCodeString()}</color>";
            }

            if (descriptionText != null)
            {
                descriptionText.text = token.GetDescription();
            }

            if (placementText != null)
            {
                placementText.text = GetPlacementAdvice(token);
            }

            // Position near the clicked card, clamped to screen margins
            if (popoverRect != null)
            {
                Vector3 targetPos = originScreenPos + new Vector3(190f, 40f, 0f);
                float clampX = Mathf.Clamp(targetPos.x, popoverRect.rect.width * 0.55f, Screen.width - popoverRect.rect.width * 0.55f);
                float clampY = Mathf.Clamp(targetPos.y, popoverRect.rect.height * 0.55f, Screen.height - popoverRect.rect.height * 0.55f);
                popoverRect.position = new Vector3(clampX, clampY, 0f);
            }

            if (backdropObj != null) backdropObj.SetActive(true);
            if (rootContainer != null) rootContainer.SetActive(true);

            transform.SetAsLastSibling();
        }

        public void Hide()
        {
            if (rootContainer != null && rootContainer != gameObject) rootContainer.SetActive(false);
            if (backdropObj != null) backdropObj.SetActive(false);
            gameObject.SetActive(false);
        }

        private string GetPlacementAdvice(CodeTokenSO token)
        {
            return token.tokenType switch
            {
                CodeTokenType.Int => "Compatible with: int sockets (maxHealth, baseShield, damageReduction, Attack.damage, Defend.shield, critChance%, evasionChance%)",
                CodeTokenType.Float => "Compatible with: float sockets (damageMultiplier, critMultiplier)",
                CodeTokenType.Bool => "Compatible with: bool sockets (applyBleed, condition sockets)",
                CodeTokenType.Condition => "Compatible with: if (...) condition sockets in ExecuteTurn & OnTakeDamage",
                CodeTokenType.Action => "Compatible with: then/else statement sockets (Attack, Defend, Pierce)",
                CodeTokenType.Targeting => "Compatible with: targeting sockets (var target = Enemies.X)",
                CodeTokenType.Operator => "Compatible with: compound operator sockets (&&, ||, !)",
                _ => "Drag and drop onto compatible glowing sockets in the editor."
            };
        }
    }
}
