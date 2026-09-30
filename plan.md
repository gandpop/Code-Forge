# ARCHITECTURAL PLAN & SPECIFICATION (v21.0)
## Project: CodeForge — Crit Compounding Baseline (1.1x), Gutter/Fold Column Alignment, Enemies.Closest(), Full Rarity Sorting, & Dynamic 1-to-3 Enemy Encounters

---

## 1. Executive Summary & Design Evaluation

### 1.1 Crit Multiplier Compounding Baseline (1.1x)
* **The Math Model:** Attack damage follows the strict compounding formula:
  $$\text{Final Damage} = (\text{baseDamage} \times \text{damageMultiplier}) \times \text{critMultiplier}$$
* **Why 1.1x Default Fallback:**
  - With a high baseline (e.g. 1.5x), drafting early float tokens like `Float_1.1`, `Float_1.2`, and `Float_1.25` felt pointless because slotting them lowered the player's crit damage below the default fallback.
  - Setting the default unslotted fallback to **`1.1x`** guarantees that **every float token in the game ($\ge 1.1f$) is an immediate, palpable upgrade**.
  - Example: Base damage 10, `damageMultiplier = 1.5f` ($10 \times 1.5 = 15$).
    - Unslotted baseline: $15 \times 1.1 = 16.5 \approx 17\text{ DMG}$.
    - Slotting `Float_1.25`: $15 \times 1.25 = 18.75 \approx 19\text{ DMG}$.
    - Slotting `Float_1.5`: $15 \times 1.5 = 22.5 \approx 23\text{ DMG}$.
    - Slotting `Float_2.0`: $15 \times 2.0 = 30\text{ DMG}$!

### 1.2 Line-Collapse Gutter Alignment & Left Margin Polish
* **The Root Cause:**
  - On foldable lines, `FoldToggleBtn` was placed before `Gutter`, shifting the line number 25px right into the code text.
  - The fold toggle touched the far left screen edge and got clipped.
* **The Solution:** Establish a strict, unified column structure across all line rows:
  $$\text{[Left Margin: 6px]} \longrightarrow \text{[Gutter / Line Number: 28px]} \longrightarrow \text{[Fold Slot: 18px]} \longrightarrow \text{[Code Text]}$$
  - Line numbers stay vertically aligned in a neat column.
  - Fold buttons sit cleanly between line numbers and code without pushing text.

### 1.3 Target Selection: `Enemies.Closest()`
* **Pedagogical Alignment:** Preserves consistent collection-query grammar (`Enemies.LowestHP()`, `Enemies.HighestShield()`, `Enemies.Closest()`).
* **Implementation:** Calculates horizontal distance: `Mathf.Abs(enemy.transform.position.x - player.transform.position.x)`.

### 1.4 Eliminating Mid-Game Boredom: Rotating 1-, 2-, and 3-Enemy Encounters
* **The Problem:** After Room 3, the encounter generation defaulted to spawning the exact same two enemies (`Memory_Leak` + `Golem_Elite`) repeatedly forever, leaving 5 other implemented archetypes unused and making combat feel dry and repetitive.
* **The Solution — Curated & Rotating Encounter Table:**
  Rotate through all 7 archetypes (`TrainingSlime`, `ShieldBeetle`, `GolemCharger`, `GlassCannon`, `SlimeTank`, `MemoryLeak`, `SyntaxGlitch`) across varied 1-, 2-, and 3-enemy compositions:
  - **Room 1 (Tutorial):** 1x Training Slime (15 HP, 3 DMG)
  - **Room 2 (Armor Check):** 1x Shield Beetle (30 HP, 6 DMG / 12 Shield)
  - **Room 3 (Reaction Check):** 1x Golem Charger (45 HP, 18 DMG Heavy)
  - **Room 4 (Targeting Puzzle):** 2 Enemies — Slime Tank (Frontline 30 HP, 4 DMG) + Glass Cannon (Backline 20 HP, 9 DMG)
  - **Room 5 (Disruption Check):** 2 Enemies — Syntax Glitch (28 HP, 8 DMG + Debuff) + Shield Beetle Elite (35 HP, 7 DMG)
  - **Room 6 (AoE Swarm):** 3 Enemies — 3x Glitch Minions / Slimes (15 HP, 3 DMG each) $\rightarrow$ prime target for cleave/AoE!
  - **Room 7 (Heavy Dual):** 2 Enemies — Memory Leak (35 HP, 6 DMG lifesteal) + Golem Elite (50 HP, 16 DMG heavy)
  - **Room 8+ (Boss Encounter):** 3 Enemies — 1x Golem Boss (65 HP, 16 DMG) flanked by 2x Shield Beetles (30 HP, 6 DMG / 10 Shield)
  - **Infinite Mode (Room 9+):** Rotates through dynamic 2- and 3-enemy compositions with scaling health and damage:
    $$\text{dmgScale} = 1.0f + (\text{room} - 3) \times 0.15f, \quad \text{hpScale} = 1.0f + (\text{room} - 3) \times 0.20f$$

