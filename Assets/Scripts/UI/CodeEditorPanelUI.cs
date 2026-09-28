using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CodeForge.Combat;
using CodeForge.Data;

namespace CodeForge.UI
{
    [ExecuteAlways]
    public class CodeEditorPanelUI : MonoBehaviour
    {
        [Header("Class Fields Sockets")]
        [SerializeField] private CodeSocketUI maxHealthSocket;
        [SerializeField] private CodeSocketUI damageMultiplierSocket;
        [SerializeField] private CodeSocketUI critChanceSocket;
        [SerializeField] private CodeSocketUI critDamageSocket;
        [SerializeField] private CodeSocketUI evasionChanceSocket;
        [SerializeField] private CodeSocketUI baseShieldSocket;

        [Header("Modular Combat Methods Sockets")]
        [SerializeField] private CodeSocketUI attackDamageSocket;
        [SerializeField] private CodeSocketUI applyBleedSocket;
        [SerializeField] private CodeSocketUI bleedDamageSocket;
        [SerializeField] private CodeSocketUI bleedDurationSocket;
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
        [SerializeField] private CodeSocketUI reactionElseActionSocket;

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
        public CodeSocketUI CritChanceSocketUI => critChanceSocket;
        public CodeSocketUI CritDamageSocketUI => critDamageSocket;
        public CodeSocketUI EvasionChanceSocketUI => evasionChanceSocket;
        public CodeSocketUI BaseShieldSocketUI => baseShieldSocket;
        public CodeSocketUI AttackDamageSocketUI => attackDamageSocket;
        public CodeSocketUI ApplyBleedSocketUI => applyBleedSocket;
        public CodeSocketUI BleedDamageSocketUI => bleedDamageSocket;
        public CodeSocketUI BleedDurationSocketUI => bleedDurationSocket;
        public CodeSocketUI DefendShieldSocketUI => defendShieldSocket;
        public CodeSocketUI StanceSocketUI => stanceSocket;
        public CodeSocketUI ReactionConditionSocketUI => reactionConditionSocket;
        public CodeSocketUI ReactionActionSocketUI => reactionActionSocket;
        public CodeSocketUI ReactionElseActionSocketUI => reactionElseActionSocket;

        private void Awake()
        {
            if (targetSocket == null) targetSocket = targetingSocket;
            if (conditionSocket == null) conditionSocket = piercingSocket;
            if (thenActionSocket == null) thenActionSocket = damageSocket;
            if (elseActionSocket == null) elseActionSocket = multiplierSocket;

            if (tokenInventoryContainer != null)
            {
                var scrollRect = tokenInventoryContainer.GetComponentInParent<ScrollRect>();
                if (scrollRect != null)
                {
                    scrollRect.scrollSensitivity = 35f;
                }
            }

            EnsureAllSockets();

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
            RegisterSocket(critChanceSocket);
            RegisterSocket(critDamageSocket);
            RegisterSocket(evasionChanceSocket);
            RegisterSocket(baseShieldSocket);
            RegisterSocket(attackDamageSocket);
            RegisterSocket(applyBleedSocket);
            RegisterSocket(bleedDamageSocket);
            RegisterSocket(bleedDurationSocket);
            RegisterSocket(defendShieldSocket);
            RegisterSocket(stanceSocket);
            RegisterSocket(reactionConditionSocket);
            RegisterSocket(reactionActionSocket);
            RegisterSocket(reactionElseActionSocket);

            FormatAllEditorRows();
            EnsureScrollPadding();
        }

