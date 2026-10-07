using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CodeForge.Combat;
using CodeForge.Data;

namespace CodeForge.UI
{
    public enum ShelfSortMode { Standard, Rarity, Type }

    [ExecuteAlways]
    public class CodeEditorPanelUI : MonoBehaviour
    {
        [Header("Class Fields Sockets")]
        [SerializeField] private CodeSocketUI maxHealthSocket;
        [SerializeField] private CodeSocketUI damageMultiplierSocket;
        [SerializeField] private CodeSocketUI critChancePercentSocket;
        [SerializeField] private CodeSocketUI critMultiplierSocket;
        [SerializeField] private CodeSocketUI evasionChancePercentSocket;
        [SerializeField] private CodeSocketUI damageReductionSocket;
        [SerializeField] private CodeSocketUI baseShieldSocket;

        [SerializeField, HideInInspector] private CodeSocketUI critChanceSocket;
        [SerializeField, HideInInspector] private CodeSocketUI critDamageSocket;
        [SerializeField, HideInInspector] private CodeSocketUI evasionChanceSocket;

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
        [SerializeField] private CodeSocketUI conditionOpSocket;
        [SerializeField] private CodeSocketUI condition2Socket;
        [SerializeField] private CodeSocketUI thenActionSocket;
        [SerializeField] private CodeSocketUI elseActionSocket;

        [Header("OnTakeDamage Callback Sockets")]
        [SerializeField] private CodeSocketUI reactionConditionSocket;
        [SerializeField] private CodeSocketUI reactionConditionOpSocket;
        [SerializeField] private CodeSocketUI reactionCondition2Socket;
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
        [SerializeField] private CodeTokenSO defaultReactionCondition;
        [SerializeField] private ActionTokenSO defaultReactionAction;
        [SerializeField] private ActionTokenSO defaultReactionElseAction;

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
        [SerializeField] private TextMeshProUGUI shelfHeaderText;
        [SerializeField] private Button shelfSortButton;
        [SerializeField] private TextMeshProUGUI shelfSortText;
        private ShelfSortMode currentSortMode = ShelfSortMode.Standard;
        private int nextAcquisitionCounter = 0;
        public ShelfSortMode CurrentSortMode => currentSortMode;

        [Header("Starter Loadout")]
        [SerializeField] private List<CodeTokenSO> starterTokens = new List<CodeTokenSO>();

        public List<CodeTokenSO> StarterTokens => starterTokens;

        private Dictionary<int, EditorLineRowUI> lineRows = new Dictionary<int, EditorLineRowUI>();

        public CodeSocketUI TargetSocketUI => targetSocket != null ? targetSocket : targetingSocket;
        public CodeSocketUI ConditionSocketUI => conditionSocket != null ? conditionSocket : piercingSocket;
        public CodeSocketUI ConditionOpSocketUI => conditionOpSocket;
        public CodeSocketUI Condition2SocketUI => condition2Socket;
        public CodeSocketUI ThenActionSocketUI => thenActionSocket != null ? thenActionSocket : damageSocket;
        public CodeSocketUI ElseActionSocketUI => elseActionSocket != null ? elseActionSocket : multiplierSocket;
        public CodeSocketUI MaxHealthSocketUI => maxHealthSocket;
        public CodeSocketUI DamageMultiplierSocketUI => damageMultiplierSocket;
        public CodeSocketUI CritChancePercentSocketUI => critChancePercentSocket != null ? critChancePercentSocket : critChanceSocket;
        public CodeSocketUI CritMultiplierSocketUI => critMultiplierSocket != null ? critMultiplierSocket : critDamageSocket;
        public CodeSocketUI EvasionChancePercentSocketUI => evasionChancePercentSocket != null ? evasionChancePercentSocket : evasionChanceSocket;
        public CodeSocketUI DamageReductionSocketUI => damageReductionSocket;
        public CodeSocketUI BaseShieldSocketUI => baseShieldSocket;
        public CodeSocketUI AttackDamageSocketUI => attackDamageSocket;
        public CodeSocketUI ApplyBleedSocketUI => applyBleedSocket;
        public CodeSocketUI BleedDamageSocketUI => bleedDamageSocket;
        public CodeSocketUI BleedDurationSocketUI => bleedDurationSocket;
        public CodeSocketUI DefendShieldSocketUI => defendShieldSocket;
        public CodeSocketUI StanceSocketUI => stanceSocket;
        public CodeSocketUI ReactionConditionSocketUI => reactionConditionSocket;
        public CodeSocketUI ReactionConditionOpSocketUI => reactionConditionOpSocket;
        public CodeSocketUI ReactionCondition2SocketUI => reactionCondition2Socket;
        public CodeSocketUI ReactionActionSocketUI => reactionActionSocket;
        public CodeSocketUI ReactionElseActionSocketUI => reactionElseActionSocket;

        // Legacy compatibility properties
        public CodeSocketUI CritChanceSocketUI => CritChancePercentSocketUI;
        public CodeSocketUI CritDamageSocketUI => CritMultiplierSocketUI;
        public CodeSocketUI EvasionChanceSocketUI => EvasionChancePercentSocketUI;

