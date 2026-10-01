using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CodeForge.UI
{
    [ExecuteAlways]
    public class ConsoleLogUI : MonoBehaviour
    {
        private static ConsoleLogUI instance;
        [SerializeField] private TextMeshProUGUI logTextDisplay;
        [SerializeField] private ScrollRect logScrollRect;

        private StringBuilder logHistory = new StringBuilder();
        private const int MaxLogLines = 150;
        private int currentLineCount = 0;
        private Coroutine scrollCoroutine;

        public ScrollRect LogScrollRect
        {
            get => logScrollRect;
            set => logScrollRect = value;
        }

        public TextMeshProUGUI LogTextDisplay => logTextDisplay;

        private void Awake()
        {
            if (Application.isPlaying)
            {
                instance = this;
            }

            SetupConsoleHierarchy();

            if (Application.isPlaying)
            {
                if (logTextDisplay != null && !string.IsNullOrEmpty(logTextDisplay.text))
                {
                    string initial = logTextDisplay.text;
                    if (initial.Contains("[Unlock] Clear Room"))
                    {
                        logHistory.Clear();
                        logTextDisplay.text = "";
                        currentLineCount = 0;
                    }
                    else
                    {
                        logHistory.Clear();
                        logHistory.Append(initial);
                        if (!initial.EndsWith("\n")) logHistory.AppendLine();
                        currentLineCount = initial.Split('\n').Length;
                    }
                }

                if (logScrollRect != null)
                {
                    logScrollRect.verticalNormalizedPosition = (currentLineCount <= 6) ? 1f : 0f;
                }
            }
        }

        private void Start()
        {
            if (logScrollRect != null)
            {
                logScrollRect.verticalNormalizedPosition = (currentLineCount <= 6) ? 1f : 0f;
            }
        }

        private void OnEnable()
        {
            SetupConsoleHierarchy();
            if (logScrollRect != null)
            {
                logScrollRect.verticalNormalizedPosition = (currentLineCount <= 6) ? 1f : 0f;
            }
        }

        public void SetupConsoleHierarchy()
        {
            var panelImg = GetComponent<Image>();
            if (panelImg != null)
            {
                panelImg.color = new Color(0.08f, 0.09f, 0.11f, 0.95f); // #14171C
            }

            var outline = GetComponent<Outline>();
            if (outline == null) outline = gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.16f, 0.17f, 0.20f, 1f); // #282C34
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            if (logScrollRect == null)
            {
                logScrollRect = GetComponentInChildren<ScrollRect>(true) ?? GetComponentInParent<ScrollRect>();
            }

            // Ensure ConsoleHeader exists and is positioned at top
            Transform headerTr = transform.Find("ConsoleHeader");
            if (headerTr == null)
            {
                var hObj = new GameObject("ConsoleHeader", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                hObj.transform.SetParent(transform, false);
                hObj.transform.SetSiblingIndex(0);
                headerTr = hObj.transform;
            }

            var headerRt = headerTr.GetComponent<RectTransform>();
            if (headerRt != null)
            {
                headerRt.anchorMin = new Vector2(0.02f, 1f);
                headerRt.anchorMax = new Vector2(0.98f, 1f);
                headerRt.pivot = new Vector2(0.5f, 1f);
                headerRt.anchoredPosition = new Vector2(0f, -8f);
                headerRt.sizeDelta = new Vector2(0f, 22f);
            }

            var headerText = headerTr.GetComponent<TextMeshProUGUI>();
            if (headerText != null)
            {
                if (string.IsNullOrEmpty(headerText.text) || headerText.text.Trim() == "")
                {
                    headerText.text = "Battle Console Output  <size=80%><color=#808080>[Debug.Log Engine]</color></size>";
                }
                headerText.fontSize = 14.5f;
                headerText.alignment = TextAlignmentOptions.MidlineLeft;
                headerText.raycastTarget = false;
            }

            // Create or configure dedicated Viewport positioned with a tight 5px gap below ConsoleHeader
            RectTransform viewportRt = transform.Find("ConsoleViewport") as RectTransform;
            if (viewportRt == null)
            {
                var vObj = new GameObject("ConsoleViewport", typeof(RectTransform), typeof(RectMask2D));
                vObj.transform.SetParent(transform, false);
                viewportRt = vObj.GetComponent<RectTransform>();
            }
            else if (viewportRt.GetComponent<RectMask2D>() == null)
            {
                viewportRt.gameObject.AddComponent<RectMask2D>();
            }

            viewportRt.anchorMin = new Vector2(0.02f, 0.03f);
            viewportRt.anchorMax = new Vector2(0.98f, 1f);
            viewportRt.pivot = new Vector2(0.5f, 1f);
            viewportRt.offsetMin = new Vector2(0f, 10f);
            viewportRt.offsetMax = new Vector2(0f, -38f);

            if (logScrollRect != null)
            {
                logScrollRect.viewport = viewportRt;
                logScrollRect.horizontal = false;
                logScrollRect.vertical = true;
                logScrollRect.movementType = ScrollRect.MovementType.Clamped;
            }

            if (logTextDisplay == null)
            {
                logTextDisplay = GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (logTextDisplay != null)
            {
                if (logTextDisplay.transform.parent != viewportRt)
                {
                    logTextDisplay.transform.SetParent(viewportRt, false);
                }

                // Respect user inspector settings; only set fallback if uninitialized
                if (logTextDisplay.fontSize <= 0f) logTextDisplay.fontSize = 15.5f;
                if (logTextDisplay.lineSpacing == 0f) logTextDisplay.lineSpacing = 4f;

                var rt = logTextDisplay.rectTransform;
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.anchoredPosition = new Vector2(0f, -4f);
                rt.sizeDelta = Vector2.zero;

                logTextDisplay.margin = new Vector4(6f, 8f, 6f, 6f);

                var csf = logTextDisplay.GetComponent<ContentSizeFitter>();
                if (csf == null) csf = logTextDisplay.gameObject.AddComponent<ContentSizeFitter>();
                csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                if (logScrollRect != null)
                {
                    logScrollRect.content = rt;
                }
            }
        }

        public static void Log(string message)
        {
            Debug.Log(message);
            if (instance == null) instance = Object.FindFirstObjectByType<ConsoleLogUI>();
            if (instance == null) return;
            instance.AppendMessage(message);
        }

        private void AppendMessage(string message)
        {
            string trimmedHistory = logHistory.ToString().TrimEnd();
            string trimmedMsg = message.TrimEnd();
            if (!string.IsNullOrEmpty(trimmedHistory) && (trimmedHistory.EndsWith(trimmedMsg) || trimmedHistory.Equals(trimmedMsg)))
            {
                if (logTextDisplay != null) logTextDisplay.text = logHistory.ToString();
                return;
            }

            if (currentLineCount >= MaxLogLines)
            {
                string currentText = logHistory.ToString();
                int newlineIdx = currentText.IndexOf('\n');
                if (newlineIdx >= 0)
                {
                    logHistory.Remove(0, newlineIdx + 1);
                    currentLineCount--;
                }
                else
                {
                    logHistory.Clear();
                    currentLineCount = 0;
                }
            }

            logHistory.AppendLine(message);
            currentLineCount++;
            if (logTextDisplay != null)
            {
                logTextDisplay.text = logHistory.ToString();
            }

            if (logScrollRect != null)
            {
                if (currentLineCount <= 6)
                {
                    if (scrollCoroutine != null) StopCoroutine(scrollCoroutine);
                    logScrollRect.verticalNormalizedPosition = 1f;
                }
                else if (gameObject.activeInHierarchy && Application.isPlaying)
                {
                    if (scrollCoroutine != null) StopCoroutine(scrollCoroutine);
                    scrollCoroutine = StartCoroutine(ScrollToBottomCoroutine());
                }
                else
                {
                    logScrollRect.verticalNormalizedPosition = 0f;
                }
            }
        }

        public void Clear()
        {
            logHistory.Clear();
            currentLineCount = 0;
            if (logTextDisplay != null) logTextDisplay.text = "";
            if (logScrollRect != null) logScrollRect.verticalNormalizedPosition = 1f;
        }

        public void SetText(string text)
        {
            logHistory.Clear();
            if (!string.IsNullOrEmpty(text))
            {
                logHistory.Append(text);
                if (!text.EndsWith("\n")) logHistory.AppendLine();
                currentLineCount = text.Split('\n').Length;
            }
            else
            {
                currentLineCount = 0;
            }
            if (logTextDisplay != null) logTextDisplay.text = logHistory.ToString();
            if (logScrollRect != null) logScrollRect.verticalNormalizedPosition = (currentLineCount <= 6) ? 1f : 0f;
        }

#if UNITY_EDITOR
        [ContextMenu("Bake Console Layout To Scene")]
        public void BakeConsoleLayoutToScene()
        {
            SetupConsoleHierarchy();
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.EditorUtility.SetDirty(gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(gameObject.scene);
            Debug.Log("[ConsoleLogUI] Successfully baked console layout into the scene!");
        }
#endif

        private IEnumerator ScrollToBottomCoroutine()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            if (logScrollRect != null)
            {
                logScrollRect.verticalNormalizedPosition = (currentLineCount <= 6) ? 1f : 0f;
            }
            scrollCoroutine = null;
        }
    }
}
