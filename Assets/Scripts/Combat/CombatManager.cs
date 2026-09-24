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
            if (RoomProgressionManager.Instance != null)
            {
                RoomProgressionManager.Instance.SetRoom(currentRoomIndex);
            }

            if (codeEditorUI != null)
            {
                codeEditorUI.SetProgressionState(currentRoomIndex);
                codeEditorUI.SetInteractionLocked(false);
                codeEditorUI.ResetAllHighlights();

                if (player != null)
                {
                    player.DamageMultiplier = codeEditorUI.GetDamageMultiplier();
                    if (codeEditorUI.IsSection1Unlocked)
                    {
                        int baseShield = codeEditorUI.GetBaseShield();
                        if (baseShield > 0)
                        {
                            player.AddShield(baseShield);
                            ConsoleLogUI.Log($"[Stats] Applied baseShield +{baseShield} to Player.");
                        }
                    }
                }
            }

            SpawnRoomEnemies(currentRoomIndex);

            string roomTitle = currentRoomIndex switch
            {
                1 => "Room 1: 'Hello World' (ExecuteTurn Method)",
                2 => "Room 2: 'The Variable Forge' (Class Fields)",
                3 => "Room 3: 'The Reaction Test' (OnTakeDamage Callback)",
                _ => $"Room {currentRoomIndex}: Scaling Dungeon"
            };

            ConsoleLogUI.Log($"[System] Initialized {roomTitle}. Configure PlayerCombat.cs and press Compile & Run.");
        }

        public void StartCombatExecution()
        {
            if (currentPhase != GamePhase.Planning) return;

            if (codeEditorUI != null)
            {
                codeEditorUI.SetInteractionLocked(true);
            }

            SetPhase(GamePhase.Running);
            ConsoleLogUI.Log($"[System] Compiling ExecuteTurn(). Starting pipeline execution...");

            combatCoroutine = StartCoroutine(CombatLoopCoroutine());
        }

        private IEnumerator CombatLoopCoroutine()
        {
            yield return new WaitForSeconds(0.2f);

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

                // 1. Player Turn Pipeline Execution
                if (player != null && !player.IsDead)
                {
                    var targetToken = codeEditorUI != null ? codeEditorUI.GetTargetingToken() : null;
                    var condToken = codeEditorUI != null ? codeEditorUI.GetConditionToken() : null;
                    var thenToken = codeEditorUI != null ? codeEditorUI.GetThenActionToken() : null;
                    var elseToken = codeEditorUI != null ? codeEditorUI.GetElseActionToken() : null;

                    var context = new CombatContext(player, activeEnemies, null, round);
                    yield return StartCoroutine(player.ExecuteTurnPipeline(context, codeEditorUI, targetToken, condToken, thenToken, elseToken));
                }

                // Check victory after player pipeline completes
                activeEnemies.RemoveAll(e => e == null || e.IsDead);
                if (activeEnemies.Count == 0)
                {
                    HandleVictory();
                    yield break;
                }

                yield return new WaitForSeconds(0.4f);

                // 2. Enemies Turn (Each living enemy acts based on telegraphed deterministic intent)
                for (int i = 0; i < activeEnemies.Count; i++)
                {
                    var enemy = activeEnemies[i];
                    if (enemy != null && !enemy.IsDead && player != null && !player.IsDead)
                    {
                        yield return StartCoroutine(enemy.ExecuteEnemyTurn(player));
                        enemy.RollNextIntent(round + 1);
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
            if (codeEditorUI != null) codeEditorUI.ResetAllHighlights();
            ConsoleLogUI.Log($"[Success] Room {currentRoomIndex} cleared without unhandled exceptions!");
            if (rewardPanelUI != null)
            {
                rewardPanelUI.ShowRewardPrompt(currentRoomIndex);
            }
        }

        private void HandleDefeat()
        {
            SetPhase(GamePhase.Defeat);
            if (codeEditorUI != null) codeEditorUI.ResetAllHighlights();
            ConsoleLogUI.Log("[Error] Runtime Exception: Player terminated by enemy forces. Run Over.");
        }

        public void AdvanceToNextRoom()
        {
            currentRoomIndex++;
            if (player != null)
            {
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

            switch (room)
            {
                case 1:
                    // Room 1: "Hello World" (1x 15 HP Slime doing 3 DMG flat)
                    SpawnEnemy("Training_Slime", new Vector3(2.4f, 1.0f, 0f), 15f, 3f, EnemyArchetype.TrainingSlime);
                    break;

                case 2:
                    // Room 2: "The Variable Forge" (1x Shield Beetle, 30 HP, rotates: Turn 1 [SHIELD 12], Turn 2 [ATK 6])
                    SpawnEnemy("Shield_Beetle", new Vector3(2.4f, 1.0f, 0f), 30f, 6f, EnemyArchetype.ShieldBeetle);
                    break;

                case 3:
                    // Room 3: "The Reaction Test" (1x Golem Charger 45 HP; Turn 1 & 2 [CHARGE], Turn 3 [HEAVY 20])
                    SpawnEnemy("Golem_Charger", new Vector3(2.4f, 1.0f, 0f), 45f, 20f, EnemyArchetype.GolemCharger);
                    break;

                default:
                    // Scaling encounters beyond Room 3
                    float scale = 1f + (room - 3) * 0.25f;
                    SpawnEnemy($"Slime_Elite_R{room}", new Vector3(1.6f, 0.7f, 0f), 25f * scale, 5f * scale, EnemyArchetype.Default);
                    SpawnEnemy($"Golem_Elite_R{room}", new Vector3(3.2f, 1.4f, 0f), 50f * scale, 15f * scale, EnemyArchetype.GolemCharger);
                    break;
            }
        }

        private void SpawnEnemy(string name, Vector3 localPos, float hp, float dmg, EnemyArchetype archetype = EnemyArchetype.Default)
        {
            GameObject obj = Instantiate(enemyPrefab, enemySpawnContainer);
            obj.name = name;
            obj.transform.localPosition = localPos;

            EnemyEntity enemy = obj.GetComponent<EnemyEntity>();
            if (enemy != null)
            {
                enemy.archetype = archetype;
                enemy.contactDamage = dmg;
                enemy.Initialize(player);
                enemy.SetMaxHp(hp);
                enemy.RollNextIntent(1);
                activeEnemies.Add(enemy);
            }
        }
    }
}
