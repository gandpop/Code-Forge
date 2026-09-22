using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeForge.Data;
using CodeForge.UI;

namespace CodeForge.Combat
{
    public enum GamePhase { Planning, Running, Victory, Defeat }

    public class CombatManager : MonoBehaviour
    {
        public static CombatManager Instance { get; private set; }

        [Header("Phase")]
        public GamePhase currentPhase = GamePhase.Planning;

        [Header("Entity References")]
        [SerializeField] private PlayerCombatController player;
        [SerializeField] private Transform enemySpawnContainer;
        [SerializeField] private GameObject enemyPrefab;

        [Header("UI Controllers")]
        [SerializeField] private CodeEditorPanelUI codeEditorUI;
        [SerializeField] private RewardPanelUI rewardPanelUI;

        private List<EnemyEntity> activeEnemies = new List<EnemyEntity>();
        private int currentRoomIndex = 1;
        private Coroutine combatCoroutine;

        public event Action<GamePhase> OnPhaseChanged;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            Application.runInBackground = true;
        }

        private void Start()
        {
            EnterPlanningPhase();
        }

        public void EnterPlanningPhase()
        {
            if (combatCoroutine != null)
            {
                StopCoroutine(combatCoroutine);
                combatCoroutine = null;
            }

            SetPhase(GamePhase.Planning);
            SpawnRoomEnemies(currentRoomIndex);
            if (codeEditorUI != null)
            {
                codeEditorUI.SetInteractionLocked(false);
            }
            ConsoleLogUI.Log($"[System] Initialized Room {currentRoomIndex}. Adjust PlayerController.cs and press Compile & Run.");
        }

        public void StartCombatExecution()
        {
            if (currentPhase != GamePhase.Planning) return;

            // Extract values configured in code sockets (or default fallbacks)
            int attacks = codeEditorUI != null ? codeEditorUI.GetSocketAttacksValue(1) : 1;
            int dmg = codeEditorUI != null ? codeEditorUI.GetSocketDamageValue(10) : 10;
            float mult = codeEditorUI != null ? codeEditorUI.GetSocketMultiplierValue(1.0f) : 1.0f;
            bool pierce = codeEditorUI != null ? codeEditorUI.GetSocketBoolValue(CodeSocketRole.Piercing, false) : false;
            TargetPriority prio = codeEditorUI != null ? codeEditorUI.GetSocketTargetingValue(CodeSocketRole.Targeting, TargetPriority.LowestHealth) : TargetPriority.LowestHealth;

            if (player != null)
            {
                player.Configure(attacks, dmg, mult, pierce, prio);
            }

            if (codeEditorUI != null)
            {
                codeEditorUI.SetInteractionLocked(true);
            }

            SetPhase(GamePhase.Running);
            ConsoleLogUI.Log($"[System] Build succeeded. Running PlayerController.cs (Attacks/Turn: {attacks}, Dmg: {dmg}, Mult: {mult:0.0}x, Pierce: {pierce}, Targeting: {prio})...");

            combatCoroutine = StartCoroutine(CombatLoopCoroutine());
        }

        private IEnumerator CombatLoopCoroutine()
        {
            yield return new WaitForSeconds(0.3f);

            int round = 1;

            while (currentPhase == GamePhase.Running)
            {
                activeEnemies.RemoveAll(e => e == null || e.IsDead);

                if (activeEnemies.Count == 0)
                {
                    HandleVictory();
                    yield break;
                }

                if (player.IsDead)
                {
                    HandleDefeat();
                    yield break;
                }

                ConsoleLogUI.Log($"--- Round {round} ---");

                // 1. Player Turn (Player attacks first)
                if (player != null && !player.IsDead)
                {
                    yield return StartCoroutine(player.ExecutePlayerTurn(activeEnemies));
                }

                // Check victory after player attack
                activeEnemies.RemoveAll(e => e == null || e.IsDead);
                if (activeEnemies.Count == 0)
                {
                    HandleVictory();
                    yield break;
                }

                yield return new WaitForSeconds(0.4f);

                // 2. Enemies Turn (Each living enemy attacks sequentially)
                for (int i = 0; i < activeEnemies.Count; i++)
                {
                    var enemy = activeEnemies[i];
                    if (enemy != null && !enemy.IsDead && player != null && !player.IsDead)
                    {
                        yield return StartCoroutine(enemy.ExecuteEnemyTurn(player));
                        yield return new WaitForSeconds(0.25f);
                    }
                }

                // Check player defeat after enemies turn
                if (player != null && player.IsDead)
                {
                    HandleDefeat();
                    yield break;
                }

                round++;
                yield return new WaitForSeconds(0.5f);
            }
        }

        private void HandleVictory()
        {
            SetPhase(GamePhase.Victory);
            ConsoleLogUI.Log($"[Success] Room {currentRoomIndex} cleared without uncaught exceptions!");
            if (rewardPanelUI != null)
            {
                rewardPanelUI.ShowRewardPrompt();
            }
        }

        private void HandleDefeat()
        {
            SetPhase(GamePhase.Defeat);
            ConsoleLogUI.Log("[Error] Runtime Exception: Player terminated by enemy forces. Run Over.");
        }

        public void AdvanceToNextRoom()
        {
            currentRoomIndex++;
            if (player != null)
            {
                // Health persists across rooms - do NOT heal to full!
                player.AdvanceRoomReset();
            }
            EnterPlanningPhase();
        }

        public void RestartFullRun()
        {
            currentRoomIndex = 1;
            if (player != null)
            {
                player.ResetToMaxHp();
            }
            EnterPlanningPhase();
        }

        public void SetTimeScale(float scale)
        {
            Time.timeScale = scale;
            ConsoleLogUI.Log($"[System] Simulation speed set to {scale}x.");
        }

        private void SetPhase(GamePhase newPhase)
        {
            currentPhase = newPhase;
            OnPhaseChanged?.Invoke(newPhase);
        }

        private void SpawnRoomEnemies(int room)
        {
            if (enemySpawnContainer != null)
            {
                foreach (Transform child in enemySpawnContainer) Destroy(child.gameObject);
            }
            activeEnemies.Clear();

            if (enemyPrefab == null) return;

            // Pokemon-style stationary staging positions in upper-right
            float hpScale = 1f + (room - 1) * 0.25f;
            SpawnEnemy($"Slime_Fast_R{room}", new Vector3(1.6f, 0.7f, 0f), 25f * hpScale, 5f);
            SpawnEnemy($"Slime_Tank_R{room}", new Vector3(3.2f, 1.4f, 0f), 60f * hpScale, 12f);
        }

        private void SpawnEnemy(string name, Vector3 localPos, float hp, float dmg)
        {
            GameObject obj = Instantiate(enemyPrefab, enemySpawnContainer);
            obj.name = name;
            obj.transform.localPosition = localPos;

            EnemyEntity enemy = obj.GetComponent<EnemyEntity>();
            if (enemy != null)
            {
                enemy.contactDamage = dmg;
                enemy.Initialize(player);
                enemy.SetMaxHp(hp);
                activeEnemies.Add(enemy);
            }
        }
    }
}
