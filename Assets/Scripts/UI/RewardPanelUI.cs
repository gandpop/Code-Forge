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

            // Room progression unlock announcements
            if (roomIndex == 1)
            {
                ConsoleLogUI.Log("[Unlock] Clear Room 1 complete! Unlocked Section 1: Class Fields (damageMultiplier & baseShield)!");
            }
            else if (roomIndex == 2)
            {
                ConsoleLogUI.Log("[Unlock] Clear Room 2 complete! Unlocked Section 3: Event Callback OnTakeDamage(int incomingDamage)!");
            }
            else if (roomIndex >= 3)
            {
                ConsoleLogUI.Log("[Unlock] Clear Room 3 complete! Full Roguelike Architecture Unlocked!");
            }

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
                    rect.anchorMin = new Vector2(0.32f, 0.04f);
                    rect.anchorMax = new Vector2(0.68f, 0.14f);
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
                modalRect.anchorMin = new Vector2(0.12f, 0.16f);
                modalRect.anchorMax = new Vector2(0.88f, 0.84f);
                modalRect.sizeDelta = Vector2.zero;
                modalRect.anchoredPosition = Vector2.zero;
            }

            var containerRect = rewardButtonsContainer.GetComponent<RectTransform>();
            if (containerRect != null)
            {
                containerRect.anchorMin = new Vector2(0.04f, 0.17f);
                containerRect.anchorMax = new Vector2(0.96f, 0.88f);
                containerRect.sizeDelta = Vector2.zero;
                containerRect.anchoredPosition = Vector2.zero;
            }

            // Convert HorizontalLayoutGroup to GridLayoutGroup for a neat 3x2 card grid
            var hlg = rewardButtonsContainer.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null) DestroyImmediate(hlg);

            var glg = rewardButtonsContainer.GetComponent<GridLayoutGroup>();
            if (glg == null) glg = rewardButtonsContainer.gameObject.AddComponent<GridLayoutGroup>();

            glg.cellSize = new Vector2(230f, 96f);
            glg.spacing = new Vector2(16f, 14f);
            glg.startCorner = GridLayoutGroup.Corner.UpperLeft;
            glg.startAxis = GridLayoutGroup.Axis.Horizontal;
            glg.childAlignment = TextAnchor.MiddleCenter;
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 3;
        }
    }
}