        public void EnsureAllSockets()
        {
            // Discover any sockets already existing in children
            var sockets = GetComponentsInChildren<CodeSocketUI>(true);
            foreach (var s in sockets)
            {
                if (s == null) continue;
                switch (s.SocketRole)
                {
                    case CodeSocketRole.CritChance:
                        if (critChanceSocket == null) critChanceSocket = s;
                        break;
                    case CodeSocketRole.CritDamage:
                        if (critDamageSocket == null) critDamageSocket = s;
                        break;
                    case CodeSocketRole.EvasionChance:
                        if (evasionChanceSocket == null) evasionChanceSocket = s;
                        break;
                    case CodeSocketRole.ApplyBleed:
                        if (applyBleedSocket == null) applyBleedSocket = s;
                        break;
                    case CodeSocketRole.BleedDamage:
                        if (bleedDamageSocket == null) bleedDamageSocket = s;
                        break;
                    case CodeSocketRole.BleedDuration:
                        if (bleedDurationSocket == null) bleedDurationSocket = s;
                        break;
                    case CodeSocketRole.ReactionElseAction:
                        if (reactionElseActionSocket == null) reactionElseActionSocket = s;
                        break;
                }
            }

            bool anyRowsCreated = false;

            // Step 1: Class fields (critChance, critDamage, evasionChance)
            EditorLineRowUI dmgMultRow = damageMultiplierSocket != null ? damageMultiplierSocket.GetComponentInParent<EditorLineRowUI>() : null;
            if (dmgMultRow != null)
            {
                Transform contentParent = dmgMultRow.transform.parent;
                int currentInsertIdx = dmgMultRow.transform.GetSiblingIndex() + 1;

                if (critChanceSocket != null)
                {
                    var row = critChanceSocket.GetComponentInParent<EditorLineRowUI>();
                    if (row != null)
                    {
                        SetRowTexts(row.gameObject, "    <color=#569CD6>public float</color> <color=#9CDCFE>critChance</color> <color=#D4D4D4>=</color> ", " <color=#D4D4D4>*</color> <color=#B5CEA8>0.10f</color><color=#D4D4D4>;</color>");
                    }
                }
                else
                {
                    GameObject rowObj = Instantiate(dmgMultRow.gameObject, contentParent);
                    rowObj.name = "Line_critChance";
                    rowObj.transform.SetSiblingIndex(currentInsertIdx++);
                    SetRowTexts(rowObj, "    <color=#569CD6>public float</color> <color=#9CDCFE>critChance</color> <color=#D4D4D4>=</color> ", " <color=#D4D4D4>*</color> <color=#B5CEA8>0.10f</color><color=#D4D4D4>;</color>");

                    critChanceSocket = rowObj.GetComponentInChildren<CodeSocketUI>();
                    if (critChanceSocket != null)
                    {
                        critChanceSocket.gameObject.name = "Socket_critChance";
                        critChanceSocket.InitializeRuntime(CodeSocketRole.CritChance, CodeTokenType.Float, this);
                        critChanceSocket.AssignToken(null);
                    }
                    anyRowsCreated = true;
                }

                if (critDamageSocket == null)
                {
                    GameObject rowObj = Instantiate(dmgMultRow.gameObject, contentParent);
                    rowObj.name = "Line_critDamage";
                    rowObj.transform.SetSiblingIndex(currentInsertIdx++);
                    SetRowTexts(rowObj, "    <color=#569CD6>public int</color> <color=#9CDCFE>critDamage</color> <color=#D4D4D4>=</color> ", ";");

                    critDamageSocket = rowObj.GetComponentInChildren<CodeSocketUI>();
                    if (critDamageSocket != null)
                    {
                        critDamageSocket.gameObject.name = "Socket_critDamage";
                        critDamageSocket.InitializeRuntime(CodeSocketRole.CritDamage, CodeTokenType.Int, this);
                        critDamageSocket.AssignToken(null);
                    }
                    anyRowsCreated = true;
                }

                if (evasionChanceSocket != null)
                {
                    var row = evasionChanceSocket.GetComponentInParent<EditorLineRowUI>();
                    if (row != null)
                    {
                        SetRowTexts(row.gameObject, "    <color=#569CD6>public float</color> <color=#9CDCFE>evasionChance</color> <color=#D4D4D4>=</color> ", " <color=#D4D4D4>*</color> <color=#B5CEA8>0.10f</color><color=#D4D4D4>;</color> <color=#6A9955>// (Max: 50%)</color>");
                    }
                }
                else
                {
                    GameObject rowObj = Instantiate(dmgMultRow.gameObject, contentParent);
                    rowObj.name = "Line_evasionChance";
                    rowObj.transform.SetSiblingIndex(currentInsertIdx++);
                    SetRowTexts(rowObj, "    <color=#569CD6>public float</color> <color=#9CDCFE>evasionChance</color> <color=#D4D4D4>=</color> ", " <color=#D4D4D4>*</color> <color=#B5CEA8>0.10f</color><color=#D4D4D4>;</color> <color=#6A9955>// (Max: 50%)</color>");

                    evasionChanceSocket = rowObj.GetComponentInChildren<CodeSocketUI>();
                    if (evasionChanceSocket != null)
                    {
                        evasionChanceSocket.gameObject.name = "Socket_evasionChance";
                        evasionChanceSocket.InitializeRuntime(CodeSocketRole.EvasionChance, CodeTokenType.Float, this);
                        evasionChanceSocket.AssignToken(null);
                    }
                }
            }

            // Step 2: Bleed block inside Attack(Enemy target)
            if (applyBleedSocket == null && attackDamageSocket != null)
            {
                EditorLineRowUI attackRow = attackDamageSocket.GetComponentInParent<EditorLineRowUI>();
                if (attackRow != null)
                {
                    Transform contentParent = attackRow.transform.parent;
                    int insertIdx = attackRow.transform.GetSiblingIndex() + 2; // right after target.TakeDamage(damage);
                    GameObject rowObj = Instantiate(attackRow.gameObject, contentParent);
                    rowObj.name = "Line_applyBleed";
                    rowObj.transform.SetSiblingIndex(insertIdx);

                    SetRowTexts(rowObj, "        <color=#C586C0>if</color> ( ", " ) <color=#9CDCFE>target</color>.<color=#DCDCAA>ApplyBleed</color>(<color=#B5CEA8>3</color>, <color=#B5CEA8>2</color>);");

                    applyBleedSocket = rowObj.GetComponentInChildren<CodeSocketUI>();
                    if (applyBleedSocket != null)
                    {
                        applyBleedSocket.gameObject.name = "Socket_applyBleed";
                        applyBleedSocket.InitializeRuntime(CodeSocketRole.ApplyBleed, CodeTokenType.Bool, this);
                        applyBleedSocket.AssignToken(null);
                    }

                    Transform postTr = rowObj.transform.Find("PostText");
                    if (postTr != null && applyBleedSocket != null)
                    {
                        GameObject mid1 = Instantiate(postTr.gameObject, rowObj.transform);
                        mid1.name = "MidText1";
                        var t1 = mid1.GetComponent<TextMeshProUGUI>();
                        if (t1 != null) t1.text = " ) <color=#9CDCFE>target</color>.<color=#DCDCAA>ApplyBleed</color>( ";

                        GameObject dmgObj = Instantiate(applyBleedSocket.gameObject, rowObj.transform);
                        dmgObj.name = "Socket_bleedDamage";
                        bleedDamageSocket = dmgObj.GetComponent<CodeSocketUI>();
                        bleedDamageSocket.InitializeRuntime(CodeSocketRole.BleedDamage, CodeTokenType.Int, this);
                        bleedDamageSocket.AssignToken(null);

                        GameObject mid2 = Instantiate(postTr.gameObject, rowObj.transform);
                        mid2.name = "MidText2";
                        var t2 = mid2.GetComponent<TextMeshProUGUI>();
                        if (t2 != null) t2.text = ", ";

                        GameObject durObj = Instantiate(applyBleedSocket.gameObject, rowObj.transform);
                        durObj.name = "Socket_bleedDuration";
                        bleedDurationSocket = durObj.GetComponent<CodeSocketUI>();
                        bleedDurationSocket.InitializeRuntime(CodeSocketRole.BleedDuration, CodeTokenType.Int, this);
                        bleedDurationSocket.AssignToken(null);

                        var tPost = postTr.GetComponent<TextMeshProUGUI>();
                        if (tPost != null) tPost.text = " );";
                        postTr.SetAsLastSibling();
                    }

                    anyRowsCreated = true;
                }
            }

            // Step 3: else action block inside OnTakeDamage
            if (reactionElseActionSocket == null && reactionActionSocket != null)
            {
                EditorLineRowUI reactionRow = reactionActionSocket.GetComponentInParent<EditorLineRowUI>();
                if (reactionRow != null)
                {
                    Transform contentParent = reactionRow.transform.parent;
                    int insertIdx = reactionRow.transform.GetSiblingIndex() + 2; // right after closing brace of THEN block
                    GameObject rowObj = Instantiate(reactionRow.gameObject, contentParent);
                    rowObj.name = "Line_reactionElseAction";
                    rowObj.transform.SetSiblingIndex(insertIdx);

                    SetRowTexts(rowObj, "        <color=#C586C0>else</color> { ", " }");

                    reactionElseActionSocket = rowObj.GetComponentInChildren<CodeSocketUI>();
                    if (reactionElseActionSocket != null)
                    {
                        reactionElseActionSocket.gameObject.name = "Socket_reactionElseAction";
                        reactionElseActionSocket.InitializeRuntime(CodeSocketRole.ReactionElseAction, CodeTokenType.Action, this);
                        reactionElseActionSocket.AssignToken(null);
                    }
                    anyRowsCreated = true;
                }
            }

            if (anyRowsCreated)
            {
                RenumberAllRows();
                FormatAllEditorRows();
            }
        }