        private void Awake()
        {
            if (targetSocket == null) targetSocket = targetingSocket;
            if (conditionSocket == null) conditionSocket = piercingSocket;
            if (thenActionSocket == null) thenActionSocket = damageSocket;
            if (elseActionSocket == null) elseActionSocket = multiplierSocket;

            EnsureStarterTokens();

            if (tokenInventoryContainer != null)
            {
                for (int i = tokenInventoryContainer.childCount - 1; i >= 0; i--)
                {
                    var child = tokenInventoryContainer.GetChild(i);
                    if (Application.isPlaying)
                        Destroy(child.gameObject);
                    else
                        DestroyImmediate(child.gameObject);
                }

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
            RegisterSocket(conditionOpSocket);
            RegisterSocket(condition2Socket);
            RegisterSocket(ThenActionSocketUI);
            RegisterSocket(ElseActionSocketUI);
            RegisterSocket(maxHealthSocket);
            RegisterSocket(damageMultiplierSocket);
            RegisterSocket(CritChancePercentSocketUI);
            RegisterSocket(CritMultiplierSocketUI);
            RegisterSocket(EvasionChancePercentSocketUI);
            RegisterSocket(damageReductionSocket);
            RegisterSocket(baseShieldSocket);
            RegisterSocket(attackDamageSocket);
            RegisterSocket(applyBleedSocket);
            RegisterSocket(bleedDamageSocket);
            RegisterSocket(bleedDurationSocket);
            RegisterSocket(defendShieldSocket);
            RegisterSocket(stanceSocket);
            RegisterSocket(reactionConditionSocket);
            RegisterSocket(reactionConditionOpSocket);
            RegisterSocket(reactionCondition2Socket);
            RegisterSocket(reactionActionSocket);
            RegisterSocket(reactionElseActionSocket);

            FormatAllEditorRows();
            UpdateChanceRowDisplays();
            EnsureScrollPadding();
            EnsureShelfHeaderLabel();
            EnsureShelfDiscardSlot();
            EnsureHighContrastScrollbars();
            SetupAllMethodFolding();
            UpdateRoomMethodUnlocks(CombatManager.Instance != null ? CombatManager.Instance.CurrentRoomIndex : 1);
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
                    case CodeSocketRole.CritChancePercent:
                        if (critChancePercentSocket == null) critChancePercentSocket = s;
                        break;
                    case CodeSocketRole.CritDamage:
                    case CodeSocketRole.CritMultiplier:
                        if (critMultiplierSocket == null) critMultiplierSocket = s;
                        break;
                    case CodeSocketRole.EvasionChance:
                    case CodeSocketRole.EvasionChancePercent:
                        if (evasionChancePercentSocket == null) evasionChancePercentSocket = s;
                        break;
                    case CodeSocketRole.DamageReduction:
                        if (damageReductionSocket == null) damageReductionSocket = s;
                        break;
                    case CodeSocketRole.ConditionOp:
                        if (conditionOpSocket == null) conditionOpSocket = s;
                        break;
                    case CodeSocketRole.Condition2:
                        if (condition2Socket == null) condition2Socket = s;
                        break;
                    case CodeSocketRole.ReactionConditionOp:
                        if (reactionConditionOpSocket == null) reactionConditionOpSocket = s;
                        break;
                    case CodeSocketRole.ReactionCondition2:
                        if (reactionCondition2Socket == null) reactionCondition2Socket = s;
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

            // Step 1: Class fields (damageMultiplier, critChancePercent, critMultiplier, evasionChancePercent, damageReduction, baseShield)
            EditorLineRowUI dmgMultRow = damageMultiplierSocket != null ? damageMultiplierSocket.GetComponentInParent<EditorLineRowUI>() : null;
            if (dmgMultRow != null)
            {
                SetRowTexts(dmgMultRow.gameObject, "    <color=#569CD6>public float</color> <color=#9CDCFE>damageMultiplier</color> <color=#D4D4D4>=</color> ", " <color=#D4D4D4>;</color> <color=#6A9955>// x (Default: 1.0x)</color>");

                Transform contentParent = dmgMultRow.transform.parent;
                int currentInsertIdx = dmgMultRow.transform.GetSiblingIndex() + 1;

                if (critChancePercentSocket != null)
                {
                    critChancePercentSocket.InitializeRuntime(CodeSocketRole.CritChancePercent, CodeTokenType.Int, this);
                    critChancePercentSocket.gameObject.name = "Socket_critChancePercent";
                    var row = critChancePercentSocket.GetComponentInParent<EditorLineRowUI>();
                    if (row != null)
                    {
                        row.gameObject.name = "Line_critChancePercent";
                        SetRowTexts(row.gameObject, "    <color=#569CD6>public int</color> <color=#9CDCFE>critChancePercent</color> <color=#D4D4D4>=</color> ", " <color=#D4D4D4>;</color> <color=#6A9955>// %</color>");
                    }
                }
                else
                {
                    GameObject rowObj = Instantiate(dmgMultRow.gameObject, contentParent);
                    rowObj.name = "Line_critChancePercent";
                    rowObj.transform.SetSiblingIndex(currentInsertIdx++);
                    SetRowTexts(rowObj, "    <color=#569CD6>public int</color> <color=#9CDCFE>critChancePercent</color> <color=#D4D4D4>=</color> ", " <color=#D4D4D4>;</color> <color=#6A9955>// %</color>");

                    critChancePercentSocket = rowObj.GetComponentInChildren<CodeSocketUI>();
                    if (critChancePercentSocket != null)
                    {
                        critChancePercentSocket.gameObject.name = "Socket_critChancePercent";
                        critChancePercentSocket.InitializeRuntime(CodeSocketRole.CritChancePercent, CodeTokenType.Int, this);
                        critChancePercentSocket.AssignToken(null);
                    }
                    anyRowsCreated = true;
                }

                if (critMultiplierSocket != null)
                {
                    critMultiplierSocket.InitializeRuntime(CodeSocketRole.CritMultiplier, CodeTokenType.Float, this);
                    critMultiplierSocket.gameObject.name = "Socket_critMultiplier";
                    var row = critMultiplierSocket.GetComponentInParent<EditorLineRowUI>();
                    if (row != null)
                    {
                        row.gameObject.name = "Line_critMultiplier";
                        SetRowTexts(row.gameObject, "    <color=#569CD6>public float</color> <color=#9CDCFE>critMultiplier</color> <color=#D4D4D4>=</color> ", " <color=#D4D4D4>;</color> <color=#6A9955>// x (Default: 1.1x)</color>");
                    }
                }
                else
                {
                    GameObject rowObj = Instantiate(dmgMultRow.gameObject, contentParent);
                    rowObj.name = "Line_critMultiplier";
                    rowObj.transform.SetSiblingIndex(currentInsertIdx++);
                    SetRowTexts(rowObj, "    <color=#569CD6>public float</color> <color=#9CDCFE>critMultiplier</color> <color=#D4D4D4>=</color> ", " <color=#D4D4D4>;</color> <color=#6A9955>// x (Default: 1.1x)</color>");

                    critMultiplierSocket = rowObj.GetComponentInChildren<CodeSocketUI>();
                    if (critMultiplierSocket != null)
                    {
                        critMultiplierSocket.gameObject.name = "Socket_critMultiplier";
                        critMultiplierSocket.InitializeRuntime(CodeSocketRole.CritMultiplier, CodeTokenType.Float, this);
                        critMultiplierSocket.AssignToken(null);
                    }
                    anyRowsCreated = true;
                }

                if (evasionChancePercentSocket != null)
                {
                    evasionChancePercentSocket.InitializeRuntime(CodeSocketRole.EvasionChancePercent, CodeTokenType.Int, this);
                    evasionChancePercentSocket.gameObject.name = "Socket_evasionChancePercent";
                    var row = evasionChancePercentSocket.GetComponentInParent<EditorLineRowUI>();
                    if (row != null)
                    {
                        row.gameObject.name = "Line_evasionChancePercent";
                        SetRowTexts(row.gameObject, "    <color=#569CD6>public int</color> <color=#9CDCFE>evasionChancePercent</color> <color=#D4D4D4>=</color> ", " <color=#D4D4D4>;</color> <color=#6A9955>// % (Max: 50%)</color>");
                    }
                }
                else
                {
                    GameObject rowObj = Instantiate(dmgMultRow.gameObject, contentParent);
                    rowObj.name = "Line_evasionChancePercent";
                    rowObj.transform.SetSiblingIndex(currentInsertIdx++);
                    SetRowTexts(rowObj, "    <color=#569CD6>public int</color> <color=#9CDCFE>evasionChancePercent</color> <color=#D4D4D4>=</color> ", " <color=#D4D4D4>;</color> <color=#6A9955>// % (Max: 50%)</color>");

                    evasionChancePercentSocket = rowObj.GetComponentInChildren<CodeSocketUI>();
                    if (evasionChancePercentSocket != null)
                    {
                        evasionChancePercentSocket.gameObject.name = "Socket_evasionChancePercent";
                        evasionChancePercentSocket.InitializeRuntime(CodeSocketRole.EvasionChancePercent, CodeTokenType.Int, this);
                        evasionChancePercentSocket.AssignToken(null);
                    }
                    anyRowsCreated = true;
                }

                if (damageReductionSocket != null)
                {
                    damageReductionSocket.InitializeRuntime(CodeSocketRole.DamageReduction, CodeTokenType.Int, this);
                    damageReductionSocket.gameObject.name = "Socket_damageReduction";
                    var row = damageReductionSocket.GetComponentInParent<EditorLineRowUI>();
                    if (row != null)
                    {
                        row.gameObject.name = "Line_damageReduction";
                        SetRowTexts(row.gameObject, "    <color=#569CD6>public int</color> <color=#9CDCFE>damageReduction</color> <color=#D4D4D4>=</color> ", " <color=#D4D4D4>;</color> <color=#6A9955>// (Flat Armor)</color>");
                    }
                }
                else
                {
                    int insertIdx = evasionChancePercentSocket != null
                        ? evasionChancePercentSocket.GetComponentInParent<EditorLineRowUI>().transform.GetSiblingIndex() + 1
                        : currentInsertIdx++;

                    GameObject rowObj = Instantiate(dmgMultRow.gameObject, contentParent);
                    rowObj.name = "Line_damageReduction";
                    rowObj.transform.SetSiblingIndex(insertIdx);
                    SetRowTexts(rowObj, "    <color=#569CD6>public int</color> <color=#9CDCFE>damageReduction</color> <color=#D4D4D4>=</color> ", " <color=#D4D4D4>;</color> <color=#6A9955>// (Flat Armor)</color>");

                    damageReductionSocket = rowObj.GetComponentInChildren<CodeSocketUI>();
                    if (damageReductionSocket != null)
                    {
                        damageReductionSocket.gameObject.name = "Socket_damageReduction";
                        damageReductionSocket.InitializeRuntime(CodeSocketRole.DamageReduction, CodeTokenType.Int, this);
                        damageReductionSocket.AssignToken(null);
                    }
                    anyRowsCreated = true;
                }

                if (baseShieldSocket != null)
                {
                    var row = baseShieldSocket.GetComponentInParent<EditorLineRowUI>();
                    if (row != null)
                    {
                        SetRowTexts(row.gameObject, "    <color=#569CD6>public int</color> <color=#9CDCFE>baseShield</color> <color=#D4D4D4>=</color> ", "<color=#D4D4D4>;</color>");
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
                        if (tPost != null) tPost.text = " ); <color=#6A9955>// (damage, duration)</color>";
                        postTr.SetAsLastSibling();
                    }

                    anyRowsCreated = true;
                }
            }
            else if (applyBleedSocket != null)
            {
                var row = applyBleedSocket.GetComponentInParent<EditorLineRowUI>();
                if (row != null)
                {
                    var postTr = row.transform.Find("PostText");
                    if (postTr != null)
                    {
                        var tPost = postTr.GetComponent<TextMeshProUGUI>();
                        if (tPost != null) tPost.text = " ); <color=#6A9955>// (damage, duration)</color>";
                    }
                }
            }

            // Step 3: Compound row inside ExecuteTurn()
            if (ConditionSocketUI != null)
            {
                EditorLineRowUI condRow = ConditionSocketUI.GetComponentInParent<EditorLineRowUI>();
                if (condRow != null)
                {
                    SetRowTexts(condRow.gameObject, "        <color=#C586C0>if</color> ( ", "");

                    if (conditionOpSocket == null)
                    {
                        Transform contentParent = condRow.transform.parent;
                        Transform existingCompound = contentParent.Find("Line_conditionCompound");
                        if (existingCompound != null)
                        {
                            var sList = existingCompound.GetComponentsInChildren<CodeSocketUI>(true);
                            foreach (var s in sList)
                            {
                                if (s.name.Contains("Op") || s.SocketRole == CodeSocketRole.ConditionOp) conditionOpSocket = s;
                                else condition2Socket = s;
                            }
                        }

                        if (conditionOpSocket == null)
                        {
                            int insertIdx = condRow.transform.GetSiblingIndex() + 1;
                            GameObject rowObj = Instantiate(condRow.gameObject, contentParent);
                            rowObj.name = "Line_conditionCompound";
                            rowObj.transform.SetSiblingIndex(insertIdx);

                            SetRowTexts(rowObj, "             ", " <color=#D4D4D4>)</color>");

                            conditionOpSocket = rowObj.GetComponentInChildren<CodeSocketUI>();
                            if (conditionOpSocket != null)
                            {
                                conditionOpSocket.gameObject.name = "Socket_conditionOp";
                                conditionOpSocket.InitializeRuntime(CodeSocketRole.ConditionOp, CodeTokenType.Operator, this);
                                conditionOpSocket.AssignToken(null);
                            }

                            Transform postTr = rowObj.transform.Find("PostText");
                            if (postTr != null && conditionOpSocket != null)
                            {
                                GameObject mid = Instantiate(postTr.gameObject, rowObj.transform);
                                mid.name = "MidText";
                                var tMid = mid.GetComponent<TextMeshProUGUI>();
                                if (tMid != null) tMid.text = " ";

                                GameObject c2Obj = Instantiate(conditionOpSocket.gameObject, rowObj.transform);
                                c2Obj.name = "Socket_condition2";
                                condition2Socket = c2Obj.GetComponent<CodeSocketUI>();
                                condition2Socket.InitializeRuntime(CodeSocketRole.Condition2, CodeTokenType.Condition, this);
                                condition2Socket.AssignToken(null);

                                postTr.SetAsLastSibling();
                            }
                            anyRowsCreated = true;
                        }
                    }
                }
            }

            // Step 4: Compound row inside OnTakeDamage()
            if (reactionConditionSocket != null)
            {
                EditorLineRowUI rCondRow = reactionConditionSocket.GetComponentInParent<EditorLineRowUI>();
                if (rCondRow != null)
                {
                    SetRowTexts(rCondRow.gameObject, "        <color=#C586C0>if</color> ( ", "");

                    if (reactionConditionOpSocket == null)
                    {
                        Transform contentParent = rCondRow.transform.parent;
                        Transform existingCompound = contentParent.Find("Line_reactionConditionCompound");
                        if (existingCompound != null)
                        {
                            var sList = existingCompound.GetComponentsInChildren<CodeSocketUI>(true);
                            foreach (var s in sList)
                            {
                                if (s.name.Contains("Op") || s.SocketRole == CodeSocketRole.ReactionConditionOp) reactionConditionOpSocket = s;
                                else reactionCondition2Socket = s;
                            }
                        }

                        if (reactionConditionOpSocket == null)
                        {
                            int insertIdx = rCondRow.transform.GetSiblingIndex() + 1;
                            GameObject rowObj = Instantiate(rCondRow.gameObject, contentParent);
                            rowObj.name = "Line_reactionConditionCompound";
                            rowObj.transform.SetSiblingIndex(insertIdx);

                            SetRowTexts(rowObj, "             ", " <color=#D4D4D4>)</color>");

                            reactionConditionOpSocket = rowObj.GetComponentInChildren<CodeSocketUI>();
                            if (reactionConditionOpSocket != null)
                            {
                                reactionConditionOpSocket.gameObject.name = "Socket_reactionConditionOp";
                                reactionConditionOpSocket.InitializeRuntime(CodeSocketRole.ReactionConditionOp, CodeTokenType.Operator, this);
                                reactionConditionOpSocket.AssignToken(null);
                            }

                            Transform postTr = rowObj.transform.Find("PostText");
                            if (postTr != null && reactionConditionOpSocket != null)
                            {
                                GameObject mid = Instantiate(postTr.gameObject, rowObj.transform);
                                mid.name = "MidText";
                                var tMid = mid.GetComponent<TextMeshProUGUI>();
                                if (tMid != null) tMid.text = " ";

                                GameObject c2Obj = Instantiate(reactionConditionOpSocket.gameObject, rowObj.transform);
                                c2Obj.name = "Socket_reactionCondition2";
                                reactionCondition2Socket = c2Obj.GetComponent<CodeSocketUI>();
                                reactionCondition2Socket.InitializeRuntime(CodeSocketRole.ReactionCondition2, CodeTokenType.Condition, this);
                                reactionCondition2Socket.AssignToken(null);

                                postTr.SetAsLastSibling();
                            }
                            anyRowsCreated = true;
                        }
                    }
                }
            }

            // Step 5: else action block inside OnTakeDamage
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

        public void EnsureStarterTokens()
        {
            if (starterTokens == null) starterTokens = new List<CodeTokenSO>();
            starterTokens.RemoveAll(t => t == null);

            // Strictly prune non-starter tokens (e.g. Int_25, Int_20, duplicate items)
            string[] requiredStarters = new string[] { "Int_8", "Action_AttackTarget", "Bool_true" };
            starterTokens.RemoveAll(t => !System.Array.Exists(requiredStarters, req => req == t.name));

            // Ensure unique set of the required starter tokens
            var uniqueList = new List<CodeTokenSO>();
            foreach (var req in requiredStarters)
            {
                var existing = starterTokens.Find(t => t.name == req);
                if (existing != null)
                {
                    uniqueList.Add(existing);
                }
                else
                {
                    CodeTokenSO found = null;
                    var allTokens = Resources.FindObjectsOfTypeAll<CodeTokenSO>();
                    foreach (var t in allTokens)
                    {
                        if (t != null && t.name == req)
                        {
                            found = t;
                            break;
                        }
                    }
#if UNITY_EDITOR
                    if (found == null)
                    {
                        found = UnityEditor.AssetDatabase.LoadAssetAtPath<CodeTokenSO>($"Assets/ScriptableObjects/Tokens/{req}.asset");
                    }
#endif
                    if (found != null)
                    {
                        uniqueList.Add(found);
                    }
                }
            }
            starterTokens = uniqueList;
        }

        public void UpdateChanceRowDisplays()
        {
            if (critChancePercentSocket != null)
            {
                var row = critChancePercentSocket.GetComponentInParent<EditorLineRowUI>();
                if (row != null)
                {
                    string postText = " <color=#D4D4D4>;</color> <color=#6A9955>// %</color>";
                    if (critChancePercentSocket.AssignedToken != null)
                    {
                        postText = $" <color=#D4D4D4>;</color> <color=#6A9955>// % (= {critChancePercentSocket.AssignedToken.intValue}%)</color>";
                    }
                    SetRowTexts(row.gameObject, "    <color=#569CD6>public int</color> <color=#9CDCFE>critChancePercent</color> <color=#D4D4D4>=</color> ", postText);
                    row.FormatRow();
                }
            }

            if (critMultiplierSocket != null)
            {
                var row = critMultiplierSocket.GetComponentInParent<EditorLineRowUI>();
                if (row != null)
                {
                    string postText = " <color=#D4D4D4>;</color> <color=#6A9955>// x (Default: 1.1x)</color>";
                    if (critMultiplierSocket.AssignedToken != null)
                    {
                        postText = $" <color=#D4D4D4>;</color> <color=#6A9955>// x (= {critMultiplierSocket.AssignedToken.floatValue:0.0#}x)</color>";
                    }
                    SetRowTexts(row.gameObject, "    <color=#569CD6>public float</color> <color=#9CDCFE>critMultiplier</color> <color=#D4D4D4>=</color> ", postText);
                    row.FormatRow();
                }
            }

            if (evasionChancePercentSocket != null)
            {
                var row = evasionChancePercentSocket.GetComponentInParent<EditorLineRowUI>();
                if (row != null)
                {
                    string postText = " <color=#D4D4D4>;</color> <color=#6A9955>// % (Max: 50%)</color>";
                    if (evasionChancePercentSocket.AssignedToken != null)
                    {
                        int capped = Mathf.Min(50, evasionChancePercentSocket.AssignedToken.intValue);
                        postText = $" <color=#D4D4D4>;</color> <color=#6A9955>// (= {capped}%, Max: 50%)</color>";
                    }
                    SetRowTexts(row.gameObject, "    <color=#569CD6>public int</color> <color=#9CDCFE>evasionChancePercent</color> <color=#D4D4D4>=</color> ", postText);
                    row.FormatRow();
                }
            }

            if (damageReductionSocket != null)
            {
                var row = damageReductionSocket.GetComponentInParent<EditorLineRowUI>();
                if (row != null)
                {
                    string postText = " <color=#D4D4D4>;</color> <color=#6A9955>// (Flat Armor)</color>";
                    if (damageReductionSocket.AssignedToken != null)
                    {
                        int armor = Mathf.Max(0, damageReductionSocket.AssignedToken.intValue);
                        postText = $" <color=#D4D4D4>;</color> <color=#6A9955>// (= -{armor} incoming DMG)</color>";
                    }
                    SetRowTexts(row.gameObject, "    <color=#569CD6>public int</color> <color=#9CDCFE>damageReduction</color> <color=#D4D4D4>=</color> ", postText);
                    row.FormatRow();
                }
            }

            if (damageMultiplierSocket != null)
            {
                var row = damageMultiplierSocket.GetComponentInParent<EditorLineRowUI>();
                if (row != null)
                {
                    string postText = " <color=#D4D4D4>;</color> <color=#6A9955>// x (Default: 1.0x)</color>";
                    if (damageMultiplierSocket.AssignedToken != null)
                    {
                        postText = $" <color=#D4D4D4>;</color> <color=#6A9955>// x (= {damageMultiplierSocket.AssignedToken.floatValue:0.0#}x)</color>";
                    }
                    SetRowTexts(row.gameObject, "    <color=#569CD6>public float</color> <color=#9CDCFE>damageMultiplier</color> <color=#D4D4D4>=</color> ", postText);
                    row.FormatRow();
                }
            }
        }

        private void Start()
        {
            EnsureStarterTokens();
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
            UpdateChanceRowDisplays();
            EnsureScrollPadding();
            EnsureInventoryScrollSensitivity();
            EnsureShelfSortChip();
            EnsureShelfDiscardSlot();
            EnsureHighContrastScrollbars();
            ApplyInventorySort();
            UpdateRoomMethodUnlocks(CombatManager.Instance != null ? CombatManager.Instance.CurrentRoomIndex : 1);
        }

        public void EnsureInventoryScrollSensitivity()
        {
            if (tokenInventoryContainer != null)
            {
                var shelfScroll = tokenInventoryContainer.GetComponentInParent<ScrollRect>();
                if (shelfScroll != null)
                {
                    shelfScroll.scrollSensitivity = 12f;
                }
            }
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

            if (defaultAttackDamage == null)
            {
                var allTokens = Resources.FindObjectsOfTypeAll<CodeTokenSO>();
                foreach (var t in allTokens)
                {
                    if (t != null && t.name == "Int_8")
                    {
                        defaultAttackDamage = t;
                        break;
                    }
                }
#if UNITY_EDITOR
                if (defaultAttackDamage == null)
                {
                    defaultAttackDamage = UnityEditor.AssetDatabase.LoadAssetAtPath<CodeTokenSO>("Assets/ScriptableObjects/Tokens/Int_8.asset");
                }
#endif
            }

            SlotDefaultIfEmpty(attackDamageSocket, defaultAttackDamage);
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
                panelCanvasGroup.alpha = locked ? 0.95f : 1.0f;
                panelCanvasGroup.blocksRaycasts = true;
                panelCanvasGroup.interactable = true;
            }

            if (compileAndRunButton != null)
            {
                compileAndRunButton.interactable = !locked;
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
            ResetSocketHighlight(conditionOpSocket);
            ResetSocketHighlight(condition2Socket);
            ResetSocketHighlight(ThenActionSocketUI);
            ResetSocketHighlight(ElseActionSocketUI);
            ResetSocketHighlight(maxHealthSocket);
            ResetSocketHighlight(damageMultiplierSocket);
            ResetSocketHighlight(critChancePercentSocket);
            ResetSocketHighlight(critMultiplierSocket);
            ResetSocketHighlight(evasionChancePercentSocket);
            ResetSocketHighlight(damageReductionSocket);
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
            ResetSocketHighlight(reactionConditionOpSocket);
            ResetSocketHighlight(reactionCondition2Socket);
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
                draggable.AcquisitionOrder = ++nextAcquisitionCounter;
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

            ApplyInventorySort();
        }

        public void EnsureShelfSortChip()
        {
            if (shelfSortButton != null)
            {
                shelfSortButton.onClick.RemoveAllListeners();
                shelfSortButton.onClick.AddListener(CycleShelfSortMode);
                UpdateShelfSortChipDisplay();
                return;
            }

            Transform existing = null;
            if (shelfHeaderText != null)
            {
                existing = shelfHeaderText.transform.Find("ShelfSortChip");
            }
            if (existing == null)
            {
                existing = transform.Find("ShelfSortChip");
            }

            GameObject chipObj;
            if (existing != null)
            {
                chipObj = existing.gameObject;
            }
            else
            {
                chipObj = new GameObject("ShelfSortChip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
                if (shelfHeaderText != null)
                {
                    chipObj.transform.SetParent(shelfHeaderText.transform, false);
                    var rt = chipObj.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(1f, 0.5f);
                    rt.anchorMax = new Vector2(1f, 0.5f);
                    rt.pivot = new Vector2(1f, 0.5f);
                    rt.anchoredPosition = new Vector2(0f, 0f);
                    rt.sizeDelta = new Vector2(140f, 22f);
                }
                else
                {
                    chipObj.transform.SetParent(transform, false);
                    var rt = chipObj.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.80f, 0.38f);
                    rt.anchorMax = new Vector2(0.96f, 0.41f);
                    rt.sizeDelta = Vector2.zero;
                }

                var img = chipObj.GetComponent<Image>();
                img.color = new Color(0.16f, 0.18f, 0.23f, 0.95f);

                var outline = chipObj.GetComponent<Outline>();
                outline.effectColor = new Color(0.35f, 0.40f, 0.50f, 0.8f);
                outline.effectDistance = new Vector2(1, -1);

                var txtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                txtObj.transform.SetParent(chipObj.transform, false);
                var trt = txtObj.GetComponent<RectTransform>();
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.sizeDelta = Vector2.zero;

                shelfSortText = txtObj.GetComponent<TextMeshProUGUI>();
                shelfSortText.alignment = TextAlignmentOptions.Center;
                shelfSortText.fontSize = 11f;
                shelfSortText.fontStyle = FontStyles.Bold;
                shelfSortText.color = new Color(0.38f, 0.69f, 0.94f, 1f); // #61AFEF
                shelfSortText.raycastTarget = false;
                shelfSortText.text = "[Sort: Standard]";
            }

            shelfSortButton = chipObj.GetComponent<Button>();
            if (shelfSortText == null) shelfSortText = chipObj.GetComponentInChildren<TextMeshProUGUI>(true);

            shelfSortButton.onClick.RemoveAllListeners();
            shelfSortButton.onClick.AddListener(CycleShelfSortMode);
            UpdateShelfSortChipDisplay();
        }

        public void CycleShelfSortMode()
        {
            currentSortMode = (ShelfSortMode)(((int)currentSortMode + 1) % 3);
            UpdateShelfSortChipDisplay();
            ApplyInventorySort();
            ConsoleLogUI.Log($"[Shelf] Inventory sorted by: <b><color=#61AFEF>{currentSortMode}</color></b>");
        }

        public void UpdateShelfSortChipDisplay()
        {
            if (shelfSortText != null)
            {
                shelfSortText.text = $"[Sort: {currentSortMode}]";
            }
        }

        public void ApplyInventorySort()
        {
            if (tokenInventoryContainer == null) return;

            var cards = new List<DraggableTokenCardUI>();
            for (int i = 0; i < tokenInventoryContainer.childCount; i++)
            {
                var card = tokenInventoryContainer.GetChild(i).GetComponent<DraggableTokenCardUI>();
                if (card != null)
                {
                    if (card.AcquisitionOrder == 0) card.AcquisitionOrder = ++nextAcquisitionCounter;
                    cards.Add(card);
                }
            }

            switch (currentSortMode)
            {
                case ShelfSortMode.Standard:
                    cards.Sort((a, b) => a.AcquisitionOrder.CompareTo(b.AcquisitionOrder));
                    break;

                case ShelfSortMode.Rarity:
                    // Legendary (Gold) -> Epic (Purple) -> Rare (Blue) -> Uncommon (Green) -> Common (Gray)
                    cards.Sort((a, b) =>
                    {
                        int rA = GetRaritySortWeight(a.Token?.rarity ?? TokenRarity.Common);
                        int rB = GetRaritySortWeight(b.Token?.rarity ?? TokenRarity.Common);
                        if (rA != rB) return rA.CompareTo(rB);
                        int tA = GetTypeSortWeight(a.Token?.tokenType ?? CodeTokenType.Int);
                        int tB = GetTypeSortWeight(b.Token?.tokenType ?? CodeTokenType.Int);
                        if (tA != tB) return tA.CompareTo(tB);
                        return a.AcquisitionOrder.CompareTo(b.AcquisitionOrder);
                    });
                    break;

                case ShelfSortMode.Type:
                    // Int -> Float -> Bool -> Action -> Condition -> Targeting -> Operator
                    cards.Sort((a, b) =>
                    {
                        int tA = GetTypeSortWeight(a.Token?.tokenType ?? CodeTokenType.Int);
                        int tB = GetTypeSortWeight(b.Token?.tokenType ?? CodeTokenType.Int);
                        if (tA != tB) return tA.CompareTo(tB);
                        int rA = GetRaritySortWeight(a.Token?.rarity ?? TokenRarity.Common);
                        int rB = GetRaritySortWeight(b.Token?.rarity ?? TokenRarity.Common);
                        if (rA != rB) return rA.CompareTo(rB);
                        return a.AcquisitionOrder.CompareTo(b.AcquisitionOrder);
                    });
                    break;
            }

            for (int i = 0; i < cards.Count; i++)
            {
                cards[i].transform.SetSiblingIndex(i);
            }
        }

        private static int GetRaritySortWeight(TokenRarity rarity) => rarity switch
        {
            TokenRarity.Legendary => 0,
            TokenRarity.Epic => 1,
            TokenRarity.Rare => 2,
            TokenRarity.Uncommon => 3,
            TokenRarity.Common => 4,
            _ => 5
        };

        private static int GetTypeSortWeight(CodeTokenType type) => type switch
        {
            CodeTokenType.Int => 0,
            CodeTokenType.Float => 1,
            CodeTokenType.Bool => 2,
            CodeTokenType.Action => 3,
            CodeTokenType.Condition => 4,
            CodeTokenType.Targeting => 5,
            CodeTokenType.Operator => 6,
            _ => 7
        };

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

        public bool AutoAssignToken(CodeTokenSO token)
        {
            if (token == null) return false;

            CodeSocketUI target = null;

            switch (token.tokenType)
            {
                case CodeTokenType.Operator:
                    if (conditionOpSocket != null && conditionOpSocket.AssignedToken == null)
                        target = conditionOpSocket;
                    else if (reactionConditionOpSocket != null && reactionConditionOpSocket.AssignedToken == null)
                        target = reactionConditionOpSocket;
                    else if (conditionOpSocket != null)
                        target = conditionOpSocket;
                    break;

                case CodeTokenType.Targeting:
                    if (TargetSocketUI != null && TargetSocketUI.AssignedToken == null)
                        target = TargetSocketUI;
                    else if (TargetSocketUI != null)
                        target = TargetSocketUI;
                    break;

                case CodeTokenType.Condition:
                    if (token is ConditionTokenSO condToken && condToken.subject == ConditionSubject.IncomingDamage)
                    {
                        if (reactionConditionSocket != null && reactionConditionSocket.AssignedToken == null)
                            target = reactionConditionSocket;
                        else if (reactionCondition2Socket != null && reactionCondition2Socket.AssignedToken == null)
                            target = reactionCondition2Socket;
                        else if (reactionConditionSocket != null)
                            target = reactionConditionSocket;
                    }
                    else
                    {
                        if (ConditionSocketUI != null && ConditionSocketUI.AssignedToken == null)
                            target = ConditionSocketUI;
                        else if (condition2Socket != null && condition2Socket.AssignedToken == null)
                            target = condition2Socket;
                        else if (reactionConditionSocket != null && reactionConditionSocket.AssignedToken == null)
                            target = reactionConditionSocket;
                        else if (reactionCondition2Socket != null && reactionCondition2Socket.AssignedToken == null)
                            target = reactionCondition2Socket;
                        else if (ConditionSocketUI != null)
                            target = ConditionSocketUI;
                    }
                    break;

                case CodeTokenType.Action:
                    if (ThenActionSocketUI != null && ThenActionSocketUI.AssignedToken == null)
                        target = ThenActionSocketUI;
                    else if (ElseActionSocketUI != null && ElseActionSocketUI.AssignedToken == null)
                        target = ElseActionSocketUI;
                    else if (reactionActionSocket != null && reactionActionSocket.AssignedToken == null)
                        target = reactionActionSocket;
                    else if (reactionElseActionSocket != null && reactionElseActionSocket.AssignedToken == null)
                        target = reactionElseActionSocket;
                    else if (ThenActionSocketUI != null)
                        target = ThenActionSocketUI;
                    break;

                case CodeTokenType.Float:
                    if (damageMultiplierSocket != null && damageMultiplierSocket.AssignedToken == null)
                        target = damageMultiplierSocket;
                    else if (critMultiplierSocket != null && critMultiplierSocket.AssignedToken == null)
                        target = critMultiplierSocket;
                    else if (damageMultiplierSocket != null)
                        target = damageMultiplierSocket;
                    break;

                case CodeTokenType.Int:
                    if (attackDamageSocket != null && attackDamageSocket.AssignedToken == null)
                        target = attackDamageSocket;
                    else if (critChancePercentSocket != null && critChancePercentSocket.AssignedToken == null)
                        target = critChancePercentSocket;
                    else if (evasionChancePercentSocket != null && evasionChancePercentSocket.AssignedToken == null)
                        target = evasionChancePercentSocket;
                    else if (damageReductionSocket != null && damageReductionSocket.AssignedToken == null)
                        target = damageReductionSocket;
                    else if (defendShieldSocket != null && defendShieldSocket.AssignedToken == null)
                        target = defendShieldSocket;
                    else if (baseShieldSocket != null && baseShieldSocket.AssignedToken == null)
                        target = baseShieldSocket;
                    else if (bleedDamageSocket != null && bleedDamageSocket.AssignedToken == null)
                        target = bleedDamageSocket;
                    else if (bleedDurationSocket != null && bleedDurationSocket.AssignedToken == null)
                        target = bleedDurationSocket;
                    else if (maxHealthSocket != null && maxHealthSocket.AssignedToken == null)
                        target = maxHealthSocket;
                    else if (attackDamageSocket != null)
                        target = attackDamageSocket;
                    break;

                case CodeTokenType.Bool:
                    if (applyBleedSocket != null && applyBleedSocket.AssignedToken == null)
                        target = applyBleedSocket;
                    else if (ConditionSocketUI != null && ConditionSocketUI.AssignedToken == null)
                        target = ConditionSocketUI;
                    else if (condition2Socket != null && condition2Socket.AssignedToken == null)
                        target = condition2Socket;
                    else if (reactionConditionSocket != null && reactionConditionSocket.AssignedToken == null)
                        target = reactionConditionSocket;
                    else if (reactionCondition2Socket != null && reactionCondition2Socket.AssignedToken == null)
                        target = reactionCondition2Socket;
                    else if (applyBleedSocket != null)
                        target = applyBleedSocket;
                    break;

                case CodeTokenType.Stance:
                    if (stanceSocket != null)
                        target = stanceSocket;
                    break;
            }

            if (target != null)
            {
                if (target.AssignedToken != null)
                {
                    AddTokenToInventory(target.AssignedToken);
                }
                target.AssignToken(token);
                ConsoleLogUI.Log($"[Auto-Slot] Assigned {token.GetFormattedCodeString()} to {target.SocketRole}.");
                return true;
            }

            ConsoleLogUI.Log($"<color=#E5C07B>[Warning] No compatible socket available for {token.tokenName}.</color>");
            return false;
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

        public int GetCritChancePercent()
        {
            if (critChancePercentSocket != null && critChancePercentSocket.AssignedToken != null)
            {
                return Mathf.Clamp(critChancePercentSocket.AssignedToken.intValue, 0, 100);
            }
            return 0;
        }

        public float GetCritMultiplier()
        {
            if (critMultiplierSocket != null && critMultiplierSocket.AssignedToken != null)
            {
                return critMultiplierSocket.AssignedToken.floatValue > 0f ? critMultiplierSocket.AssignedToken.floatValue : 1.1f;
            }
            return 1.1f;
        }

        public int GetEvasionChancePercent()
        {
            if (evasionChancePercentSocket != null && evasionChancePercentSocket.AssignedToken != null)
            {
                return Mathf.Clamp(evasionChancePercentSocket.AssignedToken.intValue, 0, 50);
            }
            return 0;
        }

        public int GetDamageReduction()
        {
            if (damageReductionSocket != null && damageReductionSocket.AssignedToken != null)
            {
                return Mathf.Max(0, damageReductionSocket.AssignedToken.intValue);
            }
            return 0;
        }

        [System.Obsolete("Use GetCritChancePercent()")]
        public float GetCritChance() => GetCritChancePercent() / 100f;

        [System.Obsolete("Use GetCritMultiplier()")]
        public int GetCritDamage() => 0;

        [System.Obsolete("Use GetEvasionChancePercent()")]
        public float GetEvasionChance() => GetEvasionChancePercent() / 100f;

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
                return Mathf.Max(0, bleedDamageSocket.AssignedToken.intValue);
            }
            return 0;
        }

        public int GetBleedDuration()
        {
            if (bleedDurationSocket != null && bleedDurationSocket.AssignedToken != null)
            {
                return Mathf.Max(0, bleedDurationSocket.AssignedToken.intValue);
            }
            return 0;
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
                    var row = req.socket != null ? req.socket.GetComponentInParent<EditorLineRowUI>(true) : null;
                    int lineNum = row != null ? row.lineNumber : 0;
                    string lineLocation = lineNum > 0 ? $"PlayerCombat.cs({lineNum}): error " : "";
                    string onLine = lineNum > 0 ? $" on line {lineNum}" : "";
                    ConsoleLogUI.Log($"<color=#FF5454>[Compiler Error] {lineLocation}CS0165: Use of unassigned variable '{req.name}'{onLine}. Please assign a token before compiling!</color>");
                    if (req.socket != null)
                    {
                        req.socket.TriggerUnassignedPulsingHighlight();
                        ScrollToSocket(req.socket);
                    }
                    return false;
                }
            }

