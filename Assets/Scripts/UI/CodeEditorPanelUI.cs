using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CodeForge.Combat;
using CodeForge.Data;

namespace CodeForge.UI
{
    public class CodeEditorPanelUI : MonoBehaviour
    {
        [Header("Sockets")]
        [SerializeField] private CodeSocketUI attacksSocket;
        [SerializeField] private CodeSocketUI damageSocket;
        [SerializeField] private CodeSocketUI multiplierSocket;
        [SerializeField] private CodeSocketUI piercingSocket;
        [SerializeField] private CodeSocketUI targetingSocket;

        [Header("Controls")]
        [SerializeField] private Button compileAndRunButton;
        [SerializeField] private Transform tokenInventoryContainer;
        [SerializeField] private GameObject inventoryTokenCardPrefab;
        [SerializeField] private CanvasGroup panelCanvasGroup;

        [Header("Starter Loadout")]
        [SerializeField] private List<CodeTokenSO> starterTokens = new List<CodeTokenSO>();

        private void Awake()
        {
            if (compileAndRunButton != null)
            {
                compileAndRunButton.onClick.AddListener(() =>
                {
                    if (CombatManager.Instance != null)
                    {
                        CombatManager.Instance.StartCombatExecution();
                    }
                });
            }

            // Ensure sockets have back-reference to this editor panel
            if (attacksSocket != null) attacksSocket.SetEditorReference(this);
            if (damageSocket != null) damageSocket.SetEditorReference(this);
            if (multiplierSocket != null) multiplierSocket.SetEditorReference(this);
            if (piercingSocket != null) piercingSocket.SetEditorReference(this);
            if (targetingSocket != null) targetingSocket.SetEditorReference(this);
        }

        private void Start()
        {
            if (starterTokens != null)
            {
                foreach (var token in starterTokens)
                {
                    if (token != null)
                    {
                        AddTokenToInventory(token);
                    }
                }
            }
        }

        public void SetInteractionLocked(bool locked)
        {
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.interactable = !locked;
                panelCanvasGroup.alpha = locked ? 0.6f : 1.0f;
            }
        }

        public void AddTokenToInventory(CodeTokenSO token)
        {
            if (token == null || inventoryTokenCardPrefab == null || tokenInventoryContainer == null) return;

            GameObject cardObj = Instantiate(inventoryTokenCardPrefab, tokenInventoryContainer);
            cardObj.name = $"Card_{token.name}";

            var draggable = cardObj.GetComponent<DraggableTokenCardUI>();
            if (draggable != null)
            {
                draggable.BindToken(token);
            }
            else
            {
                // Fallback for simple button/text
                var text = cardObj.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = $"{token.tokenName}\n<size=80%>{token.GetFormattedCodeString()}</size>";
                }
            }
        }

        public void AutoAssignToken(CodeTokenSO token)
        {
            if (token == null) return;
            switch (token.tokenType)
            {
                case CodeTokenType.Float:
                    if (multiplierSocket != null) multiplierSocket.AssignToken(token);
                    break;
                case CodeTokenType.Int:
                    // If attacks socket is default, slot attacks, else damage
                    if (attacksSocket != null && attacksSocket.AssignedToken == null)
                        attacksSocket.AssignToken(token);
                    else if (damageSocket != null)
                        damageSocket.AssignToken(token);
                    break;
                case CodeTokenType.Bool:
                    if (piercingSocket != null) piercingSocket.AssignToken(token);
                    break;
                case CodeTokenType.TargetPriority:
                    if (targetingSocket != null) targetingSocket.AssignToken(token);
                    break;
            }
        }

        public int GetSocketAttacksValue(int fallback = 1)
        {
            if (attacksSocket != null && attacksSocket.AssignedToken != null)
                return attacksSocket.AssignedToken.intValue;
            if (attacksSocket != null) return attacksSocket.defaultInt;
            return fallback;
        }

        public int GetSocketDamageValue(int fallback = 10)
        {
            if (damageSocket != null && damageSocket.AssignedToken != null)
                return damageSocket.AssignedToken.intValue;
            if (damageSocket != null) return damageSocket.defaultInt;
            return fallback;
        }

        public float GetSocketMultiplierValue(float fallback = 1.0f)
        {
            if (multiplierSocket != null && multiplierSocket.AssignedToken != null)
                return multiplierSocket.AssignedToken.floatValue;
            if (multiplierSocket != null) return multiplierSocket.defaultFloat;
            return fallback;
        }

        public bool GetSocketBoolValue(CodeSocketRole role, bool fallback = false)
        {
            if (role == CodeSocketRole.Piercing && piercingSocket != null && piercingSocket.AssignedToken != null)
                return piercingSocket.AssignedToken.boolValue;
            if (piercingSocket != null) return piercingSocket.defaultBool;
            return fallback;
        }

        public TargetPriority GetSocketTargetingValue(CodeSocketRole role, TargetPriority fallback = TargetPriority.LowestHealth)
        {
            if (role == CodeSocketRole.Targeting && targetingSocket != null && targetingSocket.AssignedToken != null)
                return targetingSocket.AssignedToken.targetPriorityValue;
            if (targetingSocket != null) return targetingSocket.defaultTargetPriority;
            return fallback;
        }
    }
}
