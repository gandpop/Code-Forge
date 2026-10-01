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

        private Vector3 playerInitialPos;
        private Vector3 playerInitialScale;
        private bool playerInitialCaptured = false;

        public event Action<GamePhase> OnPhaseChanged;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            Application.runInBackground = true;

            if (player == null)
            {
                player = FindFirstObjectByType<PlayerCombatController>();
            }

            if (player != null)
            {
                playerInitialPos = new Vector3(player.transform.position.x, player.transform.position.y, 0f);
                playerInitialScale = player.transform.localScale;
                playerInitialCaptured = true;
            }

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
                6 => "Room 6: 'Swarm Routine' (Glitch Minion, Shield Beetle & Slime Boss)",
                7 => "Room 7: 'Memory Corruption' (Syntax Glitch, Memory Leak & Golem Elite)",
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
            if (room <= 3) enemyCount = 1;
            else if (room <= 5) enemyCount = 2;
            else enemyCount = 3;

            var platforms = GameObject.Find("ArenaPlatforms");
            if (platforms != null)
            {
                var playerPed = platforms.transform.Find("Pedestal_Player");
                if (playerPed != null)
                {
                    playerPed.gameObject.SetActive(true);
                }

                var ped1 = platforms.transform.Find("Pedestal_Enemy_1") ?? platforms.transform.Find("Pedestal_Enemy_Single");
                var ped2 = platforms.transform.Find("Pedestal_Enemy_2") ?? platforms.transform.Find("Pedestal_Enemy_Left");
                var ped3 = platforms.transform.Find("Pedestal_Enemy_3") ?? platforms.transform.Find("Pedestal_Enemy_Right");

                if (ped1 != null) ped1.gameObject.SetActive(enemyCount >= 1);
                if (ped2 != null) ped2.gameObject.SetActive(enemyCount >= 2);
                if (ped3 != null) ped3.gameObject.SetActive(enemyCount >= 3);
            }

            if (player != null && playerInitialCaptured)
            {
                player.transform.position = playerInitialPos;
                player.transform.localScale = playerInitialScale;
            }

            // Curated encounter table & dynamic scaling
            switch (room)
            {
                case 1:
                    // Room 1: 1x Training Slime (15 HP, 3 DMG)
                    SpawnEnemy("Training_Slime", 1, 15f, 3f, EnemyArchetype.TrainingSlime);
                    break;

                case 2:
                    // Room 2: 1x Shield Beetle (30 HP, 6 DMG / 12 Shield)
                    SpawnEnemy("Shield_Beetle", 1, 30f, 6f, EnemyArchetype.ShieldBeetle);
                    break;

                case 3:
                    // Room 3: 1x Golem Charger (45 HP, 18 DMG Heavy)
                    SpawnEnemy("Golem_Charger", 1, 45f, 18f, EnemyArchetype.GolemCharger);
                    break;

                case 4:
                    // Room 4: 2 Enemies — Slime Tank (Frontline) + Glass Cannon (Midline)
                    SpawnEnemy("Slime_Tank", 1, 30f, 4f, EnemyArchetype.SlimeTank);
                    SpawnEnemy("Glass_Cannon", 2, 20f, 9f, EnemyArchetype.GlassCannon);
                    break;

                case 5:
                    // Room 5: 2 Enemies — Syntax Glitch (Frontline) + Shield Beetle Elite (Midline)
                    SpawnEnemy("Syntax_Glitch", 1, 28f, 8f, EnemyArchetype.SyntaxGlitch);
                    SpawnEnemy("Shield_Beetle_Elite", 2, 35f, 7f, EnemyArchetype.ShieldBeetle);
                    break;

                case 6:
                    // Room 6: 3 Enemies — Glitch Minion (Frontline) + Shield Beetle (Midline) + Slime Tank Boss (Backline)
                    SpawnEnemy("Glitch_Minion", 1, 16f, 3f, EnemyArchetype.SyntaxGlitch);
                    SpawnEnemy("Shield_Beetle", 2, 25f, 5f, EnemyArchetype.ShieldBeetle);
                    SpawnEnemy("Slime_Tank_Boss", 3, 40f, 6f, EnemyArchetype.SlimeTank);
                    break;

                case 7:
                    // Room 7: 3 Enemies — Syntax Glitch (Frontline) + Memory Leak (Midline) + Golem Elite (Backline)
                    SpawnEnemy("Syntax_Glitch", 1, 24f, 6f, EnemyArchetype.SyntaxGlitch);
                    SpawnEnemy("Memory_Leak", 2, 35f, 6f, EnemyArchetype.MemoryLeak);
                    SpawnEnemy("Golem_Elite", 3, 50f, 16f, EnemyArchetype.GolemCharger);
                    break;

                case 8:
                    // Room 8: 3 Enemies — Shield Beetle (Frontline) + Shield Beetle (Midline) + Golem Boss (Backline)
                    SpawnEnemy("Shield_Beetle_Front", 1, 30f, 5f, EnemyArchetype.ShieldBeetle);
                    SpawnEnemy("Shield_Beetle_Mid", 2, 30f, 5f, EnemyArchetype.ShieldBeetle);
                    SpawnEnemy("Golem_Boss", 3, 65f, 16f, EnemyArchetype.GolemCharger);
                    break;

                default:
                    // Room 9+: Dynamic rotating 3-enemy compositions with progressive scaling
                    float scale = 1f + (room - 8) * 0.12f;
                    int cycle = (room - 9) % 3;
                    if (cycle == 0)
                    {
                        SpawnEnemy($"Syntax_Glitch_R{room}", 1, 26f * scale, 6f * scale, EnemyArchetype.SyntaxGlitch);
                        SpawnEnemy($"Memory_Leak_R{room}", 2, 36f * scale, 6f * scale, EnemyArchetype.MemoryLeak);
                        SpawnEnemy($"Golem_Charger_R{room}", 3, 50f * scale, 16f * scale, EnemyArchetype.GolemCharger);
                    }
                    else if (cycle == 1)
                    {
                        SpawnEnemy($"Shield_Beetle_1_R{room}", 1, 28f * scale, 5f * scale, EnemyArchetype.ShieldBeetle);
                        SpawnEnemy($"Shield_Beetle_2_R{room}", 2, 28f * scale, 5f * scale, EnemyArchetype.ShieldBeetle);
                        SpawnEnemy($"Golem_Boss_R{room}", 3, 60f * scale, 18f * scale, EnemyArchetype.GolemCharger);
                    }
                    else
                    {
                        SpawnEnemy($"Glass_Cannon_1_R{room}", 1, 20f * scale, 8f * scale, EnemyArchetype.GlassCannon);
                        SpawnEnemy($"Glass_Cannon_2_R{room}", 2, 20f * scale, 8f * scale, EnemyArchetype.GlassCannon);
                        SpawnEnemy($"Slime_Tank_Boss_R{room}", 3, 45f * scale, 6f * scale, EnemyArchetype.SlimeTank);
                    }
                    break;
            }
        }

        public static Vector3 GetSlotPosition(int slotIndex)
        {
            var platforms = GameObject.Find("ArenaPlatforms");
            if (platforms != null)
            {
                string pedName = slotIndex switch
                {
                    1 => "Pedestal_Enemy_1",
                    2 => "Pedestal_Enemy_2",
                    3 => "Pedestal_Enemy_3",
                    _ => "Pedestal_Enemy_1"
                };
                var ped = platforms.transform.Find(pedName) ??
                          platforms.transform.Find(slotIndex == 1 ? "Pedestal_Enemy_Single" : (slotIndex == 2 ? "Pedestal_Enemy_Left" : "Pedestal_Enemy_Right"));
                if (ped != null)
                {
                    // Spawn directly on top of the pedestal
                    return new Vector3(ped.position.x, ped.position.y + 0.70f, 0f);
                }
            }

            return slotIndex switch
            {
                1 => new Vector3(3.12f, -1.20f, 0f),
                2 => new Vector3(-2.30f, 0.72f, 0f),
                3 => new Vector3(2.09f, 1.94f, 0f),
                _ => new Vector3(3.12f, -1.20f, 0f)
            };
        }

        public static Vector3 GetSlotScale(int slotIndex) => new Vector3(1.0f, 1.0f, 1.0f);

        private void SpawnEnemy(string name, int slotIndex, float hp, float dmg, EnemyArchetype archetype = EnemyArchetype.Default)
        {
            GameObject obj = Instantiate(enemyPrefab, enemySpawnContainer);
            obj.name = name;
            obj.transform.position = GetSlotPosition(slotIndex);
            obj.transform.localScale = GetSlotScale(slotIndex);

            EnemyEntity enemy = obj.GetComponent<EnemyEntity>();
            if (enemy != null)
            {
                enemy.slotIndex = slotIndex;
                enemy.archetype = archetype;
                enemy.contactDamage = dmg;
                enemy.Initialize(player);
                enemy.SetMaxHp(hp);
                enemy.RollNextIntent(1);
                activeEnemies.Add(enemy);
            }
        }

        private void SpawnEnemy(string name, Vector3 localPos, float hp, float dmg, EnemyArchetype archetype = EnemyArchetype.Default)
        {
            SpawnEnemy(name, 1, hp, dmg, archetype);
            if (activeEnemies.Count > 0)
            {
                activeEnemies[activeEnemies.Count - 1].transform.position = localPos;
            }
        }
    }
}
