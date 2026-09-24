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
        [Header("Section 1: Class Fields (Unlocked Room 2+)")]
        [SerializeField] private GameObject section1FieldsGroup;
        [SerializeField] private GameObject section1LockedBanner;
        [SerializeField] private CodeSocketUI damageMultiplierSocket;
        [SerializeField] private CodeSocketUI baseShieldSocket;

        [Header("Section 2: ExecuteTurn Method (Unlocked Room 1+)")]
        [SerializeField] private GameObject section2ExecuteTurnGroup;
        [SerializeField] private CodeSocketUI targetSocket;
        [SerializeField] private CodeSocketUI conditionSocket;
        [SerializeField] private CodeSocketUI thenActionSocket;
        [SerializeField] private CodeSocketUI elseActionSocket;

        [Header("Section 3: OnTakeDamage Callback (Unlocked Room 3+)")]
        [SerializeField] private GameObject section3OnTakeDamageGroup;
        [SerializeField] private GameObject section3LockedBanner;
        [SerializeField] private CodeSocketUI reactionConditionSocket;
        [SerializeField] private CodeSocketUI reactionActionSocket;

        [Header("Legacy Fallbacks (Preserves Scene References)")]
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

        public bool IsSection1Unlocked { get; private set; } = false;
        public bool IsSection3Unlocked { get; private set; } = false;

        public CodeSocketUI TargetSocketUI => targetSocket != null ? targetSocket : targetingSocket;
        public CodeSocketUI ConditionSocketUI => conditionSocket != null ? conditionSocket : piercingSocket;
        public CodeSocketUI ThenActionSocketUI => thenActionSocket != null ? thenActionSocket : damageSocket;
        public CodeSocketUI ElseActionSocketUI => elseActionSocket != null ? elseActionSocket : multiplierSocket;
        public CodeSocketUI DamageMultiplierSocketUI => damageMultiplierSocket;
        public CodeSocketUI BaseShieldSocketUI => baseShieldSocket;
        public CodeSocketUI ReactionConditionSocketUI => reactionConditionSocket;
        public CodeSocketUI ReactionActionSocketUI => reactionActionSocket;

        private void Awake()
        {
            if (targetSocket == null) targetSocket = targetingSocket;
            if (conditionSocket == null) conditionSocket = piercingSocket;
            if (thenActionSocket == null) thenActionSocket = damageSocket;
            if (elseActionSocket == null) elseActionSocket = multiplierSocket;

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
            RegisterSocket(TargetSocketUI);
            RegisterSocket(ConditionSocketUI);
            RegisterSocket(ThenActionSocketUI);
            RegisterSocket(ElseActionSocketUI);
            RegisterSocket(damageMultiplierSocket);
            RegisterSocket(baseShieldSocket);
            RegisterSocket(reactionConditionSocket);
            RegisterSocket(reactionActionSocket);
        }

        private void RegisterSocket(CodeSocketUI socket)
        {
            if (socket != null)
            {
                socket.SetEditorReference(this);
            }
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

        public void SetProgressionState(int roomIndex)
        {
            IsSection1Unlocked = roomIndex >= 2;
            IsSection3Unlocked = roomIndex >= 3;

            // Section 1: Class Fields
            if (section1FieldsGroup != null)
            {
                section1FieldsGroup.SetActive(IsSection1Unlocked);
            }
            if (section1LockedBanner != null)
            {
                section1LockedBanner.SetActive(!IsSection1Unlocked);
            }

            // Section 2: ExecuteTurn (Always active)
            if (section2ExecuteTurnGroup != null)
            {
                section2ExecuteTurnGroup.SetActive(true);
            }

            // Section 3: OnTakeDamage Callback
            if (section3OnTakeDamageGroup != null)
            {
                section3OnTakeDamageGroup.SetActive(IsSection3Unlocked);
            }
            if (section3LockedBanner != null)
            {
                section3LockedBanner.SetActive(!IsSection3Unlocked);
            }
        }

        public void SetInteractionLocked(bool locked)
        {
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.interactable = !locked;
                panelCanvasGroup.alpha = locked ? 0.75f : 1.0f;
            }
        }

        public void ResetAllHighlights()
        {
            ResetSocketHighlight(TargetSocketUI);
            ResetSocketHighlight(ConditionSocketUI);
            ResetSocketHighlight(ThenActionSocketUI);
            ResetSocketHighlight(ElseActionSocketUI);
            ResetSocketHighlight(damageMultiplierSocket);
            ResetSocketHighlight(baseShieldSocket);
            ResetSocketHighlight(reactionConditionSocket);
            ResetSocketHighlight(reactionActionSocket);
        }

        private void ResetSocketHighlight(CodeSocketUI socket)
        {
            if (socket != null)
            {
                socket.SetHighlight(Color.white, false, 1.0f);
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
                case CodeTokenType.Targeting:
                    if (TargetSocketUI != null && TargetSocketUI.AssignedToken == null)
                        TargetSocketUI.AssignToken(token);
                    break;

                case CodeTokenType.Condition:
                    if (token is ConditionTokenSO condToken && condToken.subject == ConditionSubject.IncomingDamage)
                    {
                        if (IsSection3Unlocked && reactionConditionSocket != null && reactionConditionSocket.AssignedToken == null)
                            reactionConditionSocket.AssignToken(token);
                        else if (ConditionSocketUI != null && ConditionSocketUI.AssignedToken == null)
                            ConditionSocketUI.AssignToken(token);
                    }
                    else
                    {
                        if (ConditionSocketUI != null && ConditionSocketUI.AssignedToken == null)
                            ConditionSocketUI.AssignToken(token);
                        else if (IsSection3Unlocked && reactionConditionSocket != null && reactionConditionSocket.AssignedToken == null)
                            reactionConditionSocket.AssignToken(token);
                    }
                    break;

                case CodeTokenType.Action:
                    if (ThenActionSocketUI != null && ThenActionSocketUI.AssignedToken == null)
                        ThenActionSocketUI.AssignToken(token);
                    else if (ElseActionSocketUI != null && ElseActionSocketUI.AssignedToken == null)
                        ElseActionSocketUI.AssignToken(token);
                    else if (IsSection3Unlocked && reactionActionSocket != null && reactionActionSocket.AssignedToken == null)
                        reactionActionSocket.AssignToken(token);
                    break;

                case CodeTokenType.Float:
                    if (IsSection1Unlocked && damageMultiplierSocket != null && damageMultiplierSocket.AssignedToken == null)
                        damageMultiplierSocket.AssignToken(token);
                    break;

                case CodeTokenType.Int:
                    if (IsSection1Unlocked && baseShieldSocket != null && baseShieldSocket.AssignedToken == null)
                        baseShieldSocket.AssignToken(token);
                    break;
            }
        }

        public float GetDamageMultiplier()
        {
            if (IsSection1Unlocked && damageMultiplierSocket != null && damageMultiplierSocket.AssignedToken != null)
            {
                return damageMultiplierSocket.AssignedToken.floatValue > 0f ? damageMultiplierSocket.AssignedToken.floatValue : 1.0f;
            }
            return 1.0f;
        }

        public int GetBaseShield()
        {
            if (IsSection1Unlocked && baseShieldSocket != null && baseShieldSocket.AssignedToken != null)
            {
                return Mathf.Max(0, baseShieldSocket.AssignedToken.intValue);
            }
            return 0;
        }

        public TargetingTokenSO GetTargetingToken()
        {
            var socket = TargetSocketUI;
            return socket != null ? socket.AssignedToken as TargetingTokenSO : null;
        }

        public ConditionTokenSO GetConditionToken()
        {
            var socket = ConditionSocketUI;
            return socket != null ? socket.AssignedToken as ConditionTokenSO : null;
        }

        public ActionTokenSO GetThenActionToken()
        {
            var socket = ThenActionSocketUI;
            return socket != null ? socket.AssignedToken as ActionTokenSO : null;
        }

        public ActionTokenSO GetElseActionToken()
        {
            var socket = ElseActionSocketUI;
            return socket != null ? socket.AssignedToken as ActionTokenSO : null;
        }

        public ConditionTokenSO GetReactionConditionToken()
        {
            if (!IsSection3Unlocked || reactionConditionSocket == null) return null;
            return reactionConditionSocket.AssignedToken as ConditionTokenSO;
        }

        public ActionTokenSO GetReactionActionToken()
        {
            if (!IsSection3Unlocked || reactionActionSocket == null) return null;
            return reactionActionSocket.AssignedToken as ActionTokenSO;
        }
    }
}
