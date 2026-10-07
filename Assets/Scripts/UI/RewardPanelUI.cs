using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CodeForge.Combat;
using CodeForge.Data;

namespace CodeForge.UI
{
    public class RewardPanelUI : MonoBehaviour
    {
        [Header("UI Containers")]
        [SerializeField] private GameObject rootContainer;
        [SerializeField] private Transform rewardButtonsContainer;
        [SerializeField] private GameObject rewardOptionPrefab;
        [SerializeField] private List<CodeTokenSO> globalTokenPool = new List<CodeTokenSO>();
        [SerializeField] private CodeEditorPanelUI codeEditorUI;

        [Header("Multi-Select Draft Settings")]
        [SerializeField] private int totalOfferedRewards = 6;
        [SerializeField] private int maxSelectableRewards = 2;
        [SerializeField] private Button confirmRewardsButton;
        [SerializeField] private TextMeshProUGUI confirmButtonText;

        [Header("Global Rarity Spawn Chances (0 - 100)")]
        [Tooltip("Base spawn weight for Common tokens")]
        [Range(0f, 100f)] [SerializeField] private float commonChance = 100f;
        [Tooltip("Base spawn weight for Uncommon tokens")]
        [Range(0f, 100f)] [SerializeField] private float uncommonChance = 60f;
        [Tooltip("Base spawn weight for Rare tokens")]
        [Range(0f, 100f)] [SerializeField] private float rareChance = 30f;
        [Tooltip("Base spawn weight for Epic tokens")]
        [Range(0f, 100f)] [SerializeField] private float epicChance = 15f;
        [Tooltip("Base spawn weight for Legendary tokens")]
        [Range(0f, 100f)] [SerializeField] private float legendaryChance = 5f;

        private List<CodeTokenSO> selectedTokens = new List<CodeTokenSO>();
        private Dictionary<CodeTokenSO, (GameObject cardObj, Outline outline, TextMeshProUGUI text, string originalText, Color originalColor)> cardVisuals =
            new Dictionary<CodeTokenSO, (GameObject, Outline, TextMeshProUGUI, string, Color)>();
        private bool isConfirming = false;

        public List<CodeTokenSO> GlobalTokenPool => globalTokenPool;
        public List<CodeTokenSO> SelectedTokens => selectedTokens;

        private void Awake()
        {
            EnsureGlobalTokenPool();
            EnsureConfirmButton();
            EnsureContainerLayout();
            Hide();
        }

        private void OnEnable()
        {
            EnsureConfirmButton();
            UpdateConfirmButtonState();
        }

        private void Start()
        {
            if (CombatManager.Instance != null && CombatManager.Instance.currentPhase == GamePhase.Planning)
            {
                Hide();
            }
        }

        public void Hide()
        {
            isConfirming = false;
            if (warningDialogObj != null) warningDialogObj.SetActive(false);
            if (rootContainer != null && rootContainer != gameObject)
            {
                rootContainer.SetActive(false);
            }
            gameObject.SetActive(false);
        }

        public void EnsureGlobalTokenPool()
        {
            if (globalTokenPool == null) globalTokenPool = new List<CodeTokenSO>();
            globalTokenPool.RemoveAll(t => t == null);

            var allTokens = Resources.FindObjectsOfTypeAll<CodeTokenSO>();
            foreach (var t in allTokens)
            {
                if (t != null && t.canSpawnAsReward && !globalTokenPool.Contains(t))
                {
#if UNITY_EDITOR
                    if (UnityEditor.EditorUtility.IsPersistent(t))
#endif
                    {
                        globalTokenPool.Add(t);
                    }
                }
            }
        }

        public float GetTokenWeight(CodeTokenSO token)
        {
            if (token == null || !token.canSpawnAsReward) return 0f;
            if (token.overrideSpawnChance) return token.customSpawnChance;

            return token.rarity switch
            {
                TokenRarity.Common => commonChance,
                TokenRarity.Uncommon => uncommonChance,
                TokenRarity.Rare => rareChance,
                TokenRarity.Epic => epicChance,
                TokenRarity.Legendary => legendaryChance,
                _ => 100f
            };
        }

