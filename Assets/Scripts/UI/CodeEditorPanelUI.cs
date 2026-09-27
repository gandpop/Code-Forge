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
        [Header("Class Fields Sockets")]
        [SerializeField] private CodeSocketUI maxHealthSocket;
        [SerializeField] private CodeSocketUI damageMultiplierSocket;
        [SerializeField] private CodeSocketUI baseShieldSocket;

        [Header("Modular Combat Methods Sockets")]
        [SerializeField] private CodeSocketUI attackDamageSocket;
        [SerializeField] private CodeSocketUI defendShieldSocket;

        [Header("void Start() Sockets")]
        [SerializeField] private CodeSocketUI stanceSocket;

        [Header("ExecuteTurn Method Sockets")]
        [SerializeField] private CodeSocketUI targetSocket;
        [SerializeField] private CodeSocketUI conditionSocket;
        [SerializeField] private CodeSocketUI thenActionSocket;
        [SerializeField] private CodeSocketUI elseActionSocket;

        [Header("OnTakeDamage Callback Sockets")]
        [SerializeField] private CodeSocketUI reactionConditionSocket;
        [SerializeField] private CodeSocketUI reactionActionSocket;

        [Header("Default Pre-Slotted Tokens (Anti-Paralysis)")]
        [SerializeField] private CodeTokenSO defaultMaxHealth;
        [SerializeField] private CodeTokenSO defaultDamageMultiplier;
        [SerializeField] private CodeTokenSO defaultBaseShield;
        [SerializeField] private CodeTokenSO defaultAttackDamage;
        [SerializeField] private CodeTokenSO defaultDefendShield;
        [SerializeField] private StanceTokenSO defaultStance;
        [SerializeField] private TargetingTokenSO defaultTarget;
        [SerializeField] private ConditionTokenSO defaultCondition;
        [SerializeField] private ActionTokenSO defaultThenAction;
        [SerializeField] private ActionTokenSO defaultElseAction;
        [SerializeField] private ConditionTokenSO defaultReactionCondition;
        [SerializeField] private ActionTokenSO defaultReactionAction;

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

        private Dictionary<int, EditorLineRowUI> lineRows = new Dictionary<int, EditorLineRowUI>();

        public CodeSocketUI TargetSocketUI => targetSocket != null ? targetSocket : targetingSocket;
        public CodeSocketUI ConditionSocketUI => conditionSocket != null ? conditionSocket : piercingSocket;
        public CodeSocketUI ThenActionSocketUI => thenActionSocket != null ? thenActionSocket : damageSocket;
        public CodeSocketUI ElseActionSocketUI => elseActionSocket != null ? elseActionSocket : multiplierSocket;
        public CodeSocketUI MaxHealthSocketUI => maxHealthSocket;
        public CodeSocketUI DamageMultiplierSocketUI => damageMultiplierSocket;
        public CodeSocketUI BaseShieldSocketUI => baseShieldSocket;
        public CodeSocketUI AttackDamageSocketUI => attackDamageSocket;
        public CodeSocketUI DefendShieldSocketUI => defendShieldSocket;
        public CodeSocketUI StanceSocketUI => stanceSocket;
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
                compileAndRunButton.onClick.RemoveAllListeners();
                compileAndRunButton.onClick.AddListener(() =>
                {
                    if (!ValidatePreBattle()) return;

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
            RegisterSocket(maxHealthSocket);
            RegisterSocket(damageMultiplierSocket);
            RegisterSocket(baseShieldSocket);
            RegisterSocket(attackDamageSocket);
            RegisterSocket(defendShieldSocket);
            RegisterSocket(stanceSocket);
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
            PrePopulateDefaultTokens();

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

            // Register tokens with IntelliSense
            if (IntelliSensePopoverUI.Instance != null)
            {
                if (starterTokens != null) IntelliSensePopoverUI.Instance.RegisterAvailableTokens(starterTokens);
                if (defaultMaxHealth != null) IntelliSensePopoverUI.Instance.RegisterAvailableTokens(new CodeTokenSO[] { defaultMaxHealth });
            }
        }

        public void PrePopulateDefaultTokens()
        {
            SlotDefaultIfEmpty(MaxHealthSocketUI, defaultMaxHealth);
        }

        private void SlotDefaultIfEmpty(CodeSocketUI socket, CodeTokenSO defaultToken)
        {
            if (socket != null && socket.AssignedToken == null && defaultToken != null)
            {
                socket.AssignToken(defaultToken);
            }
        }

        public void SetInteractionLocked(bool locked)
        {
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.interactable = !locked;
                panelCanvasGroup.blocksRaycasts = !locked;
                panelCanvasGroup.alpha = locked ? 0.85f : 1.0f;
            }
        }

        private void CacheLineRows()
        {
            lineRows.Clear();
            var rows = GetComponentsInChildren<EditorLineRowUI>(true);
            foreach (var r in rows)
            {
                if (r != null) lineRows[r.lineNumber] = r;
            }
        }

        public void HighlightLine(int line, bool active = true)
        {
            if (lineRows.Count == 0) CacheLineRows();
            if (lineRows.TryGetValue(line, out var row) && row != null)
            {
                row.SetHighlight(active);
            }
        }

        public void ClearAllLineHighlights()
        {
            if (lineRows.Count == 0) CacheLineRows();
            foreach (var kvp in lineRows)
            {
                if (kvp.Value != null) kvp.Value.SetHighlight(false);
            }
        }

        public void ResetAllHighlights()
        {
            ClearAllLineHighlights();
            ResetSocketHighlight(TargetSocketUI);
            ResetSocketHighlight(ConditionSocketUI);
            ResetSocketHighlight(ThenActionSocketUI);
            ResetSocketHighlight(ElseActionSocketUI);
            ResetSocketHighlight(maxHealthSocket);
            ResetSocketHighlight(damageMultiplierSocket);
            ResetSocketHighlight(baseShieldSocket);
            ResetSocketHighlight(stanceSocket);
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

            if (IntelliSensePopoverUI.Instance != null)
            {
                IntelliSensePopoverUI.Instance.RegisterAvailableTokens(new CodeTokenSO[] { token });
            }
        }

        public bool RemoveTokenFromInventory(CodeTokenSO token)
        {
            if (token == null || tokenInventoryContainer == null) return false;

            for (int i = 0; i < tokenInventoryContainer.childCount; i++)
            {
                var child = tokenInventoryContainer.GetChild(i);
                var card = child.GetComponent<DraggableTokenCardUI>();
                if (card != null && card.Token == token)
                {
                    child.SetParent(null);
                    Destroy(child.gameObject);
                    return true;
                }
            }
            return false;
        }

        public List<CodeTokenSO> GetInventoryTokens()
        {
            var list = new List<CodeTokenSO>();
            if (tokenInventoryContainer == null) return list;

            for (int i = 0; i < tokenInventoryContainer.childCount; i++)
            {
                var card = tokenInventoryContainer.GetChild(i).GetComponent<DraggableTokenCardUI>();
                if (card != null && card.Token != null)
                {
                    list.Add(card.Token);
                }
            }
            return list;
        }

        public void AutoAssignToken(CodeTokenSO token)
        {
            if (token == null) return;

            switch (token.tokenType)
            {
                case CodeTokenType.Targeting:
                    if (TargetSocketUI != null)
                        TargetSocketUI.AssignToken(token);
                    break;

                case CodeTokenType.Condition:
                    if (token is ConditionTokenSO condToken && condToken.subject == ConditionSubject.IncomingDamage)
                    {
                        if (reactionConditionSocket != null)
                            reactionConditionSocket.AssignToken(token);
                    }
                    else
                    {
                        if (ConditionSocketUI != null)
                            ConditionSocketUI.AssignToken(token);
                    }
                    break;

                case CodeTokenType.Action:
                    if (ThenActionSocketUI != null && ThenActionSocketUI.AssignedToken == null)
                        ThenActionSocketUI.AssignToken(token);
                    else if (ElseActionSocketUI != null && ElseActionSocketUI.AssignedToken == null)
                        ElseActionSocketUI.AssignToken(token);
                    else if (reactionActionSocket != null)
                        reactionActionSocket.AssignToken(token);
                    else if (ThenActionSocketUI != null)
                        ThenActionSocketUI.AssignToken(token);
                    break;

                case CodeTokenType.Float:
                    if (damageMultiplierSocket != null)
                        damageMultiplierSocket.AssignToken(token);
                    break;

                case CodeTokenType.Int:
                    if (baseShieldSocket != null)
                        baseShieldSocket.AssignToken(token);
                    break;

                case CodeTokenType.Stance:
                    if (stanceSocket != null)
                        stanceSocket.AssignToken(token);
                    break;
            }
        }

        public int GetMaxHealth()
        {
            if (maxHealthSocket != null && maxHealthSocket.AssignedToken != null)
            {
                return Mathf.Max(5, maxHealthSocket.AssignedToken.intValue);
            }
            return 20;
        }

        public float GetDamageMultiplier()
        {
            if (damageMultiplierSocket != null && damageMultiplierSocket.AssignedToken != null)
            {
                return damageMultiplierSocket.AssignedToken.floatValue > 0f ? damageMultiplierSocket.AssignedToken.floatValue : 1.0f;
            }
            return 1.0f;
        }

        public int GetBaseShield()
        {
            if (baseShieldSocket != null && baseShieldSocket.AssignedToken != null)
            {
                return Mathf.Max(0, baseShieldSocket.AssignedToken.intValue);
            }
            return 0;
        }

        public int GetAttackDamage()
        {
            if (attackDamageSocket != null && attackDamageSocket.AssignedToken != null)
            {
                return Mathf.Max(1, attackDamageSocket.AssignedToken.intValue);
            }
            return 8;
        }

        public int GetDefendShield()
        {
            if (defendShieldSocket != null && defendShieldSocket.AssignedToken != null)
            {
                return Mathf.Max(1, defendShieldSocket.AssignedToken.intValue);
            }
            return 5;
        }

        public bool ValidatePreBattle()
        {
            var requiredSockets = new (CodeSocketUI socket, string name)[]
            {
                (maxHealthSocket, "maxHealth"),
                (attackDamageSocket, "Attack.damage"),
                (thenActionSocket, "ExecuteTurn.thenAction")
            };

            foreach (var req in requiredSockets)
            {
                if (req.socket == null || req.socket.AssignedToken == null)
                {
                    ConsoleLogUI.Log($"<color=#FF5454>[Compiler Error] CS0165: Use of unassigned variable '{req.name}'. Please assign a token before compiling!</color>");
                    if (req.socket != null)
                    {
                        req.socket.TriggerUnassignedPulsingHighlight();
                    }
                    return false;
                }
            }
            return true;
        }

        public StanceTokenSO GetStanceToken()
        {
            return stanceSocket != null ? stanceSocket.AssignedToken as StanceTokenSO : null;
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
            return reactionConditionSocket != null ? reactionConditionSocket.AssignedToken as ConditionTokenSO : null;
        }

        public ActionTokenSO GetReactionActionToken()
        {
            return reactionActionSocket != null ? reactionActionSocket.AssignedToken as ActionTokenSO : null;
        }
    }
}
