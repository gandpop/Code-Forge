using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace CodeForge.UI
{
    [ExecuteAlways]
    public class EditorLineRowUI : MonoBehaviour, IDropHandler
    {
        public int lineNumber = 1;
        [SerializeField] private TextMeshProUGUI lineNumberText;
        [SerializeField] private Image lineHighlightImage;

        private void Awake()
        {
            FormatRow();
        }

        public void Initialize(int line, TextMeshProUGUI num, Image highlight)
        {
            lineNumber = line;
            lineNumberText = num;
            lineHighlightImage = highlight;

            if (lineNumberText != null)
            {
                lineNumberText.text = lineNumber.ToString("D2");
            }

            SetHighlight(false);
            FormatRow();
        }

        // Backward compatibility overload
        public void Initialize(int line, Button btn, TextMeshProUGUI dot, TextMeshProUGUI num, Image highlight)
        {
            Initialize(line, num, highlight);
        }

        private void Start()
        {
            if (lineNumberText != null)
            {
                lineNumberText.text = lineNumber.ToString("D2");
            }

            SetHighlight(false);
            FormatRow();
        }

        public void SetHighlight(bool active)
        {
            if (lineHighlightImage != null)
            {
                lineHighlightImage.color = active ? new Color(1f, 0.9f, 0.2f, 0.25f) : Color.clear;
            }
        }

        public void FormatRow()
        {
            var layout = GetComponent<HorizontalLayoutGroup>();
            if (layout != null)
            {
                layout.spacing = 3f;
                layout.childControlWidth = false;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.padding = new RectOffset(0, 0, 0, 0);
            }

            Transform hlTr = transform.Find("LineHighlight");
            if (hlTr != null)
            {
                var le = hlTr.GetComponent<LayoutElement>() ?? hlTr.gameObject.AddComponent<LayoutElement>();
                le.ignoreLayout = true;
            }
            if (lineHighlightImage != null)
            {
                var le = lineHighlightImage.GetComponent<LayoutElement>() ?? lineHighlightImage.gameObject.AddComponent<LayoutElement>();
                le.ignoreLayout = true;
            }

            Transform numContainer = transform.Find("LineNumberContainer");
            if (numContainer != null)
            {
                var rt = numContainer.GetComponent<RectTransform>();
                if (rt != null) rt.sizeDelta = new Vector2(36f, 20f);
                var le = numContainer.GetComponent<LayoutElement>();
                if (le != null)
                {
                    le.minWidth = 36f;
                    le.preferredWidth = 36f;
                    le.minHeight = 20f;
                    le.preferredHeight = 20f;
                }
            }

            var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var tmp in texts)
            {
                if (tmp == lineNumberText) continue;
                if (tmp.GetComponentInParent<CodeSocketUI>() != null) continue;

                tmp.text = NormalizeCodeSpacing(tmp.text);
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
                tmp.overflowMode = TextOverflowModes.Overflow;
                tmp.margin = Vector4.zero;
                tmp.ForceMeshUpdate();

                float textWidth = tmp.preferredWidth;
                var rt = tmp.rectTransform;
                rt.sizeDelta = new Vector2(textWidth, 20f);

                var fitter = tmp.GetComponent<ContentSizeFitter>() ?? tmp.gameObject.AddComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

                var le = tmp.GetComponent<LayoutElement>();
                if (le != null)
                {
                    le.minWidth = textWidth;
                    le.preferredWidth = textWidth;
                    le.minHeight = 20f;
                    le.preferredHeight = 20f;
                }
            }

            var sockets = GetComponentsInChildren<CodeSocketUI>(true);
            foreach (var socket in sockets)
            {
                socket.UpdateSocketWidth();
            }

            var rowRt = GetComponent<RectTransform>();
            if (rowRt != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rowRt);
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            var draggedCard = eventData.pointerDrag?.GetComponent<DraggableTokenCardUI>();
            if (draggedCard == null || draggedCard.Token == null) return;

            var sockets = GetComponentsInChildren<CodeSocketUI>();
            if (sockets == null || sockets.Length == 0) return;

            // If single socket on line, forward directly
            if (sockets.Length == 1)
            {
                sockets[0].OnDrop(eventData);
                return;
            }

            // If multiple sockets, find first empty compatible socket
            foreach (var s in sockets)
            {
                if (s.AssignedToken == null && CodeSocketUI.IsTypeCompatible(draggedCard.Token.tokenType, s.expectedType))
                {
                    s.OnDrop(eventData);
                    return;
                }
            }

            // Fallback: first compatible socket
            foreach (var s in sockets)
            {
                if (CodeSocketUI.IsTypeCompatible(draggedCard.Token.tokenType, s.expectedType))
                {
                    s.OnDrop(eventData);
                    return;
                }
            }

            // Fallback: let first socket handle it (reports CS0029 error)
            sockets[0].OnDrop(eventData);
        }

        public static string NormalizeCodeSpacing(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            int leadingCount = 0;
            while (leadingCount < text.Length && text[leadingCount] == ' ') leadingCount++;
            string indent = text.Substring(0, leadingCount);
            string content = text.Substring(leadingCount);
            content = System.Text.RegularExpressions.Regex.Replace(content, @" {2,}", " ");
            return indent + content;
        }
    }
}
