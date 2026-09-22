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

        public void ShowRewardPrompt()
        {
            if (rootContainer != null) rootContainer.SetActive(true);
            if (rewardButtonsContainer != null)
            {
                foreach (Transform child in rewardButtonsContainer) Destroy(child.gameObject);
            }

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

            // Weighted random selection of up to 3 unique tokens
            int countToDraft = Mathf.Min(3, eligibleTokens.Count);
            List<CodeTokenSO> draftedTokens = new List<CodeTokenSO>();

            for (int draft = 0; draft < countToDraft; draft++)
            {
                float totalWeight = 0f;
                for (int i = 0; i < eligibleTokens.Count; i++)
                {
                    totalWeight += GetTokenWeight(eligibleTokens[i]);
                }

                if (totalWeight <= 0f) break;

                float roll = UnityEngine.Random.Range(0f, totalWeight);
                float accumulated = 0f;
                int chosenIndex = 0;

                for (int i = 0; i < eligibleTokens.Count; i++)
                {
                    accumulated += GetTokenWeight(eligibleTokens[i]);
                    if (roll <= accumulated)
                    {
                        chosenIndex = i;
                        break;
                    }
                }

                CodeTokenSO selected = eligibleTokens[chosenIndex];
                draftedTokens.Add(selected);
                eligibleTokens.RemoveAt(chosenIndex);
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
                    if (text != null)
                    {
                        string rarityHex = token.GetRarityHexColor();
                        text.text = $"<color={rarityHex}><b>[{token.rarity.ToString().ToUpper()}]</b></color>\n<b>Import {token.tokenName}</b>\n<size=85%><color=#4EC9B0>{token.GetFormattedCodeString()}</color></size>";
                    }

                    Image bgImg = btnObj.GetComponent<Image>();
                    if (bgImg != null)
                    {
                        Color rColor = token.GetRarityColor();
                        bgImg.color = new Color(
                            0.12f + rColor.r * 0.15f,
                            0.14f + rColor.g * 0.15f,
                            0.18f + rColor.b * 0.15f,
                            1f
                        );
                    }

                    Button btn = btnObj.GetComponent<Button>();
                    if (btn != null)
                    {
                        btn.onClick.AddListener(() =>
                        {
                            if (codeEditorUI != null) codeEditorUI.AddTokenToInventory(token);
                            if (rootContainer != null) rootContainer.SetActive(false);
                            if (CombatManager.Instance != null) CombatManager.Instance.AdvanceToNextRoom();
                        });
                    }
                }
            }
        }
    }
}