        public CodeTokenSO DrawWeightedToken(List<CodeTokenSO> pool)
        {
            if (pool == null || pool.Count == 0) return null;

            float totalWeight = 0f;
            for (int i = 0; i < pool.Count; i++)
            {
                totalWeight += GetTokenWeight(pool[i]);
            }

            if (totalWeight <= 0f) return null;

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float accumulated = 0f;

            for (int i = 0; i < pool.Count; i++)
            {
                accumulated += GetTokenWeight(pool[i]);
                if (roll <= accumulated)
                {
                    CodeTokenSO selected = pool[i];
                    pool.RemoveAll(t => t == selected);
                    return selected;
                }
            }

            CodeTokenSO last = pool[pool.Count - 1];
            pool.RemoveAll(t => t == last);
            return last;
        }

        public void ShowRewardPrompt()
        {
            ShowRewardPrompt(1);
        }

        public void ShowRewardPrompt(int roomIndex)
        {
            isConfirming = false;
            gameObject.SetActive(true);
            if (rootContainer != null) rootContainer.SetActive(true);
            transform.SetAsLastSibling();

            var title = transform.Find("RewardTitle")?.GetComponent<TextMeshProUGUI>();
            if (title != null)
            {
                title.text = "<color=#FFD700><b>Refactor Complete! Room Cleared.</b></color>\n<size=70%><color=#D4D4D4>Select up to 2 Code Tokens to import into your script inventory:</color></size>";
            }

            selectedTokens.Clear();
            cardVisuals.Clear();

            EnsureConfirmButton();
            EnsureContainerLayout();
            UpdateBuildPeekDisplay();

            if (rewardButtonsContainer != null)
            {
                for (int i = rewardButtonsContainer.childCount - 1; i >= 0; i--)
                {
                    var child = rewardButtonsContainer.GetChild(i);
                    child.SetParent(null);
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        DestroyImmediate(child.gameObject);
                    }
                    else
                    {
                        Destroy(child.gameObject);
                    }
#else
                    Destroy(child.gameObject);
#endif
                }
            }

            EnsureGlobalTokenPool();
            if (globalTokenPool == null || globalTokenPool.Count == 0) return;

            // Filter out tokens with 0 weight (0 = no chance of spawning)
            List<CodeTokenSO> eligibleTokens = new List<CodeTokenSO>();
            for (int i = 0; i < globalTokenPool.Count; i++)
            {
                var token = globalTokenPool[i];
                if (token != null && GetTokenWeight(token) > 0f)
                {
                    eligibleTokens.Add(token);
                }
            }

            if (eligibleTokens.Count == 0) return;

            List<CodeTokenSO> draftedTokens = new List<CodeTokenSO>();

            // Room 1 onboarding guard: draw 3 different categories (Stat, Logic, Action) with weighted random selection
            if (roomIndex == 1)
            {
                // Category 1: Stat (Float or Int)
                var statPool = eligibleTokens.FindAll(t => t.tokenType == CodeTokenType.Float || t.tokenType == CodeTokenType.Int);
                var statToken = DrawWeightedToken(statPool);
                if (statToken != null)
                {
                    draftedTokens.Add(statToken);
                    eligibleTokens.Remove(statToken);
                }

                // Category 2: Logic (Condition, Targeting, or Bool)
                var logicPool = eligibleTokens.FindAll(t => t.tokenType == CodeTokenType.Condition || t.tokenType == CodeTokenType.Targeting || t.tokenType == CodeTokenType.Bool);
                var logicToken = DrawWeightedToken(logicPool);
                if (logicToken != null)
                {
                    draftedTokens.Add(logicToken);
                    eligibleTokens.Remove(logicToken);
                }

                // Category 3: Action
                var actionPool = eligibleTokens.FindAll(t => t.tokenType == CodeTokenType.Action);
                var actionToken = DrawWeightedToken(actionPool);
                if (actionToken != null)
                {
                    draftedTokens.Add(actionToken);
                    eligibleTokens.Remove(actionToken);
                }
            }

            // Fill remaining slots up to totalOfferedRewards (6) using universal weighted random draft
            while (draftedTokens.Count < totalOfferedRewards && eligibleTokens.Count > 0)
            {
                CodeTokenSO token = DrawWeightedToken(eligibleTokens);
                if (token != null)
                {
                    draftedTokens.Add(token);
                }
                else
                {
                    break;
                }
            }

