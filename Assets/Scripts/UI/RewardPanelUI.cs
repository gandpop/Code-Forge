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
        [SerializeField] private GameObject rootContainer;
        [SerializeField] private Transform rewardButtonsContainer;
        [SerializeField] private GameObject rewardOptionPrefab;
        [SerializeField] private List<CodeTokenSO> globalTokenPool = new List<CodeTokenSO>();
        [SerializeField] private CodeEditorPanelUI codeEditorUI;

        public void ShowRewardPrompt()
        {
            if (rootContainer != null) rootContainer.SetActive(true);
            if (rewardButtonsContainer != null)
            {
                foreach (Transform child in rewardButtonsContainer) Destroy(child.gameObject);
            }

            if (globalTokenPool == null || globalTokenPool.Count == 0) return;

            int countToDraft = Mathf.Min(3, globalTokenPool.Count);
            List<CodeTokenSO> pool = new List<CodeTokenSO>(globalTokenPool);
            for (int i = 0; i < pool.Count; i++)
            {
                int rnd = UnityEngine.Random.Range(i, pool.Count);
                CodeTokenSO temp = pool[i];
                pool[i] = pool[rnd];
                pool[rnd] = temp;
            }

            for (int i = 0; i < countToDraft; i++)
            {
                CodeTokenSO token = pool[i];
                if (rewardOptionPrefab != null && rewardButtonsContainer != null)
                {
                    GameObject btnObj = Instantiate(rewardOptionPrefab, rewardButtonsContainer);
                    TextMeshProUGUI text = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                    if (text != null)
                    {
                        text.text = $"Import {token.tokenName}\n({token.GetFormattedCodeString()})";
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
