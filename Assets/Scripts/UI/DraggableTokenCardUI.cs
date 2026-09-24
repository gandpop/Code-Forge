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
        [SerializeField] private Image rarityBorder;
        [SerializeField] private Outline cardOutline;
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

            string rarityHex = Token.GetRarityHexColor();
            Color rarityColor = Token.GetRarityColor();

            if (cardText != null)
            {
                string typeColor = Token.tokenType switch
                {
                    CodeTokenType.Targeting => "#4EC9B0", // Teal
                    CodeTokenType.Condition => "#DCDCAA", // Yellowish / Expression
                    CodeTokenType.Action => "#C586C0",    // Purple / Action
                    _ => "#D4D4D4"
                };

                string typeName = Token.tokenType switch
                {
                    CodeTokenType.Targeting => "target",
                    CodeTokenType.Condition => "condition",
                    CodeTokenType.Action => "action",
                    _ => "var"
                };

                cardText.text = $"<size=75%><color={rarityHex}><b>[{Token.rarity.ToString().ToUpper()}]</b></color></size> <color={typeColor}><b>{typeName}</b></color>\n<size=110%><b>{Token.GetFormattedCodeString()}</b></size>";
            }

            if (rarityBorder != null)
            {
                rarityBorder.color = rarityColor;
            }

            if (cardOutline == null) cardOutline = GetComponent<Outline>();
            if (cardOutline != null)
            {
                cardOutline.effectColor = new Color(rarityColor.r, rarityColor.g, rarityColor.b, 0.9f);
                cardOutline.effectDistance = new Vector2(2f, -2f);
            }

            if (cardBackground != null)
            {
                // Dark base tinted with subtle rarity tone
                cardBackground.color = new Color(
                    0.12f + rarityColor.r * 0.12f,
                    0.12f + rarityColor.g * 0.12f,
                    0.16f + rarityColor.b * 0.12f,
                    1f
                );
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
