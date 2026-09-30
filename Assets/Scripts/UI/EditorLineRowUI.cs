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

        [Header("Code Folding")]
        [SerializeField] private Button foldToggleButton;
        [SerializeField] private TextMeshProUGUI foldToggleText;
        [SerializeField] private System.Collections.Generic.List<EditorLineRowUI> childRows = new System.Collections.Generic.List<EditorLineRowUI>();
        public bool isFolded = false;

        public System.Collections.Generic.List<EditorLineRowUI> ChildRows => childRows;
        public Button FoldToggleButton => foldToggleButton;
        public TextMeshProUGUI FoldToggleText => foldToggleText;

        public void SetupFoldToggle(System.Collections.Generic.List<EditorLineRowUI> children)
        {
            childRows = children ?? new System.Collections.Generic.List<EditorLineRowUI>();
            EnsureFoldButton();
            UpdateFoldDisplay();
        }

        public void EnsureFoldButton()
        {
            if (foldToggleButton == null)
            {
                var foldObj = transform.Find("FoldToggleBtn")?.gameObject;
                if (foldObj == null)
                {
                    foldObj = new GameObject("FoldToggleBtn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
                    foldObj.transform.SetParent(transform, false);
                    foldObj.transform.SetSiblingIndex(2);

                    var rt = foldObj.GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(18f, 18f);

                    var le = foldObj.GetComponent<LayoutElement>();
                    le.minWidth = 18f;
                    le.preferredWidth = 18f;
                    le.minHeight = 18f;
                    le.preferredHeight = 18f;

                    var img = foldObj.GetComponent<Image>();
                    img.color = new Color(0.18f, 0.20f, 0.25f, 0.7f);

                    var txtObj = new GameObject("Txt", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                    txtObj.transform.SetParent(foldObj.transform, false);
                    var trt = txtObj.GetComponent<RectTransform>();
                    trt.anchorMin = Vector2.zero;
                    trt.anchorMax = Vector2.one;
                    trt.sizeDelta = Vector2.zero;

                    foldToggleText = txtObj.GetComponent<TextMeshProUGUI>();
                    foldToggleText.alignment = TextAlignmentOptions.Center;
                    foldToggleText.fontSize = 11f;
                    foldToggleText.fontStyle = FontStyles.Bold;
                    foldToggleText.color = new Color(0.7f, 0.75f, 0.85f, 1f);
                    foldToggleText.raycastTarget = false;
                    foldToggleText.text = "[-]";

                    foldToggleButton = foldObj.GetComponent<Button>();
                }
                else
                {
                    foldToggleButton = foldObj.GetComponent<Button>();
                    foldToggleText = foldObj.GetComponentInChildren<TextMeshProUGUI>(true);
                }
            }

            if (foldToggleButton != null)
            {
                foldToggleButton.onClick.RemoveAllListeners();
                foldToggleButton.onClick.AddListener(ToggleFold);
            }
        }

        public void ToggleFold()
        {
            SetFolded(!isFolded);
        }

        public void SetFolded(bool fold)
        {
            isFolded = fold;
            if (childRows != null)
            {
                foreach (var row in childRows)
                {
                    if (row != null && row != this)
                    {
                        row.gameObject.SetActive(!isFolded);
                    }
                }
            }
            UpdateFoldDisplay();

            if (transform.parent != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent as RectTransform);
            }
        }

        private void UpdateFoldDisplay()
        {
            if (foldToggleText != null)
            {
                foldToggleText.text = isFolded ? "[+]" : "[-]";
                foldToggleText.color = isFolded ? new Color(1f, 0.85f, 0.4f, 1f) : new Color(0.7f, 0.75f, 0.85f, 1f);
            }
        }

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
                layout.padding = new RectOffset(6, 0, 0, 0);
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

            // Gutter is always Sibling 1 (fixed 28px width)
            Transform gutterTr = transform.Find("Gutter") ?? transform.Find("LineNumberContainer");
            if (gutterTr != null)
            {
                gutterTr.SetSiblingIndex(1);
                var rt = gutterTr.GetComponent<RectTransform>();
                if (rt != null) rt.sizeDelta = new Vector2(28f, 20f);
                var le = gutterTr.GetComponent<LayoutElement>() ?? gutterTr.gameObject.AddComponent<LayoutElement>();
                le.minWidth = 28f;
                le.preferredWidth = 28f;
                le.minHeight = 20f;
                le.preferredHeight = 20f;
            }

            // FoldToggleBtn is always Sibling 2 (fixed 18px width, between Gutter and CodeText)
            Transform foldTr = transform.Find("FoldToggleBtn");
            if (foldTr != null)
            {
                foldTr.SetSiblingIndex(2);
                var rt = foldTr.GetComponent<RectTransform>();
                if (rt != null) rt.sizeDelta = new Vector2(18f, 18f);
                var le = foldTr.GetComponent<LayoutElement>() ?? foldTr.gameObject.AddComponent<LayoutElement>();
                le.minWidth = 18f;
                le.preferredWidth = 18f;
                le.minHeight = 18f;
                le.preferredHeight = 18f;
            }

            var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var tmp in texts)
            {
                if (tmp == lineNumberText) continue;
                if (tmp == foldToggleText) continue;
                if (foldToggleButton != null && tmp.transform.IsChildOf(foldToggleButton.transform)) continue;
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
