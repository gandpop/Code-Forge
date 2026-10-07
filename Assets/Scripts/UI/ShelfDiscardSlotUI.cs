using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using CodeForge.Data;

namespace CodeForge.UI
{
    public class ShelfDiscardSlotUI : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Outline outline;
        [SerializeField] private TextMeshProUGUI promptText;

        private Color normalBg = new Color(0.14f, 0.10f, 0.12f, 0.95f);
        private Color hoverBg = new Color(0.28f, 0.10f, 0.12f, 0.98f);
        private Color normalOutline = new Color(0.70f, 0.25f, 0.28f, 0.85f);
        private Color hoverOutline = new Color(1f, 0.27f, 0.27f, 1f); // #FF4444

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (backgroundImage == null) backgroundImage = GetComponent<Image>();
            if (outline == null) outline = GetComponent<Outline>();
            if (promptText == null) promptText = GetComponentInChildren<TextMeshProUGUI>(true);

            if (backgroundImage != null && backgroundImage.color == default)
            {
                backgroundImage.color = normalBg;
            }

            if (outline != null && outline.effectColor == default)
            {
                outline.effectColor = normalOutline;
                outline.effectDistance = new Vector2(2f, -2f);
            }

            if (promptText != null)
            {
                if (string.IsNullOrEmpty(promptText.text))
                {
                    promptText.text = "<b>DISCARD</b>\n<color=#E06C75><b>[ X ]</b></color>\n<size=75%><color=#858585>Drop token here</color></size>";
                }

                if (promptText.color == default)
                {
                    promptText.color = new Color(0.95f, 0.70f, 0.70f, 1f);
                }

                // Ensure rich text is enabled so tags render nicely
                promptText.richText = true;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (eventData.dragging)
            {
                if (backgroundImage != null) backgroundImage.color = hoverBg;
                if (outline != null) outline.effectColor = hoverOutline;
                if (promptText != null) promptText.color = Color.white;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (backgroundImage != null) backgroundImage.color = normalBg;
            if (outline != null) outline.effectColor = normalOutline;
            if (promptText != null) promptText.color = new Color(0.95f, 0.70f, 0.70f, 1f);
        }

        public void OnDrop(PointerEventData eventData)
        {
            var draggedCard = eventData.pointerDrag?.GetComponent<DraggableTokenCardUI>();
            if (draggedCard == null || draggedCard.Token == null) return;

            var token = draggedCard.Token;
            string tokenName = !string.IsNullOrEmpty(token.tokenName) ? token.tokenName : "Token";

            OnPointerExit(eventData);

            var canvas = GetComponentInParent<Canvas>();
            var modal = DiscardConfirmationModalUI.GetOrCreate(canvas);
            if (modal != null)
            {
                // Temporarily deactivate card during prompt
                draggedCard.gameObject.SetActive(false);

                modal.PromptDiscard(
                    token,
                    onConfirm: () =>
                    {
                        var editorUI = FindFirstObjectByType<CodeEditorPanelUI>(FindObjectsInactive.Include);
                        if (editorUI != null)
                        {
                            editorUI.RemoveTokenFromInventory(token);
                        }

                        Destroy(draggedCard.gameObject);
                        ConsoleLogUI.Log($"<color=#E06C75>[Inventory] Discarded token: {tokenName}</color>");
                    },
                    onCancel: () =>
                    {
                        // Restore card to shelf
                        if (draggedCard != null)
                        {
                            draggedCard.gameObject.SetActive(true);
                            // Ensure card returns to shelf
                            draggedCard.OnEndDrag(eventData);
                        }
                    }
                );
            }
            else
            {
                // Fallback if modal cannot be instantiated
                var editorUI = FindFirstObjectByType<CodeEditorPanelUI>(FindObjectsInactive.Include);
                if (editorUI != null)
                {
                    editorUI.RemoveTokenFromInventory(token);
                }

                Destroy(draggedCard.gameObject);
                ConsoleLogUI.Log($"<color=#E06C75>[Inventory] Discarded token: {tokenName}</color>");
            }
        }
    }
}