### 1.5 Token Inventory Shelf Sorting with Full Rarity
* **Sorting Modes:**
  - `Standard`: Natural acquisition order.
  - `Rarity`: `Legendary` (Gold) $\rightarrow$ `Epic` (Purple) $\rightarrow$ `Rare` (Blue) $\rightarrow$ `Uncommon` (Green) $\rightarrow$ `Common` (Gray).
  - `Type`: `Int` $\rightarrow$ `Float` $\rightarrow$ `Bool` $\rightarrow$ `Action` $\rightarrow$ `Condition` $\rightarrow$ `Targeting` $\rightarrow$ `Operator`.
* Strictly manual toggle to prevent disorienting card shifting.

---

## 2. Technical Specifications & Architecture

### 2.1 Crit Baseline Update (`PlayerCombatController.cs` & `CodeEditorPanelUI.cs`)

* In `PlayerCombatController.cs`:
  ```csharp
  public float CritMultiplier { get; set; } = 1.1f;
  ```
* In `CodeEditorPanelUI.cs`:
  ```csharp
  public float GetCritMultiplier()
  {
      if (critMultiplierSocket != null && critMultiplierSocket.AssignedToken != null)
      {
          return critMultiplierSocket.AssignedToken.floatValue > 0f ? critMultiplierSocket.AssignedToken.floatValue : 1.1f;
      }
      return 1.1f;
  }
  ```
* Update Line 10 comment: `// x (Default: 1.1x)`.

---

### 2.2 Gutter & Fold Layout (`EditorLineRowUI.cs`)

```csharp
public void FormatRow()
{
    var layout = GetComponent<HorizontalLayoutGroup>();
    if (layout != null)
    {
        layout.spacing = 4f;
        layout.padding = new RectOffset(6, 6, 0, 0); // 6px left margin
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.MiddleLeft;
    }

    Transform gutterTr = transform.Find("Gutter") ?? transform.Find("LineNumberContainer");
    if (gutterTr != null)
    {
        gutterTr.SetSiblingIndex(1);
        var le = gutterTr.GetComponent<LayoutElement>() ?? gutterTr.gameObject.AddComponent<LayoutElement>();
        le.minWidth = 28f;
        le.preferredWidth = 28f;
    }

    if (foldToggleButton != null)
    {
        foldToggleButton.transform.SetSiblingIndex(2);
        var le = foldToggleButton.GetComponent<LayoutElement>() ?? foldToggleButton.gameObject.AddComponent<LayoutElement>();
        le.minWidth = 18f;
        le.preferredWidth = 18f;
    }
}
```

---

### 2.3 Curated & Rotating Encounter Spawner (`CombatManager.cs`)

