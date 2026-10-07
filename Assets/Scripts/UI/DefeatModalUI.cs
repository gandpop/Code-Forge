using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CodeForge.Combat;

namespace CodeForge.UI
{
    public class DefeatModalUI : MonoBehaviour
    {
        private static DefeatModalUI instance;
        public static DefeatModalUI Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<DefeatModalUI>(FindObjectsInactive.Include);
                    if (instance == null)
                    {
                        var canvas = FindFirstObjectByType<Canvas>();
                        if (canvas != null)
                        {
                            var go = new GameObject("DefeatModal", typeof(RectTransform), typeof(CanvasRenderer));
                            go.transform.SetParent(canvas.transform, false);
                            instance = go.AddComponent<DefeatModalUI>();
                        }
                    }
                }
                return instance;
            }
            private set => instance = value;
        }

        [Header("UI Containers")]
        [SerializeField] private GameObject rootContainer;
        [SerializeField] private TextMeshProUGUI headerText;
        [SerializeField] private TextMeshProUGUI bodyText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button restartButton;

        private void Awake()
        {
            if (instance == null) instance = this;
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }

            BuildUIHierarchyIfNeeded();
            BindButtons();

            if (rootContainer != null)
            {
                rootContainer.SetActive(false);
            }
        }

        private void BindButtons()
        {
            if (retryButton != null)
            {
                retryButton.onClick.RemoveAllListeners();
                retryButton.onClick.AddListener(OnRetryClicked);
            }
            if (restartButton != null)
            {
                restartButton.onClick.RemoveAllListeners();
                restartButton.onClick.AddListener(OnRestartClicked);
            }
        }

        private void Start()
        {
            BindButtons();
            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.OnPhaseChanged += HandlePhaseChanged;
            }
        }

        private void OnDestroy()
        {
            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
            }
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            if (phase == GamePhase.Defeat)
            {
                Show();
            }
            else
            {
                Hide();
            }
        }

        public void BuildUIHierarchyIfNeeded()
        {
            if (rootContainer != null && retryButton != null) return;

            rootContainer = gameObject;

            var rect = GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.28f, 0.30f);
            rect.anchorMax = new Vector2(0.72f, 0.70f);
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            var bg = GetComponent<Image>();
            if (bg == null) bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.08f, 0.10f, 0.96f);

            var outline = GetComponent<Outline>();
            if (outline == null) outline = gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.9f, 0.2f, 0.2f, 0.8f);
            outline.effectDistance = new Vector2(3f, -3f);

            // Header Text
            var headGo = new GameObject("HeaderText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            headGo.transform.SetParent(transform, false);
            var headRect = headGo.GetComponent<RectTransform>();
            headRect.anchorMin = new Vector2(0.05f, 0.70f);
            headRect.anchorMax = new Vector2(0.95f, 0.94f);
            headRect.sizeDelta = Vector2.zero;
            headRect.anchoredPosition = Vector2.zero;

            headerText = headGo.GetComponent<TextMeshProUGUI>();
            headerText.text = "<color=#FF5454>SYSTEM CRASH: EXECUTION TERMINATED</color>";
            headerText.fontSize = 20f;
            headerText.fontStyle = FontStyles.Bold;
            headerText.alignment = TextAlignmentOptions.Center;

            // Body Text
            var bodyGo = new GameObject("BodyText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            bodyGo.transform.SetParent(transform, false);
            var bodyRect = bodyGo.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0.08f, 0.42f);
            bodyRect.anchorMax = new Vector2(0.92f, 0.68f);
            bodyRect.sizeDelta = Vector2.zero;
            bodyRect.anchoredPosition = Vector2.zero;

            bodyText = bodyGo.GetComponent<TextMeshProUGUI>();
            bodyText.text = "Player health reached 0 HP.\nPipeline execution halted due to fatal exception.";
            bodyText.fontSize = 14.5f;
            bodyText.alignment = TextAlignmentOptions.Center;
            bodyText.color = new Color(0.85f, 0.85f, 0.90f, 1f);

            // Retry Button
            var retryGo = new GameObject("RetryButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            retryGo.transform.SetParent(transform, false);
            var retryRect = retryGo.GetComponent<RectTransform>();
            retryRect.anchorMin = new Vector2(0.12f, 0.12f);
            retryRect.anchorMax = new Vector2(0.48f, 0.32f);
            retryRect.sizeDelta = Vector2.zero;
            retryRect.anchoredPosition = Vector2.zero;

            retryGo.GetComponent<Image>().color = new Color(0.20f, 0.50f, 0.30f, 1f);
            retryButton = retryGo.GetComponent<Button>();
            retryButton.onClick.AddListener(OnRetryClicked);

            var retryTxtGo = new GameObject("Txt", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            retryTxtGo.transform.SetParent(retryGo.transform, false);
            var retryTxtRect = retryTxtGo.GetComponent<RectTransform>();
            retryTxtRect.anchorMin = Vector2.zero;
            retryTxtRect.anchorMax = Vector2.one;
            retryTxtRect.sizeDelta = Vector2.zero;
            retryTxtRect.anchoredPosition = Vector2.zero;
            var rTxt = retryTxtGo.GetComponent<TextMeshProUGUI>();
            rTxt.text = "Retry Room";
            rTxt.fontSize = 15f;
            rTxt.fontStyle = FontStyles.Bold;
            rTxt.alignment = TextAlignmentOptions.Center;
            rTxt.color = Color.white;

            // Restart Run Button
            var restartGo = new GameObject("RestartButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            restartGo.transform.SetParent(transform, false);
            var restartRect = restartGo.GetComponent<RectTransform>();
            restartRect.anchorMin = new Vector2(0.52f, 0.12f);
            restartRect.anchorMax = new Vector2(0.88f, 0.32f);
            restartRect.sizeDelta = Vector2.zero;
            restartRect.anchoredPosition = Vector2.zero;

            restartGo.GetComponent<Image>().color = new Color(0.50f, 0.25f, 0.25f, 1f);
            restartButton = restartGo.GetComponent<Button>();
            restartButton.onClick.AddListener(OnRestartClicked);

            var restartTxtGo = new GameObject("Txt", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            restartTxtGo.transform.SetParent(restartGo.transform, false);
            var restartTxtRect = restartTxtGo.GetComponent<RectTransform>();
            restartTxtRect.anchorMin = Vector2.zero;
            restartTxtRect.anchorMax = Vector2.one;
            restartTxtRect.sizeDelta = Vector2.zero;
            restartTxtRect.anchoredPosition = Vector2.zero;
            var restTxt = restartTxtGo.GetComponent<TextMeshProUGUI>();
            restTxt.text = "Restart Run";
            restTxt.fontSize = 15f;
            restTxt.fontStyle = FontStyles.Bold;
            restTxt.alignment = TextAlignmentOptions.Center;
            restTxt.color = Color.white;
        }

        public void Show()
        {
            gameObject.SetActive(true);
            BuildUIHierarchyIfNeeded();
            if (headerText != null)
            {
                headerText.text = "<color=#FF5454>SYSTEM CRASH: EXECUTION TERMINATED</color>";
            }
            if (bodyText != null)
            {
                bodyText.text = "Player health reached 0 HP.\nPipeline execution halted due to fatal exception.";
            }
            if (retryButton != null)
            {
                var txt = retryButton.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = "Retry Room";
            }
            if (rootContainer != null) rootContainer.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void ShowRuntimeError(string errorMessage = null)
        {
            gameObject.SetActive(true);
            BuildUIHierarchyIfNeeded();
            if (headerText != null)
            {
                headerText.text = "<color=#FFCC00>[!] RUNTIME ERROR: EXECUTION TIMEOUT</color>";
            }
            if (bodyText != null)
            {
                bodyText.text = errorMessage ?? "Execution halted to prevent application freeze. Potential circular call or infinite loop in PlayerCombat.cs.";
            }
            if (retryButton != null)
            {
                var txt = retryButton.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = "Retry Room";
            }
            if (rootContainer != null) rootContainer.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Hide()
        {
            if (rootContainer != null && rootContainer != gameObject) rootContainer.SetActive(false);
            gameObject.SetActive(false);
        }

        private void OnRetryClicked()
        {
            Hide();
            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.RetryCurrentRoom();
            }
        }

        private void OnRestartClicked()
        {
            Hide();
            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.RestartFullRun();
            }
        }
    }
}