            // Instantiate reward buttons
            for (int i = 0; i < draftedTokens.Count; i++)
            {
                CodeTokenSO token = draftedTokens[i];
                if (rewardOptionPrefab != null && rewardButtonsContainer != null)
                {
                    GameObject btnObj = Instantiate(rewardOptionPrefab, rewardButtonsContainer);
                    btnObj.name = $"Reward_{token.name}";

                    TextMeshProUGUI text = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                    string baseText = "";
                    if (text != null)
                    {
                        string rarityHex = token.GetRarityHexColor();
                        string displayName = token.tokenType switch
                        {
                            CodeTokenType.Float => "Float",
                            CodeTokenType.Int => "Int",
                            CodeTokenType.Bool => "Bool",
                            _ => token.tokenName
                        };
                        baseText = $"<color={rarityHex}><b>[{token.rarity.ToString().ToUpper()}]</b></color>\n<b>{displayName}</b>\n<size=85%><color=#4EC9B0>{token.GetFormattedCodeString()}</color></size>";
                        text.text = baseText;
                    }

                    Image bgImg = btnObj.GetComponent<Image>();
                    Color baseBgColor = new Color(0.15f, 0.18f, 0.22f, 1f);
                    if (bgImg != null)
                    {
                        Color rColor = token.GetRarityColor();
                        baseBgColor = new Color(
                            0.12f + rColor.r * 0.15f,
                            0.14f + rColor.g * 0.15f,
                            0.18f + rColor.b * 0.15f,
                            1f
                        );
                        bgImg.color = baseBgColor;
                    }

                    var outline = btnObj.GetComponent<Outline>();
                    if (outline == null) outline = btnObj.AddComponent<Outline>();
                    outline.effectColor = new Color(0.3f, 0.4f, 0.5f, 0.5f);
                    outline.effectDistance = new Vector2(2f, -2f);
                    outline.enabled = false;

                    cardVisuals[token] = (btnObj, outline, text, baseText, baseBgColor);

                    Button btn = btnObj.GetComponent<Button>();
                    if (btn != null)
                    {
                        btn.onClick.AddListener(() =>
                        {
                            ToggleTokenSelection(token);
                        });
                    }
                }
            }

            UpdateConfirmButtonState();
        }

        public void ToggleTokenSelection(CodeTokenSO token)
        {
            if (token == null) return;

            if (selectedTokens.Contains(token))
            {
                selectedTokens.Remove(token);
                SetCardSelectedVisual(token, false);
            }
            else
            {
                if (selectedTokens.Count >= maxSelectableRewards)
                {
                    ConsoleLogUI.Log($"<color=#E5C07B>[Reward] You can select up to {maxSelectableRewards} rewards. Click a selected card to unselect it first.</color>");
                    return;
                }

                selectedTokens.Add(token);
                SetCardSelectedVisual(token, true);
            }

            UpdateConfirmButtonState();
        }

        private void SetCardSelectedVisual(CodeTokenSO token, bool isSelected)
        {
            if (!cardVisuals.TryGetValue(token, out var data)) return;

            if (data.cardObj != null)
            {
                var bgImg = data.cardObj.GetComponent<Image>();
                if (bgImg != null)
                {
                    bgImg.color = isSelected ? new Color(0.12f, 0.35f, 0.20f, 1f) : data.originalColor;
                }
            }

            if (data.outline != null)
            {
                data.outline.enabled = isSelected;
                data.outline.effectColor = new Color(1.0f, 0.84f, 0.0f, 1f); // Glowing Gold
                data.outline.effectDistance = new Vector2(4f, -4f);
            }

            if (data.text != null)
            {
                if (isSelected)
                {
                    data.text.text = $"<color=#FFD700><b>[SELECTED]</b></color>\n{data.originalText}";
                }
                else
                {
                    data.text.text = data.originalText;
                }
            }
        }

        private void UpdateConfirmButtonState()
        {
            if (confirmButtonText != null)
            {
                confirmButtonText.text = $"Accept Rewards ({selectedTokens.Count}/{maxSelectableRewards})";
            }

            if (confirmRewardsButton != null)
            {
                confirmRewardsButton.interactable = selectedTokens.Count > 0;
            }
        }

