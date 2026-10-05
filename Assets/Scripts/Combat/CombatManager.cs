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

        [Header("Enemy Spawn Points (Adjust in Scene View)")]
        [SerializeField] private Transform slot1SpawnPoint;
        [SerializeField] private Transform slot2SpawnPoint;
        [SerializeField] private Transform slot3SpawnPoint;

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
                3 => "Room 3: 'The Heavy Threat' (Stone Golem Charge)",
                4 => "Room 4: 'Double Threat' (Slime Tank & Demonic Eye)",
                5 => "Room 5: 'Dark Synergy' (Skeleton & Vampire)",
                6 => "Room 6: 'Swarm Routine' (Demonic Eyes & Slime Tank Boss)",
                7 => "Room 7: 'Elite Trial' (Skeleton, Vampire & Stone Golem Elite)",
                8 => "Room 8: 'The Core Golem' (Skeleton Guard, Vampire Thrall & Golem Boss)",
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
            else if (room <= 8) enemyCount = 3;
            else
            {
                var rngCheck = new System.Random(room * 7919);
                enemyCount = rngCheck.NextDouble() < 0.40 ? 2 : 3;
            }

            var platforms = GameObject.Find("ArenaPlatforms");
            if (platforms != null) platforms.SetActive(false);

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
                    // Room 2: 1x Skeleton (30 HP, 6 DMG / 12 Shield)
                    SpawnEnemy("Skeleton", 1, 30f, 6f, EnemyArchetype.Skeleton);
                    break;

                case 3:
                    // Room 3: 1x Stone Golem (45 HP, 18 DMG Heavy)
                    SpawnEnemy("Stone_Golem", 1, 45f, 18f, EnemyArchetype.StoneGolem);
                    break;

                case 4:
                    // Room 4: 2 Enemies — Slime Tank (Slot 1) + Demonic Eye (Slot 2)
                    SpawnEnemy("Slime_Tank", 1, 30f, 4f, EnemyArchetype.SlimeTank);
                    SpawnEnemy("Demonic_Eye", 2, 22f, 6f, EnemyArchetype.DemonicEye);
                    break;

                case 5:
                    // Room 5: 2 Enemies — Skeleton (Slot 1) + Vampire (Slot 2)
                    SpawnEnemy("Skeleton", 1, 32f, 6f, EnemyArchetype.Skeleton);
                    SpawnEnemy("Vampire", 2, 28f, 5f, EnemyArchetype.Vampire);
                    break;

                case 6:
                    // Room 6: 3 Enemies — Demonic Eye A (Slot 1) + Demonic Eye B (Slot 2) + Slime Tank Boss (Slot 3)
                    SpawnEnemy("Demonic_Eye_A", 1, 20f, 5f, EnemyArchetype.DemonicEye);
                    SpawnEnemy("Demonic_Eye_B", 2, 20f, 5f, EnemyArchetype.DemonicEye);
                    SpawnEnemy("Slime_Tank_Boss", 3, 45f, 6f, EnemyArchetype.SlimeTank);
                    break;

                case 7:
                    // Room 7: 3 Enemies — Skeleton (Slot 1) + Vampire (Slot 2) + Stone Golem Elite (Slot 3)
                    SpawnEnemy("Skeleton", 1, 32f, 6f, EnemyArchetype.Skeleton);
                    SpawnEnemy("Vampire", 2, 30f, 6f, EnemyArchetype.Vampire);
                    SpawnEnemy("Stone_Golem_Elite", 3, 55f, 18f, EnemyArchetype.StoneGolem);
                    break;

                case 8:
                    // Room 8: 3 Enemies (Boss) — Skeleton Guard (Slot 1) + Vampire Thrall (Slot 2) + Stone Golem Boss (Slot 3)
                    SpawnEnemy("Skeleton_Guard", 1, 35f, 6f, EnemyArchetype.Skeleton);
                    SpawnEnemy("Vampire_Thrall", 2, 32f, 6f, EnemyArchetype.Vampire);
                    SpawnEnemy("Stone_Golem_Boss", 3, 70f, 20f, EnemyArchetype.StoneGolem);
                    break;

                default:
                    // Room 9+: Dynamic Scaling Endless Mode
                    // 40% 2 enemies, 60% 3 enemies from the 5 surviving archetypes
                    var rng = new System.Random(room * 7919);
                    bool isTwoEnemies = rng.NextDouble() < 0.40;
                    int count = isTwoEnemies ? 2 : 3;
                    float scale = 1f + (room - 8) * 0.15f;

                    // Slot 1 prefers SlimeTank / Skeleton / DemonicEye
                    EnemyArchetype[] slot1Pool = { EnemyArchetype.SlimeTank, EnemyArchetype.Skeleton, EnemyArchetype.DemonicEye };
                    EnemyArchetype a1 = slot1Pool[rng.Next(slot1Pool.Length)];
                    float baseHp1 = a1 == EnemyArchetype.SlimeTank ? 35f : (a1 == EnemyArchetype.Skeleton ? 32f : 24f);
                    float baseDmg1 = a1 == EnemyArchetype.SlimeTank ? 5f : 6f;
                    SpawnEnemy($"{a1}_R{room}", 1, Mathf.Round(baseHp1 * scale), Mathf.Round(baseDmg1 * scale), a1);

                    // Slot 2 prefers DemonicEye / Skeleton / Vampire
                    EnemyArchetype[] slot2Pool = { EnemyArchetype.DemonicEye, EnemyArchetype.Skeleton, EnemyArchetype.Vampire };
                    EnemyArchetype a2 = slot2Pool[rng.Next(slot2Pool.Length)];
                    float baseHp2 = a2 == EnemyArchetype.DemonicEye ? 24f : (a2 == EnemyArchetype.Skeleton ? 32f : 30f);
                    float baseDmg2 = 6f;
                    SpawnEnemy($"{a2}_R{room}", 2, Mathf.Round(baseHp2 * scale), Mathf.Round(baseDmg2 * scale), a2);

                    // Slot 3 prefers StoneGolem / SlimeTank
                    if (count >= 3)
                    {
                        EnemyArchetype[] slot3Pool = { EnemyArchetype.StoneGolem, EnemyArchetype.SlimeTank };
                        EnemyArchetype a3 = slot3Pool[rng.Next(slot3Pool.Length)];
                        float baseHp3 = a3 == EnemyArchetype.StoneGolem ? 55f : 45f;
                        float baseDmg3 = a3 == EnemyArchetype.StoneGolem ? 18f : 6f;
                        SpawnEnemy($"{a3}_Boss_R{room}", 3, Mathf.Round(baseHp3 * scale), Mathf.Round(baseDmg3 * scale), a3);
                    }
                    break;
            }
        }

        public static Vector3 GetSlotPosition(int slotIndex)
        {
            if (Instance != null)
            {
                Transform spawnPoint = slotIndex switch
                {
                    1 => Instance.slot1SpawnPoint,
                    2 => Instance.slot2SpawnPoint,
                    3 => Instance.slot3SpawnPoint,
                    _ => Instance.slot1SpawnPoint
                };

                if (spawnPoint != null)
                {
                    return spawnPoint.position;
                }
            }

            return slotIndex switch
            {
                1 => new Vector3(3.40f, -1.60f, 0f),
                2 => new Vector3(-0.60f, 0.40f, 0f),
                3 => new Vector3(2.20f, 1.80f, 0f),
                _ => new Vector3(3.40f, -1.60f, 0f)
            };
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            DrawSlotGizmo(slot1SpawnPoint, 1, new Color(0.2f, 1f, 0.4f, 0.8f));
            DrawSlotGizmo(slot2SpawnPoint, 2, new Color(0.3f, 0.8f, 1f, 0.8f));
            DrawSlotGizmo(slot3SpawnPoint, 3, new Color(1f, 0.4f, 0.8f, 0.8f));
        }

        private void DrawSlotGizmo(Transform spawnPoint, int slot, Color col)
        {
            Vector3 pos = spawnPoint != null ? spawnPoint.position : GetSlotPosition(slot);
            Gizmos.color = col;
            Gizmos.DrawWireSphere(pos, 0.35f);
            UnityEditor.Handles.color = col;
            UnityEditor.Handles.Label(pos + Vector3.up * 0.45f, $"Slot {slot} ({(spawnPoint != null ? spawnPoint.name : "Default")})");
        }

        [ContextMenu("Clear Scene Enemies")]
        public void ClearSceneEnemies()
        {
            if (enemySpawnContainer != null)
            {
                while (enemySpawnContainer.childCount > 0)
                {
                    DestroyImmediate(enemySpawnContainer.GetChild(0).gameObject);
                }
            }
            activeEnemies.Clear();
        }
#endif

        public static Vector3 GetSlotScale(int slotIndex) => new Vector3(1.0f, 1.0f, 1.0f);

        private void SpawnEnemy(string name, int slotIndex, float hp, float dmg, EnemyArchetype archetype = EnemyArchetype.DemonicEye)
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

        private void SpawnEnemy(string name, Vector3 localPos, float hp, float dmg, EnemyArchetype archetype = EnemyArchetype.DemonicEye)
        {
            SpawnEnemy(name, 1, hp, dmg, archetype);
            if (activeEnemies.Count > 0)
            {
                activeEnemies[activeEnemies.Count - 1].transform.position = localPos;
            }
        }
    }
}
