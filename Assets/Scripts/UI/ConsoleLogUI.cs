using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CodeForge.UI
{
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

        private void Awake()
        {
            instance = this;

            if (logScrollRect == null)
            {
                logScrollRect = GetComponentInChildren<ScrollRect>(true) ?? GetComponentInParent<ScrollRect>();
            }

            if (logScrollRect != null)
            {
                logScrollRect.viewport = logScrollRect.GetComponent<RectTransform>();
                logScrollRect.horizontal = false;
                logScrollRect.vertical = true;
                logScrollRect.movementType = ScrollRect.MovementType.Clamped;
            }

            if (logTextDisplay != null)
            {
                logTextDisplay.fontSize = 15.5f;
                logTextDisplay.lineSpacing = 4f;

                var rt = logTextDisplay.rectTransform;
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchorMin = new Vector2(0.02f, 1f);
                rt.anchorMax = new Vector2(0.98f, 1f);
                rt.anchoredPosition = new Vector2(0f, -40f);
                rt.sizeDelta = new Vector2(0f, 0f);

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
                if (gameObject.activeInHierarchy)
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

        private IEnumerator ScrollToBottomCoroutine()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            if (logScrollRect != null)
            {
                logScrollRect.verticalNormalizedPosition = 0f;
            }
            scrollCoroutine = null;
        }
    }
}