            // If operator is slotted, condition2 MUST be assigned
            if (conditionOpSocket != null && conditionOpSocket.AssignedToken != null)
            {
                if (condition2Socket == null || condition2Socket.AssignedToken == null)
                {
                    int lineNum = conditionOpSocket.GetComponentInParent<EditorLineRowUI>(true)?.lineNumber ?? 38;
                    ConsoleLogUI.Log($"<color=#FF5454>[Compiler Error] PlayerCombat.cs({lineNum}): error CS1525: Invalid expression term. Operator '{conditionOpSocket.AssignedToken.GetFormattedCodeString()}' requires a right-hand condition.</color>");
                    if (condition2Socket != null)
                    {
                        condition2Socket.TriggerUnassignedPulsingHighlight();
                        ScrollToSocket(condition2Socket);
                    }
                    return false;
                }
            }

            if (reactionConditionOpSocket != null && reactionConditionOpSocket.AssignedToken != null)
            {
                if (reactionCondition2Socket == null || reactionCondition2Socket.AssignedToken == null)
                {
                    int lineNum = reactionConditionOpSocket.GetComponentInParent<EditorLineRowUI>(true)?.lineNumber ?? 45;
                    ConsoleLogUI.Log($"<color=#FF5454>[Compiler Error] PlayerCombat.cs({lineNum}): error CS1525: Invalid expression term. Operator '{reactionConditionOpSocket.AssignedToken.GetFormattedCodeString()}' requires a right-hand condition.</color>");
                    if (reactionCondition2Socket != null)
                    {
                        reactionCondition2Socket.TriggerUnassignedPulsingHighlight();
                        ScrollToSocket(reactionCondition2Socket);
                    }
                    return false;
                }
            }