        public void ConfirmSelection()
        {
            if (isConfirming) return;

            if (selectedTokens.Count == 0)
            {
                ConsoleLogUI.Log("<color=#E5C07B>[Reward] Please select at least 1 token before accepting!</color>");
                return;
            }

            if (selectedTokens.Count < maxSelectableRewards)
            {
                int remaining = maxSelectableRewards - selectedTokens.Count;
                ShowUnspentRewardWarning(remaining);
                return;
            }

            ProceedDraftSelection();
        }

        public void ProceedDraftSelection()
        {
            if (warningDialogObj != null) warningDialogObj.SetActive(false);
            isConfirming = true;

            var tokensToDraft = new List<CodeTokenSO>(selectedTokens);
            selectedTokens.Clear();

            if (codeEditorUI == null)
            {
                codeEditorUI = FindFirstObjectByType<CodeEditorPanelUI>(FindObjectsInactive.Include);
            }

            foreach (var token in tokensToDraft)
            {
                if (token != null && codeEditorUI != null)
                {
                    codeEditorUI.AddTokenToInventory(token);
                }
            }

            ConsoleLogUI.Log($"[Reward] Successfully drafted {tokensToDraft.Count} token(s) into inventory!");

            Hide();
            if (CombatManager.Instance != null) CombatManager.Instance.AdvanceToNextRoom();
        }

        private GameObject warningDialogObj;
        private TextMeshProUGUI warningMessageText;

