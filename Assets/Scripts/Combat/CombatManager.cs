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
        [SerializeField] private DefeatModalUI defeatModalUI;

        private List<EnemyEntity> activeEnemies = new List<EnemyEntity>();
        public List<EnemyEntity> ActiveEnemies => activeEnemies;
        private int currentRoomIndex = 1;
        private Coroutine combatCoroutine;

        public event Action<GamePhase> OnPhaseChanged;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            Application.runInBackground = true;

            if (rewardPanelUI == null)
            {
                rewardPanelUI = FindFirstObjectByType<CodeForge.UI.RewardPanelUI>(FindObjectsInactive.Include);
            }
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

            if (rewardPanelUI != null)
            {
                rewardPanelUI.Hide();
            }

            SetPhase(GamePhase.Planning);
            if (RoomProgressionManager.Instance != null)
            {
                RoomProgressionManager.Instance.SetRoom(currentRoomIndex);
            }

            if (codeEditorUI != null)
            {
                codeEditorUI.SetInteractionLocked(false);
                codeEditorUI.ResetAllHighlights();

                if (player != null)
                {
                    player.SetMaxHealth(codeEditorUI.GetMaxHealth());
                    player.DamageMultiplier = codeEditorUI.GetDamageMultiplier();
                }
            }

            SpawnRoomEnemies(currentRoomIndex);

            string roomTitle = currentRoomIndex switch
            {
                1 => "Room 1: 'Hello World' (ExecuteTurn Method)",
                2 => "Room 2: 'The Variable Forge' (Class Fields)",
                3 => "Room 3: 'The Reaction Test' (OnTakeDamage Callback)",
                4 => "Room 4: 'Double Threat' (Slime Tank & Glass Cannon)",
                5 => "Room 5: 'Syntax Error' (Syntax Glitch & Shield Beetle Elite)",
                6 => "Room 6: 'Swarm Routine' (Slime Tank & Glitch Minions)",
                7 => "Room 7: 'Memory Corruption' (Memory Leak & Golem Elite)",
                8 => "Room 8: 'The Core Golem' (Golem Boss & Shield Beetles)",
                _ => $"Room {currentRoomIndex}: Scaling Dungeon"
            };

            ConsoleLogUI.Log($"[System] Initialized {roomTitle}. Configure PlayerCombat.cs and press Compile & Run.");
        }

        public void StartCombatExecution()
        {
            if (currentPhase != GamePhase.Planning) return;

            if (codeEditorUI != null && !codeEditorUI.ValidatePreBattle())
            {
                return;
            }

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

            // 0. Pre-Combat Lifecycle: Execute PlayerCombat.Start() block once before Round 1
            if (player != null && !player.IsDead)
            {
                ConsoleLogUI.Log("[System] Executing PlayerCombat.Start() lifecycle initialization...");
                var startContext = new CombatContext(player, activeEnemies, null, 0);
                yield return StartCoroutine(player.ExecuteStartBlock(startContext, codeEditorUI));
                yield return new WaitForSeconds(0.35f);
            }

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
            ConsoleLogUI.Log("<color=#FF5454>[Error] Runtime Exception: Player terminated by enemy forces. Run Over.</color>");
            if (defeatModalUI != null)
            {
                defeatModalUI.Show();
            }
            else if (DefeatModalUI.Instance != null)
            {
                DefeatModalUI.Instance.Show();
            }
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

        public void RetryCurrentRoom()
        {
            if (player != null)
            {
                player.ResetToMaxHp();
            }
            ConsoleLogUI.Log($"[System] Retrying Room {currentRoomIndex}... Health restored to max.");
            EnterPlanningPhase();
        }

        public void RestartFullRun()
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
            );
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

            int enemyCount = 1;
            if (room == 4 || room == 5 || room == 7) enemyCount = 2;
            else if (room == 6 || room == 8) enemyCount = 3;
            else if (room >= 9)
            {
                int cycle = (room - 9) % 3;
                enemyCount = (cycle == 0) ? 2 : 3;
            }

            var platforms = GameObject.Find("ArenaPlatforms");
            if (platforms != null)
            {
                var playerPed = platforms.transform.Find("Pedestal_Player");
                if (playerPed != null) playerPed.position = new Vector3(-2.0f, 0.4f, 0f);

                var single = platforms.transform.Find("Pedestal_Enemy_Single");
                var left = platforms.transform.Find("Pedestal_Enemy_Left");
                var right = platforms.transform.Find("Pedestal_Enemy_Right");

                if (enemyCount == 1)
                {
                    if (single != null)
                    {
                        single.position = new Vector3(2.5f, 0.4f, 0f);
                        single.gameObject.SetActive(true);
                    }
                    if (left != null) left.gameObject.SetActive(false);
                    if (right != null) right.gameObject.SetActive(false);
                }
                else if (enemyCount == 2)
                {
                    if (single != null) single.gameObject.SetActive(false);
                    if (left != null)
                    {
                        left.position = new Vector3(1.6f, 0.4f, 0f);
                        left.gameObject.SetActive(true);
                    }
                    if (right != null)
                    {
                        right.position = new Vector3(3.4f, 0.4f, 0f);
                        right.gameObject.SetActive(true);
                    }
                }
                else // 3 enemies
                {
                    if (single != null)
                    {
                        single.position = new Vector3(2.5f, 0.4f, 0f);
                        single.gameObject.SetActive(true);
                    }
                    if (left != null)
                    {
                        left.position = new Vector3(1.4f, 0.4f, 0f);
                        left.gameObject.SetActive(true);
                    }
                    if (right != null)
                    {
                        right.position = new Vector3(3.6f, 0.4f, 0f);
                        right.gameObject.SetActive(true);
                    }
                }
            }

            if (player != null)
            {
                player.transform.position = new Vector3(-2.0f, 1.0f, 0f);
            }

            // Curated encounter table & dynamic scaling
            switch (room)
            {
                case 1:
                    // Room 1: 1x Training Slime (15 HP, 3 DMG)
                    SpawnEnemy("Training_Slime", new Vector3(2.5f, 1.1f, 0f), 15f, 3f, EnemyArchetype.TrainingSlime);
                    break;

                case 2:
                    // Room 2: 1x Shield Beetle (30 HP, 6 DMG / 12 Shield)
                    SpawnEnemy("Shield_Beetle", new Vector3(2.5f, 1.1f, 0f), 30f, 6f, EnemyArchetype.ShieldBeetle);
                    break;

                case 3:
                    // Room 3: 1x Golem Charger (45 HP, 18 DMG Heavy)
                    SpawnEnemy("Golem_Charger", new Vector3(2.5f, 1.1f, 0f), 45f, 18f, EnemyArchetype.GolemCharger);
                    break;

                case 4:
                    // Room 4: 2 Enemies — Slime Tank (30 HP, 4 DMG) + Glass Cannon (20 HP, 9 DMG)
                    SpawnEnemy("Slime_Tank", new Vector3(1.6f, 1.1f, 0f), 30f, 4f, EnemyArchetype.SlimeTank);
                    SpawnEnemy("Glass_Cannon", new Vector3(3.4f, 1.1f, 0f), 20f, 9f, EnemyArchetype.GlassCannon);
                    break;

                case 5:
                    // Room 5: 2 Enemies — Syntax Glitch (28 HP, 8 DMG) + Shield Beetle Elite (35 HP, 7 DMG)
                    SpawnEnemy("Syntax_Glitch", new Vector3(1.6f, 1.1f, 0f), 28f, 8f, EnemyArchetype.SyntaxGlitch);
                    SpawnEnemy("Shield_Beetle_Elite", new Vector3(3.4f, 1.1f, 0f), 35f, 7f, EnemyArchetype.ShieldBeetle);
                    break;

                case 6:
                    // Room 6: 3 Enemies — 1x Slime Tank (32 HP) flanked by 2x Glitch Minions (16 HP, 3 DMG each)
                    SpawnEnemy("Glitch_Minion_Left", new Vector3(1.4f, 1.1f, 0f), 16f, 3f, EnemyArchetype.SyntaxGlitch);
                    SpawnEnemy("Slime_Tank", new Vector3(2.5f, 1.1f, 0f), 32f, 4f, EnemyArchetype.SlimeTank);
                    SpawnEnemy("Glitch_Minion_Right", new Vector3(3.6f, 1.1f, 0f), 16f, 3f, EnemyArchetype.SyntaxGlitch);
                    break;

                case 7:
                    // Room 7: 2 Enemies — Memory Leak (35 HP, 6 DMG) + Golem Elite (50 HP, 16 DMG)
                    SpawnEnemy("Memory_Leak", new Vector3(1.6f, 1.1f, 0f), 35f, 6f, EnemyArchetype.MemoryLeak);
                    SpawnEnemy("Golem_Elite", new Vector3(3.4f, 1.1f, 0f), 50f, 16f, EnemyArchetype.GolemCharger);
                    break;

                case 8:
                    // Room 8: 3 Enemies — 1x Golem Boss (65 HP, 16 DMG) flanked by 2x Shield Beetles (30 HP, 5 DMG)
                    SpawnEnemy("Shield_Beetle_Left", new Vector3(1.4f, 1.1f, 0f), 30f, 5f, EnemyArchetype.ShieldBeetle);
                    SpawnEnemy("Golem_Boss", new Vector3(2.5f, 1.1f, 0f), 65f, 16f, EnemyArchetype.GolemCharger);
                    SpawnEnemy("Shield_Beetle_Right", new Vector3(3.6f, 1.1f, 0f), 30f, 5f, EnemyArchetype.ShieldBeetle);
                    break;

                default:
                    // Room 9+: Dynamic rotating 2- and 3-enemy compositions with progressive scaling
                    float scale = 1f + (room - 8) * 0.12f;
                    int cycle = (room - 9) % 3;
                    if (cycle == 0)
                    {
                        // 2 Enemies: Memory Leak + Syntax Glitch
                        SpawnEnemy($"Memory_Leak_R{room}", new Vector3(1.6f, 1.1f, 0f), 36f * scale, 6f * scale, EnemyArchetype.MemoryLeak);
                        SpawnEnemy($"Syntax_Glitch_R{room}", new Vector3(3.4f, 1.1f, 0f), 30f * scale, 8f * scale, EnemyArchetype.SyntaxGlitch);
                    }
                    else if (cycle == 1)
                    {
                        // 3 Enemies: Golem flanked by Shield Beetles
                        SpawnEnemy($"Shield_Beetle_L_R{room}", new Vector3(1.4f, 1.1f, 0f), 28f * scale, 5f * scale, EnemyArchetype.ShieldBeetle);
                        SpawnEnemy($"Golem_Charger_R{room}", new Vector3(2.5f, 1.1f, 0f), 45f * scale, 16f * scale, EnemyArchetype.GolemCharger);
                        SpawnEnemy($"Shield_Beetle_R_R{room}", new Vector3(3.6f, 1.1f, 0f), 28f * scale, 5f * scale, EnemyArchetype.ShieldBeetle);
                    }
                    else
                    {
                        // 3 Enemies: Slime Tank flanked by Glass Cannons
                        SpawnEnemy($"Glass_Cannon_L_R{room}", new Vector3(1.4f, 1.1f, 0f), 20f * scale, 8f * scale, EnemyArchetype.GlassCannon);
                        SpawnEnemy($"Slime_Tank_R{room}", new Vector3(2.5f, 1.1f, 0f), 34f * scale, 4f * scale, EnemyArchetype.SlimeTank);
                        SpawnEnemy($"Glass_Cannon_R_R{room}", new Vector3(3.6f, 1.1f, 0f), 20f * scale, 8f * scale, EnemyArchetype.GlassCannon);
                    }
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