            return true;
        }

        public bool ValidateCode() => ValidatePreBattle();

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

        public CodeTokenSO GetReactionConditionToken()
        {
            return reactionConditionSocket != null ? reactionConditionSocket.AssignedToken : null;
        }

        public ActionTokenSO GetReactionActionToken()
        {
            return reactionActionSocket != null ? reactionActionSocket.AssignedToken as ActionTokenSO : null;
        }

        public ActionTokenSO GetReactionElseActionToken()
        {
            return reactionElseActionSocket != null ? reactionElseActionSocket.AssignedToken as ActionTokenSO : null;
        }

        private bool EvaluateSingleSocket(CodeSocketUI socket, CombatContext context)
        {
            if (socket == null) return true;
            if (socket == reactionConditionSocket || socket == reactionCondition2Socket ||
                socket.SocketRole == CodeSocketRole.ReactionCondition || socket.SocketRole == CodeSocketRole.ReactionCondition2)
            {
                bool tookHealthDamage = context != null && context.IncomingDamage > 0;
                bool expectedBool = socket.AssignedToken != null ? socket.AssignedToken.boolValue : true;
                bool result = (tookHealthDamage == expectedBool);
                string dmgDesc = tookHealthDamage ? $"{context.IncomingDamage} HP lost" : "0 HP lost (shield absorbed all damage)";
                string branch = result ? "THEN" : "ELSE";
                ConsoleLogUI.Log($"[Reaction] Condition evaluated: Slotted '{expectedBool.ToString().ToLower()}', actual: {dmgDesc} -> Result: <b>{(result ? "<color=#98C379>TRUE (THEN)</color>" : "<color=#E06C75>FALSE (ELSE)</color>")}</b> -> Selected {branch} branch.");
                return result;
            }

            if (socket.AssignedToken == null) return true;
            if (socket.AssignedToken.tokenType == CodeTokenType.Bool)
            {
                return socket.AssignedToken.boolValue;
            }
            if (socket.AssignedToken is ConditionTokenSO cond) return cond.Evaluate(context);
            return true;
        }

        public bool EvaluateCondition(CombatContext context)
        {
            bool c1 = EvaluateSingleSocket(ConditionSocketUI, context);
            string c1Str = ConditionSocketUI?.AssignedToken != null ? ConditionSocketUI.AssignedToken.GetFormattedCodeString() : "true";

            // Single condition path
            if (conditionOpSocket == null || conditionOpSocket.AssignedToken == null)
            {
                return c1;
            }

            var opToken = conditionOpSocket.AssignedToken as OperatorTokenSO;
            bool c2 = EvaluateSingleSocket(condition2Socket, context);
            string c2Str = condition2Socket?.AssignedToken != null ? condition2Socket.AssignedToken.GetFormattedCodeString() : "true";

            bool finalResult = opToken != null ? opToken.Evaluate(c1, c2) : c1;
            string opSymbol = opToken != null ? opToken.GetFormattedCodeString() : "&&";
            ConsoleLogUI.Log($"[Eval] ({c1Str} [{c1}] {opSymbol} {c2Str} [{c2}]) => Overall: <b>{(finalResult ? "<color=#98C379>TRUE</color>" : "<color=#E06C75>FALSE</color>")}</b>");

            return finalResult;
        }

        public string GetConditionSyntax()
        {
            string c1Str = ConditionSocketUI?.AssignedToken != null ? ConditionSocketUI.AssignedToken.GetFormattedCodeString() : "true";
            if (conditionOpSocket != null && conditionOpSocket.AssignedToken != null)
            {
                string opStr = conditionOpSocket.AssignedToken.GetFormattedCodeString();
                string c2Str = condition2Socket?.AssignedToken != null ? condition2Socket.AssignedToken.GetFormattedCodeString() : "...";
                return $"{c1Str} {opStr} {c2Str}";
            }
            return c1Str;
        }

        public bool EvaluateReactionCondition(CombatContext context)
        {
            bool c1 = EvaluateSingleSocket(reactionConditionSocket, context);
            string c1Str = reactionConditionSocket?.AssignedToken != null ? reactionConditionSocket.AssignedToken.GetFormattedCodeString() : "true";

            // Single condition path
            if (reactionConditionOpSocket == null || reactionConditionOpSocket.AssignedToken == null)
            {
                return c1;
            }

            var opToken = reactionConditionOpSocket.AssignedToken as OperatorTokenSO;
            bool c2 = EvaluateSingleSocket(reactionCondition2Socket, context);
            string c2Str = reactionCondition2Socket?.AssignedToken != null ? reactionCondition2Socket.AssignedToken.GetFormattedCodeString() : "true";

            bool finalResult = opToken != null ? opToken.Evaluate(c1, c2) : c1;
            string opSymbol = opToken != null ? opToken.GetFormattedCodeString() : "&&";
            ConsoleLogUI.Log($"[Eval] ({c1Str} [{c1}] {opSymbol} {c2Str} [{c2}]) => Overall: <b>{(finalResult ? "<color=#98C379>TRUE</color>" : "<color=#E06C75>FALSE</color>")}</b>");

            return finalResult;
        }

        public string GetReactionConditionSyntax()
        {
            string c1Str = reactionConditionSocket?.AssignedToken != null ? reactionConditionSocket.AssignedToken.GetFormattedCodeString() : "true";
            if (reactionConditionOpSocket != null && reactionConditionOpSocket.AssignedToken != null)
            {
                string opStr = reactionConditionOpSocket.AssignedToken.GetFormattedCodeString();
                string c2Str = reactionCondition2Socket?.AssignedToken != null ? reactionCondition2Socket.AssignedToken.GetFormattedCodeString() : "...";
                return $"{c1Str} {opStr} {c2Str}";
            }
            return c1Str;
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

        public void EnsureShelfHeaderLabel()
        {
            if (shelfHeaderText == null)
            {
                var shelfTr = transform.Find("ShelfHeader");
                if (shelfTr != null) shelfHeaderText = shelfTr.GetComponent<TextMeshProUGUI>();
            }
            if (shelfHeaderText != null)
            {
                shelfHeaderText.text = "// Token Inventory Shelf (Click to inspect | Drag to socket)";
            }
        }

        public void ScrollToSocket(CodeSocketUI socket)
        {
            if (socket == null) return;
            var row = socket.GetComponentInParent<EditorLineRowUI>(true);
            if (row == null) return;
            ScrollToRow(row);
        }

        public void ScrollToRow(EditorLineRowUI row)
        {
            if (row == null) return;

            // If row is inside a collapsed method, unfold its parent method first!
            EnsureRowVisible(row);

            var scrolls = GetComponentsInChildren<ScrollRect>(true);
            ScrollRect editorScroll = null;
            foreach (var s in scrolls)
            {
                if (s != null && s.name.Contains("Editor")) { editorScroll = s; break; }
            }
            if (editorScroll == null || editorScroll.content == null || editorScroll.viewport == null) return;

            var content = editorScroll.content;
            var viewport = editorScroll.viewport;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);

            float scrollableHeight = content.rect.height - viewport.rect.height;
            if (scrollableHeight <= 0f) return;

            Vector3 localPos = content.InverseTransformPoint(row.transform.position);
            float distFromTop = -localPos.y;
            float targetScrollOffset = distFromTop - (viewport.rect.height / 2f);
            float clampedOffset = Mathf.Clamp(targetScrollOffset, 0f, scrollableHeight);
            float normPos = 1f - (clampedOffset / scrollableHeight);

            editorScroll.verticalNormalizedPosition = Mathf.Clamp01(normPos);
        }

        public void EnsureRowVisible(EditorLineRowUI targetRow)
        {
            if (targetRow == null) return;
            var scrolls = GetComponentsInChildren<ScrollRect>(true);
            ScrollRect editorScroll = null;
            foreach (var s in scrolls)
            {
                if (s != null && s.name.Contains("Editor")) { editorScroll = s; break; }
            }
            if (editorScroll == null || editorScroll.content == null) return;

            var allRows = editorScroll.content.GetComponentsInChildren<EditorLineRowUI>(true);
            foreach (var r in allRows)
            {
                if (r != null && r.ChildRows != null && r.ChildRows.Contains(targetRow))
                {
                    if (r.isFolded)
                    {
                        r.SetFolded(false);
                    }
                }
            }
        }

        public void SetupAllMethodFolding()
        {
            var scrolls = GetComponentsInChildren<ScrollRect>(true);
            ScrollRect editorScroll = null;
            foreach (var s in scrolls)
            {
                if (s != null && s.name.Contains("Editor")) { editorScroll = s; break; }
            }
            if (editorScroll == null || editorScroll.content == null) return;

            var content = editorScroll.content;
            var rows = content.GetComponentsInChildren<EditorLineRowUI>(true);
            if (rows == null || rows.Length == 0) return;

            for (int i = 0; i < rows.Length; i++)
            {
                var row = rows[i];
                string rowText = GetRowFullText(row);

                if (IsMajorMethodHeader(rowText))
                {
                    var childList = new List<EditorLineRowUI>();
                    int braceDepth = 0;
                    bool startedBraces = false;

                    for (int j = i + 1; j < rows.Length; j++)
                    {
                        var childRow = rows[j];
                        string cText = GetRowFullText(childRow);
                        string cleanChild = System.Text.RegularExpressions.Regex.Replace(cText, "<.*?>", string.Empty);

                        childList.Add(childRow);

                        if (cleanChild.Contains("{"))
                        {
                            braceDepth++;
                            startedBraces = true;
                        }
                        if (cleanChild.Contains("}"))
                        {
                            braceDepth--;
                        }

                        if (startedBraces && braceDepth <= 0)
                        {
                            break;
                        }
                    }

                    row.SetupFoldToggle(childList);
                }
            }
        }

        private bool IsMajorMethodHeader(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            string clean = System.Text.RegularExpressions.Regex.Replace(text, "<.*?>", string.Empty);
            return clean.Contains("void Attack") ||
                   clean.Contains("void Defend") ||
                   clean.Contains("void ExecuteTurn") ||
                   clean.Contains("void OnTakeDamage");
        }

        private string GetRowFullText(EditorLineRowUI row)
        {
            if (row == null) return "";
            var tmps = row.GetComponentsInChildren<TextMeshProUGUI>(true);
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (var t in tmps)
            {
                if (t.name != "LineNumber" && t != row.FoldToggleText)
                {
                    sb.Append(t.text).Append(" ");
                }
            }
            return sb.ToString();
        }

        private HashSet<string> loggedUnlocks = new HashSet<string>();

        public void UpdateRoomMethodUnlocks(int currentRoom)
        {
            SetupAllMethodFolding();

            EditorLineRowUI attackRow = FindMethodRow("void Attack");
            EditorLineRowUI defendRow = FindMethodRow("void Defend");
            EditorLineRowUI executeTurnRow = FindMethodRow("void ExecuteTurn");
            EditorLineRowUI reactionRow = FindMethodRow("void OnTakeDamage");

            if (currentRoom <= 1)
            {
                // Room 1: Only ExecuteTurn() is unlocked. Defend() is locked (🔒 Unlocks in Room 2), OnTakeDamage() is locked (🔒 Unlocks in Room 3). Helper methods (Attack) are folded.
                executeTurnRow?.SetLocked(false, null);
                executeTurnRow?.SetFolded(false);

                attackRow?.SetFolded(true);

                defendRow?.SetLocked(true, "Unlocks in Room 2");
                reactionRow?.SetLocked(true, "Unlocks in Room 3");
            }
            else if (currentRoom == 2)
            {
                // Room 2: Defend() unlocks!
                if (defendRow != null)
                {
                    bool wasLocked = defendRow.isLocked;
                    defendRow.SetLocked(false, null);
                    if (wasLocked && !loggedUnlocks.Contains("Defend"))
                    {
                        loggedUnlocks.Add("Defend");
                        ConsoleLogUI.Log("<color=#98C379>[Unlock] Defend() method unlocked in PlayerCombat.cs!</color>");
                    }
                }
                reactionRow?.SetLocked(true, "Unlocks in Room 3");
            }
            else // currentRoom >= 3
            {
                // Room 3+: Both Defend() and OnTakeDamage() are unlocked!
                if (defendRow != null)
                {
                    bool wasLocked = defendRow.isLocked;
                    defendRow.SetLocked(false, null);
                    if (wasLocked && !loggedUnlocks.Contains("Defend"))
                    {
                        loggedUnlocks.Add("Defend");
                        ConsoleLogUI.Log("<color=#98C379>[Unlock] Defend() method unlocked in PlayerCombat.cs!</color>");
                    }
                }

                if (reactionRow != null)
                {
                    bool wasLocked = reactionRow.isLocked;
                    reactionRow.SetLocked(false, null);
                    if (wasLocked && !loggedUnlocks.Contains("OnTakeDamage"))
                    {
                        loggedUnlocks.Add("OnTakeDamage");
                        ConsoleLogUI.Log("<color=#98C379>[Unlock] OnTakeDamage() reaction callback unlocked!</color>");
                    }
                }
            }
        }

        private EditorLineRowUI FindMethodRow(string methodSignature)
        {
            if (lineRows.Count == 0) CacheLineRows();
            foreach (var kvp in lineRows)
            {
                if (kvp.Value != null)
                {
                    string txt = GetRowFullText(kvp.Value);
                    string clean = System.Text.RegularExpressions.Regex.Replace(txt, "<.*?>", string.Empty);
                    if (clean.Contains(methodSignature)) return kvp.Value;
                }
            }

            var allRows = GetComponentsInChildren<EditorLineRowUI>(true);
            foreach (var r in allRows)
            {
                if (r != null)
                {
                    string txt = GetRowFullText(r);
                    string clean = System.Text.RegularExpressions.Regex.Replace(txt, "<.*?>", string.Empty);
                    if (clean.Contains(methodSignature)) return r;
                }
            }
            return null;
        }

        public void EnsureShelfDiscardSlot()
        {
            Transform panelTr = transform;
            Transform invScrollTr = panelTr.Find("InventoryScroll");
            if (invScrollTr == null) return;

            var invRt = invScrollTr.GetComponent<RectTransform>();
            if (invRt != null)
            {
                invRt.anchorMin = new Vector2(0.04f, 0.145f);
                invRt.anchorMax = new Vector2(0.815f, 0.35f);
                invRt.offsetMin = Vector2.zero;
                invRt.offsetMax = Vector2.zero;
            }

            Transform existingSlot = panelTr.Find("ShelfDiscardSlot");
            if (existingSlot == null)
            {
                GameObject slotObj = new GameObject("ShelfDiscardSlot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline), typeof(ShelfDiscardSlotUI));
                slotObj.transform.SetParent(panelTr, false);
                slotObj.transform.SetSiblingIndex(invScrollTr.GetSiblingIndex() + 1);

                var slotRt = slotObj.GetComponent<RectTransform>();
                slotRt.anchorMin = new Vector2(0.825f, 0.145f);
                slotRt.anchorMax = new Vector2(0.96f, 0.35f);
                slotRt.offsetMin = Vector2.zero;
                slotRt.offsetMax = Vector2.zero;

                GameObject textObj = new GameObject("PromptText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                textObj.transform.SetParent(slotObj.transform, false);
                var textRt = textObj.GetComponent<RectTransform>();
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.offsetMin = Vector2.zero;
                textRt.offsetMax = Vector2.zero;

                var discardUI = slotObj.GetComponent<ShelfDiscardSlotUI>();
                discardUI.Initialize();
            }
            else
            {
                var slotRt = existingSlot.GetComponent<RectTransform>();
                slotRt.anchorMin = new Vector2(0.825f, 0.145f);
                slotRt.anchorMax = new Vector2(0.96f, 0.35f);
                slotRt.offsetMin = Vector2.zero;
                slotRt.offsetMax = Vector2.zero;
                var discardUI = existingSlot.GetComponent<ShelfDiscardSlotUI>();
                if (discardUI != null) discardUI.Initialize();
            }
        }

        public void EnsureHighContrastScrollbars()
        {
            Color trackBgColor = new Color(0.118f, 0.133f, 0.169f, 1f); // #1E222B, 100% alpha
            Color handleColor = new Color(0.294f, 0.322f, 0.388f, 1f);  // #4B5263
            Color highlightColor = new Color(0.380f, 0.686f, 0.937f, 1f); // #61AFEF

            // 1. EditorScroll Vertical Scrollbar
            Transform editorScrollTr = transform.Find("EditorScroll");
            if (editorScrollTr != null)
            {
                var scrollRect = editorScrollTr.GetComponent<ScrollRect>();
                if (scrollRect != null)
                {
                    Scrollbar vScrollbar = scrollRect.verticalScrollbar;
                    if (vScrollbar == null)
                    {
                        var sbTr = editorScrollTr.Find("Scrollbar");
                        if (sbTr != null) vScrollbar = sbTr.GetComponent<Scrollbar>();
                    }

                    if (vScrollbar != null)
                    {
                        var trackImg = vScrollbar.GetComponent<Image>();
                        if (trackImg != null) trackImg.color = trackBgColor;

                        var handleImg = vScrollbar.handleRect?.GetComponent<Image>();
                        if (handleImg != null) handleImg.color = handleColor;

                        var colors = vScrollbar.colors;
                        colors.normalColor = handleColor;
                        colors.highlightedColor = highlightColor;
                        colors.pressedColor = highlightColor;
                        colors.selectedColor = highlightColor;
                        vScrollbar.colors = colors;

                        var sbRt = vScrollbar.GetComponent<RectTransform>();
                        if (sbRt != null && sbRt.sizeDelta.x < 10f)
                        {
                            sbRt.sizeDelta = new Vector2(10f, sbRt.sizeDelta.y);
                        }
                    }
                }
            }

            // 2. InventoryScroll Horizontal Scrollbar
            Transform invScrollTr = transform.Find("InventoryScroll");
            if (invScrollTr != null)
            {
                var scrollRect = invScrollTr.GetComponent<ScrollRect>();
                if (scrollRect != null)
                {
                    Scrollbar hScrollbar = scrollRect.horizontalScrollbar;
                    if (hScrollbar == null)
                    {
                        var sbTr = invScrollTr.Find("Scrollbar_Horizontal");
                        if (sbTr != null) hScrollbar = sbTr.GetComponent<Scrollbar>();
                    }

                    if (hScrollbar == null)
                    {
                        GameObject sbObj = new GameObject("Scrollbar_Horizontal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Scrollbar));
                        sbObj.transform.SetParent(invScrollTr, false);
                        var sbRt = sbObj.GetComponent<RectTransform>();
                        sbRt.anchorMin = new Vector2(0f, 0f);
                        sbRt.anchorMax = new Vector2(1f, 0f);
                        sbRt.pivot = new Vector2(0.5f, 0f);
                        sbRt.offsetMin = new Vector2(0f, 0f);
                        sbRt.offsetMax = new Vector2(0f, 10f);

                        var trackImg = sbObj.GetComponent<Image>();
                        trackImg.color = trackBgColor;

                        GameObject slidingArea = new GameObject("Sliding Area", typeof(RectTransform));
                        slidingArea.transform.SetParent(sbObj.transform, false);
                        var saRt = slidingArea.GetComponent<RectTransform>();
                        saRt.anchorMin = Vector2.zero;
                        saRt.anchorMax = Vector2.one;
                        saRt.offsetMin = Vector2.zero;
                        saRt.offsetMax = Vector2.zero;

                        GameObject handleObj = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                        handleObj.transform.SetParent(slidingArea.transform, false);
                        var handleRt = handleObj.GetComponent<RectTransform>();
                        handleRt.anchorMin = Vector2.zero;
                        handleRt.anchorMax = Vector2.one;
                        handleRt.offsetMin = Vector2.zero;
                        handleRt.offsetMax = Vector2.zero;

                        var handleImg = handleObj.GetComponent<Image>();
                        handleImg.color = handleColor;

                        hScrollbar = sbObj.GetComponent<Scrollbar>();
                        hScrollbar.direction = Scrollbar.Direction.LeftToRight;
                        hScrollbar.handleRect = handleRt;

                        var colors = hScrollbar.colors;
                        colors.normalColor = handleColor;
                        colors.highlightedColor = highlightColor;
                        colors.pressedColor = highlightColor;
                        colors.selectedColor = highlightColor;
                        hScrollbar.colors = colors;

                        scrollRect.horizontalScrollbar = hScrollbar;
                        scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
                    }
                    else
                    {
                        var trackImg = hScrollbar.GetComponent<Image>();
                        if (trackImg != null) trackImg.color = trackBgColor;

                        var handleImg = hScrollbar.handleRect?.GetComponent<Image>();
                        if (handleImg != null) handleImg.color = handleColor;

                        var colors = hScrollbar.colors;
                        colors.normalColor = handleColor;
                        colors.highlightedColor = highlightColor;
                        colors.pressedColor = highlightColor;
                        colors.selectedColor = highlightColor;
                        hScrollbar.colors = colors;
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
            EnsureInventoryScrollSensitivity();
            EnsureShelfHeaderLabel();
            SetupAllMethodFolding();

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