        public void ShowUnspentRewardWarning(int remaining)
        {
            if (warningDialogObj == null)
            {
                GameObject dlg = new GameObject("RewardWarningDialog", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
                dlg.transform.SetParent(transform, false);

                var rt = dlg.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.26f, 0.32f);
                rt.anchorMax = new Vector2(0.74f, 0.68f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                var img = dlg.GetComponent<Image>();
                img.color = new Color(0.12f, 0.12f, 0.16f, 0.98f);

                var outline = dlg.GetComponent<Outline>();
                outline.effectColor = new Color(0.90f, 0.75f, 0.30f, 0.95f);
                outline.effectDistance = new Vector2(3f, -3f);

                GameObject headObj = new GameObject("Header", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                headObj.transform.SetParent(dlg.transform, false);
                var headRt = headObj.GetComponent<RectTransform>();
                headRt.anchorMin = new Vector2(0.05f, 0.68f);
                headRt.anchorMax = new Vector2(0.95f, 0.92f);
                headRt.offsetMin = Vector2.zero;
                headRt.offsetMax = Vector2.zero;
                var headTmp = headObj.GetComponent<TextMeshProUGUI>();
                headTmp.text = "<color=#FFCC00><b>[!] UNSPENT REWARD CHOICES</b></color>";
                headTmp.fontSize = 17f;
                headTmp.fontStyle = FontStyles.Bold;
                headTmp.alignment = TextAlignmentOptions.Center;

                GameObject msgObj = new GameObject("Message", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                msgObj.transform.SetParent(dlg.transform, false);
                var msgRt = msgObj.GetComponent<RectTransform>();
                msgRt.anchorMin = new Vector2(0.06f, 0.36f);
                msgRt.anchorMax = new Vector2(0.94f, 0.66f);
                msgRt.offsetMin = Vector2.zero;
                msgRt.offsetMax = Vector2.zero;
                warningMessageText = msgObj.GetComponent<TextMeshProUGUI>();
                warningMessageText.fontSize = 13.5f;
                warningMessageText.alignment = TextAlignmentOptions.Center;

                GameObject btnDraftObj = new GameObject("DraftMoreButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                btnDraftObj.transform.SetParent(dlg.transform, false);
                var bdRt = btnDraftObj.GetComponent<RectTransform>();
                bdRt.anchorMin = new Vector2(0.10f, 0.10f);
                bdRt.anchorMax = new Vector2(0.46f, 0.30f);
                bdRt.offsetMin = Vector2.zero;
                bdRt.offsetMax = Vector2.zero;
                btnDraftObj.GetComponent<Image>().color = new Color(0.20f, 0.38f, 0.58f, 1f);
                var btnDraft = btnDraftObj.GetComponent<Button>();
                btnDraft.onClick.AddListener(() =>
                {
                    dlg.SetActive(false);
                });

                GameObject bdTxtObj = new GameObject("Txt", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                bdTxtObj.transform.SetParent(btnDraftObj.transform, false);
                var bdtRt = bdTxtObj.GetComponent<RectTransform>();
                bdtRt.anchorMin = Vector2.zero;
                bdtRt.anchorMax = Vector2.one;
                bdtRt.offsetMin = Vector2.zero;
                bdtRt.offsetMax = Vector2.zero;
                var bdtTmp = bdTxtObj.GetComponent<TextMeshProUGUI>();
                bdtTmp.text = "Draft More";
                bdtTmp.fontSize = 13f;
                bdtTmp.fontStyle = FontStyles.Bold;
                bdtTmp.alignment = TextAlignmentOptions.Center;

                GameObject btnConfirmObj = new GameObject("ConfirmAnywayButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                btnConfirmObj.transform.SetParent(dlg.transform, false);
                var bcRt = btnConfirmObj.GetComponent<RectTransform>();
                bcRt.anchorMin = new Vector2(0.54f, 0.10f);
                bcRt.anchorMax = new Vector2(0.90f, 0.30f);
                bcRt.offsetMin = Vector2.zero;
                bcRt.offsetMax = Vector2.zero;
                btnConfirmObj.GetComponent<Image>().color = new Color(0.55f, 0.32f, 0.20f, 1f);
                var btnConfirm = btnConfirmObj.GetComponent<Button>();
                btnConfirm.onClick.AddListener(() =>
                {
                    dlg.SetActive(false);
                    ProceedDraftSelection();
                });

                GameObject bcTxtObj = new GameObject("Txt", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                bcTxtObj.transform.SetParent(btnConfirmObj.transform, false);
                var bctRt = bcTxtObj.GetComponent<RectTransform>();
                bctRt.anchorMin = Vector2.zero;
                bctRt.anchorMax = Vector2.one;
                bctRt.offsetMin = Vector2.zero;
                bctRt.offsetMax = Vector2.zero;
                var bctTmp = bcTxtObj.GetComponent<TextMeshProUGUI>();
                bctTmp.text = "Confirm Anyway";
                bctTmp.fontSize = 13f;
                bctTmp.fontStyle = FontStyles.Bold;
                bctTmp.alignment = TextAlignmentOptions.Center;

                warningDialogObj = dlg;
            }

            if (warningMessageText != null)
            {
                warningMessageText.text = $"You still have <color=#FFD700><b>{remaining}</b></color> reward choice(s) left!\nAre you sure you want to finish drafting?";
            }

            warningDialogObj.SetActive(true);
            warningDialogObj.transform.SetAsLastSibling();
        }

        [Header("Build Peek View")]
        [SerializeField] private GameObject buildPeekPanelObj;
        [SerializeField] private TextMeshProUGUI buildPeekText;

        public void EnsureBuildPeekPanel()
        {
            if (buildPeekPanelObj != null && buildPeekText != null) return;

            Transform existing = transform.Find("BuildPeekPanel");
            if (existing != null)
            {
                buildPeekPanelObj = existing.gameObject;
                buildPeekText = existing.GetComponentInChildren<TextMeshProUGUI>();
                return;
            }

            GameObject panelObj = new GameObject("BuildPeekPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
            panelObj.transform.SetParent(transform, false);

            var rt = panelObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.68f, 0.17f);
            rt.anchorMax = new Vector2(0.96f, 0.88f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = panelObj.GetComponent<Image>();
            img.color = new Color(0.10f, 0.12f, 0.16f, 0.96f);

            var outline = panelObj.GetComponent<Outline>();
            outline.effectColor = new Color(0.25f, 0.30f, 0.38f, 0.8f);
            outline.effectDistance = new Vector2(2f, -2f);

            GameObject textObj = new GameObject("PeekText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(panelObj.transform, false);
            var textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0.06f, 0.04f);
            textRt.anchorMax = new Vector2(0.94f, 0.96f);
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            buildPeekText = textObj.GetComponent<TextMeshProUGUI>();
            buildPeekText.fontSize = 11.5f;
            buildPeekText.color = new Color(0.85f, 0.88f, 0.92f, 1f);
            buildPeekText.richText = true;
            buildPeekText.alignment = TextAlignmentOptions.TopLeft;

            buildPeekPanelObj = panelObj;
        }

        public void UpdateBuildPeekDisplay()
        {
            EnsureBuildPeekPanel();
            if (buildPeekText == null) return;

            if (codeEditorUI == null)
            {
                codeEditorUI = FindFirstObjectByType<CodeEditorPanelUI>(FindObjectsInactive.Include);
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("<color=#61AFEF><b>// Current Build & Shelf</b></color>");
            sb.AppendLine("<size=40%> </size>");
            sb.AppendLine("<color=#E5C07B><b>Equipped Sockets:</b></color>");

            if (codeEditorUI != null)
            {
                string maxHp = codeEditorUI.MaxHealthSocketUI?.AssignedToken?.GetFormattedCodeString() ?? "20 HP";
                string target = codeEditorUI.TargetSocketUI?.AssignedToken?.GetFormattedCodeString() ?? "Closest()";
                string cond = codeEditorUI.ConditionSocketUI?.AssignedToken?.GetFormattedCodeString() ?? "true";
                string thenAct = codeEditorUI.ThenActionSocketUI?.AssignedToken?.GetFormattedCodeString() ?? "Attack()";
                string elseAct = codeEditorUI.ElseActionSocketUI?.AssignedToken?.GetFormattedCodeString() ?? "Attack()";
                string defend = codeEditorUI.DefendShieldSocketUI?.AssignedToken?.GetFormattedCodeString() ?? "Shield(5)";
                string reaction = codeEditorUI.ReactionActionSocketUI?.AssignedToken?.GetFormattedCodeString() ?? "None";

                sb.AppendLine($"- <color=#ABB2BF>Max HP:</color> <color=#98C379>{maxHp}</color>");
                sb.AppendLine($"- <color=#ABB2BF>Target:</color> <color=#C586C0>{target}</color>");
                sb.AppendLine($"- <color=#ABB2BF>Condition:</color> <color=#61AFEF>{cond}</color>");
                sb.AppendLine($"- <color=#ABB2BF>If True:</color> <color=#DCDCAA>{thenAct}</color>");
                sb.AppendLine($"- <color=#ABB2BF>Else:</color> <color=#DCDCAA>{elseAct}</color>");
                sb.AppendLine($"- <color=#ABB2BF>Defend:</color> <color=#4EC9B0>{defend}</color>");
                sb.AppendLine($"- <color=#ABB2BF>Reaction:</color> <color=#E06C75>{reaction}</color>");
                sb.AppendLine("<size=40%> </size>");

                var shelfTokens = new List<string>();
                var cards = codeEditorUI.GetComponentsInChildren<DraggableTokenCardUI>(true);
                foreach (var c in cards)
                {
                    if (c != null && c.Token != null && c.transform.parent != null && c.transform.parent.name.Contains("Content"))
                    {
                        shelfTokens.Add(c.Token.tokenName);
                    }
                }

                sb.AppendLine($"<color=#98C379><b>Shelf Inventory ({shelfTokens.Count}):</b></color>");
                if (shelfTokens.Count == 0)
                {
                    sb.AppendLine("<color=#5C6370><i>(Shelf is currently empty)</i></color>");
                }
                else
                {
                    for (int i = 0; i < shelfTokens.Count && i < 6; i++)
                    {
                        sb.AppendLine($"- <color=#ABB2BF>{shelfTokens[i]}</color>");
                    }
                    if (shelfTokens.Count > 6)
                    {
                        sb.AppendLine($"<color=#5C6370><i>+ {shelfTokens.Count - 6} more...</i></color>");
                    }
                }
            }
            else
            {
                sb.AppendLine("<color=#5C6370><i>No editor data found</i></color>");
            }

            buildPeekText.text = sb.ToString();
        }

        public void EnsureConfirmButton()
        {
            if (confirmRewardsButton == null)
            {
                var existingBtn = transform.Find("ConfirmRewardsButton");
                if (existingBtn != null)
                {
                    confirmRewardsButton = existingBtn.GetComponent<Button>();
                    confirmButtonText = existingBtn.GetComponentInChildren<TextMeshProUGUI>();
                }
                else
                {
                    // Create Confirm Rewards button at bottom of RewardModal
                    GameObject btnObj = new GameObject("ConfirmRewardsButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                    btnObj.transform.SetParent(transform, false);

                    var rect = btnObj.GetComponent<RectTransform>();
                    rect.anchorMin = new Vector2(0.20f, 0.04f);
                    rect.anchorMax = new Vector2(0.50f, 0.14f);
                    rect.sizeDelta = Vector2.zero;
                    rect.anchoredPosition = Vector2.zero;

                    var img = btnObj.GetComponent<Image>();
                    img.color = new Color(0.18f, 0.45f, 0.28f, 1f); // Vibrant Emerald Green

                    confirmRewardsButton = btnObj.GetComponent<Button>();
                    var colors = confirmRewardsButton.colors;
                    colors.highlightedColor = new Color(0.25f, 0.60f, 0.35f, 1f);
                    colors.pressedColor = new Color(0.12f, 0.35f, 0.20f, 1f);
                    colors.disabledColor = new Color(0.25f, 0.28f, 0.32f, 0.5f);
                    confirmRewardsButton.colors = colors;

                    var textObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                    textObj.transform.SetParent(btnObj.transform, false);
                    var textRect = textObj.GetComponent<RectTransform>();
                    textRect.anchorMin = Vector2.zero;
                    textRect.anchorMax = Vector2.one;
                    textRect.sizeDelta = Vector2.zero;
                    textRect.anchoredPosition = Vector2.zero;

                    confirmButtonText = textObj.GetComponent<TextMeshProUGUI>();
                    confirmButtonText.fontSize = 16f;
                    confirmButtonText.fontStyle = FontStyles.Bold;
                    confirmButtonText.alignment = TextAlignmentOptions.Center;
                    confirmButtonText.color = Color.white;
                    confirmButtonText.text = $"Accept Rewards (0/{maxSelectableRewards})";
                }
            }

            if (confirmButtonText == null && confirmRewardsButton != null)
            {
                confirmButtonText = confirmRewardsButton.GetComponentInChildren<TextMeshProUGUI>();
            }

            if (confirmButtonText != null)
            {
                confirmButtonText.raycastTarget = false;
            }

            if (confirmRewardsButton != null)
            {
                confirmRewardsButton.onClick.RemoveAllListeners();
                confirmRewardsButton.onClick.AddListener(ConfirmSelection);
            }
        }

        public void EnsureContainerLayout()
        {
            if (rewardButtonsContainer == null) return;

            // Expand modal slightly if needed to accommodate 6 cards comfortably
            var modalRect = GetComponent<RectTransform>();
            if (modalRect != null)
            {
                modalRect.anchorMin = new Vector2(0.10f, 0.14f);
                modalRect.anchorMax = new Vector2(0.90f, 0.86f);
                modalRect.sizeDelta = Vector2.zero;
                modalRect.anchoredPosition = Vector2.zero;
            }

            var containerRect = rewardButtonsContainer.GetComponent<RectTransform>();
            if (containerRect != null)
            {
                containerRect.anchorMin = new Vector2(0.04f, 0.17f);
                containerRect.anchorMax = new Vector2(0.66f, 0.88f);
                containerRect.sizeDelta = Vector2.zero;
                containerRect.anchoredPosition = Vector2.zero;
            }

            // Convert HorizontalLayoutGroup to GridLayoutGroup for a neat 3x2 card grid
            var hlg = rewardButtonsContainer.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null) DestroyImmediate(hlg);

            var glg = rewardButtonsContainer.GetComponent<GridLayoutGroup>();
            if (glg == null) glg = rewardButtonsContainer.gameObject.AddComponent<GridLayoutGroup>();

            glg.cellSize = new Vector2(200f, 96f);
            glg.spacing = new Vector2(14f, 14f);
            glg.startCorner = GridLayoutGroup.Corner.UpperLeft;
            glg.startAxis = GridLayoutGroup.Axis.Horizontal;
            glg.childAlignment = TextAnchor.MiddleCenter;
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 3;
        }
    }
}