```csharp
private void SpawnRoomEnemies(int room)
{
    if (enemySpawnContainer != null)
    {
        foreach (Transform child in enemySpawnContainer) Destroy(child.gameObject);
    }
    activeEnemies.Clear();

    float dmgScale = 1.0f + Mathf.Max(0, room - 3) * 0.15f;
    float hpScale = 1.0f + Mathf.Max(0, room - 3) * 0.20f;

    switch (room)
    {
        case 1:
            SpawnEnemy("Training_Slime", new Vector3(2.5f, 1.1f, 0f), 15f, 3f, EnemyArchetype.TrainingSlime);
            break;
        case 2:
            SpawnEnemy("Shield_Beetle", new Vector3(2.5f, 1.1f, 0f), 30f, 6f, EnemyArchetype.ShieldBeetle);
            break;
        case 3:
            SpawnEnemy("Golem_Charger", new Vector3(2.5f, 1.1f, 0f), 45f, 18f, EnemyArchetype.GolemCharger);
            break;
        case 4:
            // 2 Enemies: Frontline Tank + Backline Glass Cannon
            SpawnEnemy("Slime_Tank", new Vector3(1.6f, 1.1f, 0f), 30f * hpScale, 4f * dmgScale, EnemyArchetype.SlimeTank);
            SpawnEnemy("Glass_Cannon", new Vector3(3.4f, 1.1f, 0f), 20f * hpScale, 9f * dmgScale, EnemyArchetype.GlassCannon);
            break;
        case 5:
            // 2 Enemies: Disruptor + Shield Tank
            SpawnEnemy("Syntax_Glitch", new Vector3(1.6f, 1.1f, 0f), 28f * hpScale, 8f * dmgScale, EnemyArchetype.SyntaxGlitch);
            SpawnEnemy("Shield_Beetle_Elite", new Vector3(3.4f, 1.1f, 0f), 35f * hpScale, 7f * dmgScale, EnemyArchetype.ShieldBeetle);
            break;
        case 6:
            // 3 Enemies: Swarm Cleave Puzzle
            SpawnEnemy("Glitch_Bug_A", new Vector3(1.4f, 1.1f, 0f), 16f * hpScale, 3f * dmgScale, EnemyArchetype.Default);
            SpawnEnemy("Slime_Tank", new Vector3(2.5f, 1.1f, 0f), 32f * hpScale, 4f * dmgScale, EnemyArchetype.SlimeTank);
            SpawnEnemy("Glitch_Bug_B", new Vector3(3.6f, 1.1f, 0f), 16f * hpScale, 3f * dmgScale, EnemyArchetype.Default);
            break;
        case 7:
            // 2 Enemies: Life Steal + Heavy Charger
            SpawnEnemy("Memory_Leak", new Vector3(1.6f, 1.1f, 0f), 35f * hpScale, 6f * dmgScale, EnemyArchetype.MemoryLeak);
            SpawnEnemy("Golem_Elite", new Vector3(3.4f, 1.1f, 0f), 50f * hpScale, 16f * dmgScale, EnemyArchetype.GolemCharger);
            break;
        case 8:
            // 3 Enemies: Boss Encounter (Golem Boss flanked by 2 Shield Beetles)
            SpawnEnemy("Shield_Beetle_L", new Vector3(1.3f, 1.1f, 0f), 30f * hpScale, 5f * dmgScale, EnemyArchetype.ShieldBeetle);
            SpawnEnemy("Golem_Boss", new Vector3(2.5f, 1.2f, 0f), 65f * hpScale, 16f * dmgScale, EnemyArchetype.GolemCharger);
            SpawnEnemy("Shield_Beetle_R", new Vector3(3.7f, 1.1f, 0f), 30f * hpScale, 5f * dmgScale, EnemyArchetype.ShieldBeetle);
            break;
        default:
            // Endless Mode (Rotating Compositions)
            int cycle = (room - 9) % 3;
            if (cycle == 0)
            {
                SpawnEnemy($"Slime_Tank_R{room}", new Vector3(1.6f, 1.1f, 0f), 35f * hpScale, 5f * dmgScale, EnemyArchetype.SlimeTank);
                SpawnEnemy($"Glass_Cannon_R{room}", new Vector3(3.4f, 1.1f, 0f), 25f * hpScale, 11f * dmgScale, EnemyArchetype.GlassCannon);
            }
            else if (cycle == 1)
            {
                SpawnEnemy($"Memory_Leak_R{room}", new Vector3(1.6f, 1.1f, 0f), 40f * hpScale, 7f * dmgScale, EnemyArchetype.MemoryLeak);
                SpawnEnemy($"Syntax_Glitch_R{room}", new Vector3(3.4f, 1.1f, 0f), 32f * hpScale, 9f * dmgScale, EnemyArchetype.SyntaxGlitch);
            }
            else
            {
                SpawnEnemy($"Bug_L_R{room}", new Vector3(1.3f, 1.1f, 0f), 20f * hpScale, 4f * dmgScale, EnemyArchetype.Default);
                SpawnEnemy($"Golem_R{room}", new Vector3(2.5f, 1.2f, 0f), 60f * hpScale, 18f * dmgScale, EnemyArchetype.GolemCharger);
                SpawnEnemy($"Bug_R_R{room}", new Vector3(3.7f, 1.1f, 0f), 20f * hpScale, 4f * dmgScale, EnemyArchetype.Default);
            }
            break;
    }
}
```

---

## 3. Verification & Acceptance Checklist

- [ ] **Crit Multiplier Baseline (1.1x):** Without a slotted token, `critMultiplier` defaults to `1.1f`. Attack formula is verified as `(baseDamage * damageMultiplier) * critMultiplier`. Any drafted float ($\ge 1.1f$) provides an immediate upgrade over leaving the socket empty.
- [ ] **Gutter Alignment:** Line numbers stay 100% aligned in a straight vertical column across all lines. Fold toggles sit between the gutter and code without protruding or clipping.
- [ ] **Targeting Syntax:** The targeting token displays `Enemies.Closest()` and resolves to the closest frontline enemy.
- [ ] **Dynamic Enemy Variety:** Rooms 1 to 8+ feature rotating compositions (1, 2, and 3 enemies) utilizing all 7 archetypes with progressive scaling.
- [ ] **Inventory Sorting:** Manual sort chip cycles between Standard, Rarity (Legendary $\rightarrow$ Common), and Type.
