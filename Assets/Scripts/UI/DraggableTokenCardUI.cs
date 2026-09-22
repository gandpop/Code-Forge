using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using CodeForge.Data;

namespace CodeForge.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class DraggableTokenCardUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public CodeTokenSO Token { get; private set; }

        [SerializeField] private TextMeshProUGUI cardText;
        [SerializeField] private Image cardBackground;
        [SerializeField] private CanvasGroup canvasGroup;

        private Transform originalParent;
        private int originalSiblingIndex;
        private Canvas rootCanvas;
        private bool dropAccepted = false;

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            rootCanvas = GetComponentInParent<Canvas>();
        }

        public void BindToken(CodeTokenSO token)
        {
            Token = token;
            dropAccepted = false;
            RefreshCardVisual();
        }

        public void RefreshCardVisual()
        {
            if (Token == null) return;

            if (cardText != null)
            {
                string typeColor = Token.tokenType switch
                {
                    CodeTokenType.Float => "#569CD6",
                    CodeTokenType.Int => "#569CD6",
                    CodeTokenType.Bool => "#569CD6",
                    CodeTokenType.TargetPriority => "#4EC9B0",
                    _ => "#D4D4D4"
                };

                string typeName = Token.tokenType switch
                {
                    CodeTokenType.Float => "float",
                    CodeTokenType.Int => "int",
                    CodeTokenType.Bool => "bool",
                    CodeTokenType.TargetPriority => "enum",
                    _ => "var"
                };

                cardText.text = $"<color={typeColor}><b>{typeName}</b></color>\n<size=115%>{Token.GetFormattedCodeString()}</size>";
            }

            if (cardBackground != null)
            {
                cardBackground.color = Token.tokenType switch
                {
                    CodeTokenType.Float => new Color(0.16f, 0.22f, 0.28f, 1f),
                    CodeTokenType.Int => new Color(0.18f, 0.20f, 0.28f, 1f),
                    CodeTokenType.Bool => new Color(0.24f, 0.18f, 0.28f, 1f),
                    CodeTokenType.TargetPriority => new Color(0.16f, 0.26f, 0.24f, 1f),
                    _ => new Color(0.20f, 0.20f, 0.25f, 1f)
                };
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            dropAccepted = false;
            originalParent = transform.parent;
            originalSiblingIndex = transform.GetSiblingIndex();

            if (rootCanvas == null) rootCanvas = GetComponentInParent<Canvas>();

            // Reparent to root canvas so card floats above all panels and escapes RectMask2D
            if (rootCanvas != null)
            {
                transform.SetParent(rootCanvas.transform, true);
                transform.SetAsLastSibling();
            }

            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.85f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            transform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1.0f;

            if (dropAccepted)
            {
                // Card consumed by socket, destroy this inventory UI card
                Destroy(gameObject);
            }
            else
            {
                // Returned to inventory shelf
                if (originalParent != null)
                {
                    transform.SetParent(originalParent, false);
                    transform.SetSiblingIndex(originalSiblingIndex);
                }
            }
        }

        public void NotifyDropAccepted()
        {
            dropAccepted = true;
        }
    }
}
