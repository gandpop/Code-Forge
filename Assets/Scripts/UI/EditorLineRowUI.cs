using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CodeForge.UI
{
    public class EditorLineRowUI : MonoBehaviour
    {
        public int lineNumber = 1;
        [SerializeField] private TextMeshProUGUI lineNumberText;
        [SerializeField] private Image lineHighlightImage;

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
        }

        public void SetHighlight(bool active)
        {
            if (lineHighlightImage != null)
            {
                lineHighlightImage.color = active ? new Color(1f, 0.9f, 0.2f, 0.25f) : Color.clear;
            }
        }
    }
}