        private void SetRowTexts(GameObject rowObj, string preText, string postText)
        {
            Transform pre = rowObj.transform.Find("PreText");
            if (pre != null)
            {
                var tmp = pre.GetComponent<TextMeshProUGUI>();
                if (tmp != null) tmp.text = preText;
            }
            else
            {
                var texts = rowObj.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var t in texts)
                {
                    if (t.gameObject.name == "PreText")
                    {
                        t.text = preText;
                        break;
                    }
                }
            }

            Transform post = rowObj.transform.Find("PostText");
            if (post != null)
            {
                var tmp = post.GetComponent<TextMeshProUGUI>();
                if (tmp != null) tmp.text = postText;
            }
            else
            {
                var texts = rowObj.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var t in texts)
                {
                    if (t.gameObject.name == "PostText")
                    {
                        t.text = postText;
                        break;
                    }
                }
            }
        }

        public void RenumberAllRows()
        {
            var anyRow = GetComponentInChildren<EditorLineRowUI>(true);
            Transform content = anyRow != null ? anyRow.transform.parent : null;
            if (content == null) return;

            var rows = content.GetComponentsInChildren<EditorLineRowUI>(true);
            for (int i = 0; i < rows.Length; i++)
            {
                int lineNum = i + 1;
                rows[i].lineNumber = lineNum;
                rows[i].gameObject.name = $"Line_{lineNum:D2}";
                var numText = rows[i].transform.Find("LineNumberContainer/LineNumberText")?.GetComponent<TextMeshProUGUI>();
                if (numText == null)
                {
                    var allTexts = rows[i].GetComponentsInChildren<TextMeshProUGUI>(true);
                    foreach (var t in allTexts)
                    {
                        if (t.gameObject.name.Contains("LineNumber") || (t.transform.parent != null && t.transform.parent.name.Contains("LineNumber")))
                        {
                            numText = t;
                            break;
                        }
                    }
                }
                if (numText != null)
                {
                    numText.text = lineNum.ToString("D2");
                }
            }
            CacheLineRows();
        }

        public void HighlightSocketRow(CodeSocketUI socket, bool active = true)
        {
            if (socket != null)
            {
                var row = socket.GetComponentInParent<EditorLineRowUI>();
                if (row != null)
                {
                    row.SetHighlight(active);
                }
            }
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

            if (Application.isPlaying)
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

                // Register tokens with IntelliSense
                if (IntelliSensePopoverUI.Instance != null)
                {
                    if (starterTokens != null) IntelliSensePopoverUI.Instance.RegisterAvailableTokens(starterTokens);
                    if (defaultMaxHealth != null) IntelliSensePopoverUI.Instance.RegisterAvailableTokens(new CodeTokenSO[] { defaultMaxHealth });
                }
            }

            FormatAllEditorRows();
            EnsureScrollPadding();
        }

        public void FormatAllEditorRows()
        {
            var rows = GetComponentsInChildren<EditorLineRowUI>(true);
            foreach (var row in rows)
            {
                if (row != null)
                {
                    row.FormatRow();
                }
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
            ResetSocketHighlight(critChanceSocket);
            ResetSocketHighlight(critDamageSocket);
            ResetSocketHighlight(evasionChanceSocket);
            ResetSocketHighlight(baseShieldSocket);
            ResetSocketHighlight(attackDamageSocket);
            ResetSocketHighlight(applyBleedSocket);
            ResetSocketHighlight(bleedDamageSocket);
            ResetSocketHighlight(bleedDurationSocket);
            ResetSocketHighlight(defendShieldSocket);
            ResetSocketHighlight(stanceSocket);
            ResetSocketHighlight(reactionConditionSocket);
            ResetSocketHighlight(reactionActionSocket);
            ResetSocketHighlight(reactionElseActionSocket);
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
                    else if (reactionActionSocket != null && reactionActionSocket.AssignedToken == null)
                        reactionActionSocket.AssignToken(token);
                    else if (reactionElseActionSocket != null && reactionElseActionSocket.AssignedToken == null)
                        reactionElseActionSocket.AssignToken(token);
                    else if (ThenActionSocketUI != null)
                        ThenActionSocketUI.AssignToken(token);
                    break;

                case CodeTokenType.Float:
                    if (damageMultiplierSocket != null && damageMultiplierSocket.AssignedToken == null)
                        damageMultiplierSocket.AssignToken(token);
                    else if (critChanceSocket != null && critChanceSocket.AssignedToken == null)
                        critChanceSocket.AssignToken(token);
                    else if (evasionChanceSocket != null && evasionChanceSocket.AssignedToken == null)
                        evasionChanceSocket.AssignToken(token);
                    else if (damageMultiplierSocket != null)
                        damageMultiplierSocket.AssignToken(token);
                    break;

                case CodeTokenType.Int:
                    if (attackDamageSocket != null && attackDamageSocket.AssignedToken == null)
                        attackDamageSocket.AssignToken(token);
                    else if (defendShieldSocket != null && defendShieldSocket.AssignedToken == null)
                        defendShieldSocket.AssignToken(token);
                    else if (critDamageSocket != null && critDamageSocket.AssignedToken == null)
                        critDamageSocket.AssignToken(token);
                    else if (baseShieldSocket != null && baseShieldSocket.AssignedToken == null)
                        baseShieldSocket.AssignToken(token);
                    else if (bleedDamageSocket != null && bleedDamageSocket.AssignedToken == null)
                        bleedDamageSocket.AssignToken(token);
                    else if (bleedDurationSocket != null && bleedDurationSocket.AssignedToken == null)
                        bleedDurationSocket.AssignToken(token);
                    else if (maxHealthSocket != null && maxHealthSocket.AssignedToken == null)
                        maxHealthSocket.AssignToken(token);
                    else if (baseShieldSocket != null)
                        baseShieldSocket.AssignToken(token);
                    break;

                case CodeTokenType.Bool:
                    if (applyBleedSocket != null)
                        applyBleedSocket.AssignToken(token);
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

        public float GetCritChance()
        {
            if (critChanceSocket == null || critChanceSocket.AssignedToken == null) return 0.0f;
            float rawTokenVal = critChanceSocket.AssignedToken.floatValue;
            return rawTokenVal * 0.10f; // e.g. 1.25f * 0.10f = 0.125f (12.5%)
        }

        public int GetCritDamage()
        {
            if (critDamageSocket != null && critDamageSocket.AssignedToken != null)
            {
                return Mathf.Max(0, critDamageSocket.AssignedToken.intValue);
            }
            return 0;
        }

        public float GetEvasionChance()
        {
            if (evasionChanceSocket == null || evasionChanceSocket.AssignedToken == null) return 0.0f;
            float rawTokenVal = evasionChanceSocket.AssignedToken.floatValue;
            float calculatedEvasion = rawTokenVal * 0.10f; // e.g. 1.50f * 0.10f = 0.15f (15%)

            if (calculatedEvasion > 0.50f)
            {
                ConsoleLogUI.Log($"<color=#E5C07B>[Warning] Evasion {calculatedEvasion * 100:0.0}% exceeds maximum limit. Clamped to 50.0%!</color>");
                return 0.50f;
            }

            return calculatedEvasion;
        }

        public bool GetApplyBleed()
        {
            if (applyBleedSocket != null && applyBleedSocket.AssignedToken != null)
            {
                return applyBleedSocket.AssignedToken.boolValue;
            }
            return false;
        }

        public int GetBleedDamage()
        {
            if (bleedDamageSocket != null && bleedDamageSocket.AssignedToken != null)
            {
                return Mathf.Max(1, bleedDamageSocket.AssignedToken.intValue);
            }
            return 3;
        }

        public int GetBleedDuration()
        {
            if (bleedDurationSocket != null && bleedDurationSocket.AssignedToken != null)
            {
                return Mathf.Max(1, bleedDurationSocket.AssignedToken.intValue);
            }
            return 2;
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
                return Mathf.Max(1, defendShieldShieldOrDefault());
            }
            return 5;
        }

        private int defendShieldShieldOrDefault()
        {
            return defendShieldSocket.AssignedToken != null ? defendShieldSocket.AssignedToken.intValue : 5;
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

        public ActionTokenSO GetReactionElseActionToken()
        {
            return reactionElseActionSocket != null ? reactionElseActionSocket.AssignedToken as ActionTokenSO : null;
        }

        public void EnsureScrollPadding()
        {
            var scrolls = GetComponentsInChildren<ScrollRect>(true);
            foreach (var s in scrolls)
            {
                if (s != null && s.name.Contains("Editor") && s.content != null)
                {
                    var vlg = s.content.GetComponent<VerticalLayoutGroup>();
                    if (vlg != null)
                    {
                        var pad = vlg.padding;
                        pad.bottom = 45;
                        vlg.padding = pad;
                    }
                }
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Bake All Rows To Scene")]
        public void BakeAllRowsToScene()
        {
            EnsureAllSockets();
            PrePopulateDefaultTokens();

            var allSockets = GetComponentsInChildren<CodeSocketUI>(true);
            foreach (var s in allSockets)
            {
                if (s != null)
                {
                    s.SetEditorReference(this);
                    s.RefreshDisplay();
                    s.UpdateSocketWidth();
                    UnityEditor.EditorUtility.SetDirty(s);
                    var txt = s.GetComponentInChildren<TextMeshProUGUI>();
                    if (txt != null)
                    {
                        UnityEditor.EditorUtility.SetDirty(txt);
                    }
                }
            }

            RenumberAllRows();
            FormatAllEditorRows();
            EnsureScrollPadding();

            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.EditorUtility.SetDirty(gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(gameObject.scene);
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log("[CodeEditorPanelUI] Successfully baked all rows and sockets into the scene!");
        }
#endif
    }
}
