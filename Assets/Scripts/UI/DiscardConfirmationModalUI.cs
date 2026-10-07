using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using CodeForge.Data;

namespace CodeForge.UI
{
    public class DiscardConfirmationModalUI : MonoBehaviour
    {
        private static DiscardConfirmationModalUI instance;
        public static DiscardConfirmationModalUI Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<DiscardConfirmationModalUI>(FindObjectsInactive.Include);
                }
                return instance;
            }
        }

        public static bool SuppressDiscardPrompt { get; set; } = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            SuppressDiscardPrompt = false;
        }

        [SerializeField] private GameObject modalContainer;
        [SerializeField] private TextMeshProUGUI headerText;
        [SerializeField] private TextMeshProUGUI bodyText;
        [SerializeField] private Toggle dontAskAgainToggle;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button discardButton;

        private Action onConfirmDiscard;
        private Action onCancelDiscard;

        private void Awake()
        {
            if (instance == null) instance = this;
            SuppressDiscardPrompt = false; // Reset on startup
            BuildUIHierarchyIfNeeded();
            BindEvents();
            if (modalContainer != null) modalContainer.SetActive(false);
        }

        private void OnEnable()
        {
            BindEvents();
        }

        public static DiscardConfirmationModalUI GetOrCreate(Canvas targetCanvas = null)
        {
            if (Instance != null)
            {
                // Ensure existing instance is parented to top-level canvas and not ArenaUICanvas
                if (Instance.transform.parent != null && Instance.transform.parent.name == "ArenaUICanvas")
                {
                    Canvas rootCanvas = targetCanvas;
                    if (rootCanvas == null || rootCanvas.name == "ArenaUICanvas")
                    {
                        var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                        foreach (var c in canvases)
                        {
                            if (c != null && c.name != "ArenaUICanvas" && c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay)
                            {
                                rootCanvas = c;
                                break;
                            }
                        }
                    }
                    if (rootCanvas != null)
                    {
                        Instance.transform.SetParent(rootCanvas.transform, false);
                    }
                }
                Instance.transform.SetAsLastSibling();
                return Instance;
            }

            Canvas chosenCanvas = targetCanvas;
            if (chosenCanvas == null || chosenCanvas.name == "ArenaUICanvas")
            {
                var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var c in canvases)
                {
                    if (c != null && c.name != "ArenaUICanvas" && c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay)
                    {
                        chosenCanvas = c;
                        break;
                    }
                }

                if (chosenCanvas == null)
                {
                    foreach (var c in canvases)
                    {
                        if (c != null && c.name != "ArenaUICanvas" && c.isRootCanvas)
                        {
                            chosenCanvas = c;
                            break;
                        }
                    }
                }
            }

            if (chosenCanvas == null) return null;

            GameObject modalObj = new GameObject("DiscardConfirmationModal", typeof(RectTransform), typeof(CanvasRenderer));
            modalObj.transform.SetParent(chosenCanvas.transform, false);
            instance = modalObj.AddComponent<DiscardConfirmationModalUI>();
            instance.BuildUIHierarchyIfNeeded();
            instance.transform.SetAsLastSibling();
            return instance;
        }

        public void BuildUIHierarchyIfNeeded()
        {
            if (modalContainer != null) return;

            var existing = transform.Find("ModalContainer");
            if (existing != null)
            {
                modalContainer = existing.gameObject;
                headerText = modalContainer.transform.Find("Header")?.GetComponent<TextMeshProUGUI>();
                bodyText = modalContainer.transform.Find("Body")?.GetComponent<TextMeshProUGUI>();
                dontAskAgainToggle = modalContainer.transform.Find("DontAskToggle")?.GetComponent<Toggle>();
                cancelButton = modalContainer.transform.Find("CancelBtn")?.GetComponent<Button>();
                discardButton = modalContainer.transform.Find("DiscardBtn")?.GetComponent<Button>();
                BindEvents();
                return;
            }

            // Root blocker
            var rt = GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            // Dim Background Overlay
            GameObject blockerObj = new GameObject("DimOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            blockerObj.transform.SetParent(transform, false);
            var blRt = blockerObj.GetComponent<RectTransform>();
            blRt.anchorMin = Vector2.zero;
            blRt.anchorMax = Vector2.one;
            blRt.offsetMin = Vector2.zero;
            blRt.offsetMax = Vector2.zero;
            var blImg = blockerObj.GetComponent<Image>();
            blImg.color = new Color(0f, 0f, 0f, 0.65f);

            // Modal Container Dialog Box
            GameObject dialogObj = new GameObject("ModalContainer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
            dialogObj.transform.SetParent(transform, false);
            modalContainer = dialogObj;

            var dRt = dialogObj.GetComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0.30f, 0.35f);
            dRt.anchorMax = new Vector2(0.70f, 0.65f);
            dRt.offsetMin = Vector2.zero;
            dRt.offsetMax = Vector2.zero;

            var dImg = dialogObj.GetComponent<Image>();
            dImg.color = new Color(0.12f, 0.12f, 0.16f, 0.98f);

            var dOutline = dialogObj.GetComponent<Outline>();
            dOutline.effectColor = new Color(0.85f, 0.30f, 0.35f, 0.95f);
            dOutline.effectDistance = new Vector2(2.5f, -2.5f);

            // Header
            GameObject headObj = new GameObject("Header", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            headObj.transform.SetParent(dialogObj.transform, false);
            var hRt = headObj.GetComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0.05f, 0.72f);
            hRt.anchorMax = new Vector2(0.95f, 0.94f);
            hRt.offsetMin = Vector2.zero;
            hRt.offsetMax = Vector2.zero;
            headerText = headObj.GetComponent<TextMeshProUGUI>();
            headerText.text = "<color=#E06C75><b>CONFIRM TOKEN DISCARD</b></color>";
            headerText.fontSize = 17f;
            headerText.fontStyle = FontStyles.Bold;
            headerText.alignment = TextAlignmentOptions.Center;

            // Body
            GameObject bodyObj = new GameObject("Body", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            bodyObj.transform.SetParent(dialogObj.transform, false);
            var bRt = bodyObj.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.06f, 0.40f);
            bRt.anchorMax = new Vector2(0.94f, 0.70f);
            bRt.offsetMin = Vector2.zero;
            bRt.offsetMax = Vector2.zero;
            bodyText = bodyObj.GetComponent<TextMeshProUGUI>();
            bodyText.fontSize = 13.5f;
            bodyText.alignment = TextAlignmentOptions.Center;
            bodyText.richText = true;

            // Don't ask again toggle
            GameObject toggleObj = new GameObject("DontAskToggle", typeof(RectTransform), typeof(Toggle));
            toggleObj.transform.SetParent(dialogObj.transform, false);
            var tRt = toggleObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0.15f, 0.26f);
            tRt.anchorMax = new Vector2(0.85f, 0.38f);
            tRt.offsetMin = Vector2.zero;
            tRt.offsetMax = Vector2.zero;
            dontAskAgainToggle = toggleObj.GetComponent<Toggle>();

            // Checkbox background
            GameObject checkBgObj = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            checkBgObj.transform.SetParent(toggleObj.transform, false);
            var cbRt = checkBgObj.GetComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(0f, 0.5f);
            cbRt.anchorMax = new Vector2(0f, 0.5f);
            cbRt.pivot = new Vector2(0f, 0.5f);
            cbRt.sizeDelta = new Vector2(18f, 18f);
            var cbImg = checkBgObj.GetComponent<Image>();
            cbImg.color = new Color(0.20f, 0.22f, 0.28f, 1f);

            // Checkmark
            GameObject checkmarkObj = new GameObject("Checkmark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            checkmarkObj.transform.SetParent(checkBgObj.transform, false);
            var cmRt = checkmarkObj.GetComponent<RectTransform>();
            cmRt.anchorMin = new Vector2(0.15f, 0.15f);
            cmRt.anchorMax = new Vector2(0.85f, 0.85f);
            cmRt.offsetMin = Vector2.zero;
            cmRt.offsetMax = Vector2.zero;
            var cmImg = checkmarkObj.GetComponent<Image>();
            cmImg.color = new Color(0.95f, 0.75f, 0.30f, 1f);

            dontAskAgainToggle.targetGraphic = cbImg;
            dontAskAgainToggle.graphic = cmImg;
            dontAskAgainToggle.isOn = false;

            // Toggle Label
            GameObject toggleLabelObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            toggleLabelObj.transform.SetParent(toggleObj.transform, false);
            var tlRt = toggleLabelObj.GetComponent<RectTransform>();
            tlRt.anchorMin = new Vector2(0f, 0f);
            tlRt.anchorMax = new Vector2(1f, 1f);
            tlRt.offsetMin = new Vector2(26f, 0f);
            tlRt.offsetMax = Vector2.zero;
            var tlTmp = toggleLabelObj.GetComponent<TextMeshProUGUI>();
            tlTmp.text = "<size=90%><color=#ABB2BF>Don't ask me again</color></size>";
            tlTmp.fontSize = 12f;
            tlTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // Cancel Button
            GameObject cancelObj = new GameObject("CancelBtn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            cancelObj.transform.SetParent(dialogObj.transform, false);
            var cRt = cancelObj.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.12f, 0.08f);
            cRt.anchorMax = new Vector2(0.46f, 0.22f);
            cRt.offsetMin = Vector2.zero;
            cRt.offsetMax = Vector2.zero;
            cancelObj.GetComponent<Image>().color = new Color(0.24f, 0.27f, 0.35f, 1f);
            cancelButton = cancelObj.GetComponent<Button>();

            GameObject cTxtObj = new GameObject("Txt", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            cTxtObj.transform.SetParent(cancelObj.transform, false);
            var ctRt = cTxtObj.GetComponent<RectTransform>();
            ctRt.anchorMin = Vector2.zero;
            ctRt.anchorMax = Vector2.one;
            ctRt.offsetMin = Vector2.zero;
            ctRt.offsetMax = Vector2.zero;
            var ctTmp = cTxtObj.GetComponent<TextMeshProUGUI>();
            ctTmp.text = "Cancel";
            ctTmp.fontSize = 13f;
            ctTmp.fontStyle = FontStyles.Bold;
            ctTmp.alignment = TextAlignmentOptions.Center;

            // Discard Button
            GameObject discObj = new GameObject("DiscardBtn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            discObj.transform.SetParent(dialogObj.transform, false);
            var dcRt = discObj.GetComponent<RectTransform>();
            dcRt.anchorMin = new Vector2(0.54f, 0.08f);
            dcRt.anchorMax = new Vector2(0.88f, 0.22f);
            dcRt.offsetMin = Vector2.zero;
            dcRt.offsetMax = Vector2.zero;
            discObj.GetComponent<Image>().color = new Color(0.65f, 0.22f, 0.25f, 1f);
            discardButton = discObj.GetComponent<Button>();

            GameObject dTxtObj = new GameObject("Txt", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            dTxtObj.transform.SetParent(discObj.transform, false);
            var dtRt = dTxtObj.GetComponent<RectTransform>();
            dtRt.anchorMin = Vector2.zero;
            dtRt.anchorMax = Vector2.one;
            dtRt.offsetMin = Vector2.zero;
            dtRt.offsetMax = Vector2.zero;
            var dtTmp = dTxtObj.GetComponent<TextMeshProUGUI>();
            dtTmp.text = "Discard";
            dtTmp.fontSize = 13f;
            dtTmp.fontStyle = FontStyles.Bold;
            dtTmp.alignment = TextAlignmentOptions.Center;

            BindEvents();
        }

        private void BindEvents()
        {
            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveAllListeners();
                cancelButton.onClick.AddListener(OnCancelClicked);
            }

            if (discardButton != null)
            {
                discardButton.onClick.RemoveAllListeners();
                discardButton.onClick.AddListener(OnDiscardClicked);
            }
        }

        public void PromptDiscard(CodeTokenSO token, Action onConfirm, Action onCancel)
        {
            if (token == null) return;

            if (SuppressDiscardPrompt)
            {
                onConfirm?.Invoke();
                return;
            }

            BuildUIHierarchyIfNeeded();
            BindEvents();
            onConfirmDiscard = onConfirm;
            onCancelDiscard = onCancel;

            string tokenName = !string.IsNullOrEmpty(token.tokenName) ? token.tokenName : "this token";
            string rarityHex = token.GetRarityHexColor();
            if (bodyText != null)
            {
                bodyText.text = $"Are you sure you want to permanently discard\n<color={rarityHex}><b>{tokenName}</b></color>?";
            }

            if (dontAskAgainToggle != null)
            {
                dontAskAgainToggle.isOn = false;
            }

            gameObject.SetActive(true);
            if (modalContainer != null) modalContainer.SetActive(true);
            transform.SetAsLastSibling();
        }

        private void OnDiscardClicked()
        {
            if (dontAskAgainToggle != null && dontAskAgainToggle.isOn)
            {
                SuppressDiscardPrompt = true;
            }

            Hide();
            onConfirmDiscard?.Invoke();
            onConfirmDiscard = null;
            onCancelDiscard = null;
        }

        private void OnCancelClicked()
        {
            Hide();
            onCancelDiscard?.Invoke();
            onConfirmDiscard = null;
            onCancelDiscard = null;
        }

        public void Hide()
        {
            if (modalContainer != null) modalContainer.SetActive(false);
            gameObject.SetActive(false);
        }
    }
}
