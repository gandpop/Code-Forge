# ARCHITECTURAL PLAN & SPECIFICATION (v3.0)
## Project: CodeForge — Unity C# Programming Roguelike

---

## 1. Core Vision & Progression Flow
* **Target Audience:** Novice C# and Unity learners trapped in "Tutorial Hell" who struggle with independent problem-solving, architectural thinking, and understanding execution flow.
* **Core Metaphor:** The player is writing a real Unity `MonoBehaviour` script (`PlayerCombat.cs`). 
  - **Left Half (IDE View):** Dark-mode C# script editor with syntax highlighting and horizontal inline sockets.
  - **Right Half (Game View):** Classic dungeon crawler roguelike combat arena.
* **Pacing & Progressive Unlocking (Rooms 1–3):**
  - **Room 1: The Method (`ExecuteTurn`)** — Learn basic targeting and conditional execution.
  - **Room 2: The Variables (`Class Fields`)** — Learn data types (`float`, `int`) for stats (damage multiplier, base shield).
  - **Room 3: The Event Callback (`OnTakeDamage`)** — Learn event-driven programming (reacting to incoming damage).
  - **Room 4+: Infinite / Scaling Roguelike** — Full build crafting with synergies, elite enemies, and rare/legendary token drafts.

---

## 2. The Script Architecture (`PlayerCombat.cs`)

The left-side editor represents a single cohesive Unity script:

```csharp
public class PlayerCombat : MonoBehaviour
{
    // ========================================================
    // SECTION 1: CLASS FIELDS (Unlocked after Room 1)
    // ========================================================
    [Header("Stats")]
    public float damageMultiplier = [ FLOAT_SOCKET ]; // e.g. 1.25f
    public int   baseShield       = [ INT_SOCKET   ]; // e.g. 8

    // ========================================================
    // SECTION 2: MAIN TURN METHOD (Unlocked Room 1)
    // ========================================================
    public void ExecuteTurn()
    {
        var target = [ TARGET_SOCKET ];

        if ( [ CONDITION_SOCKET ] )
        {
            [ ACTION_THEN_SOCKET ];
        }
        else
        {
            [ ACTION_ELSE_SOCKET ];
        }
    }

    // ========================================================
    // SECTION 3: EVENT CALLBACK (Unlocked after Room 2)
    // ========================================================
    public void OnTakeDamage(int incomingDamage)
    {
        if ( [ REACTION_CONDITION_SOCKET ] ) // e.g. incomingDamage >= 8
        {
            [ REACTION_ACTION_SOCKET ];     // e.g. player.AddShield(12)
        }
    }
}
```

*Note: In Room 1, Sections 1 & 3 are grayed out with inline comments:*
`// TODO: Clear Room 1 to unlock Class Fields`
`// TODO: Clear Room 2 to unlock Event Callbacks`

---

## 3. Data Layer Specifications (`CodeForge.Data`)

### 3.1 Token Typings
* `Float`: Assigned to `damageMultiplier` or threshold values (e.g. `1.25f`, `1.5f`, `2.0f`).
* `Int`: Assigned to `baseShield` or flat numeric fields (e.g. `5`, `10`, `15`).
* `Targeting`: Plugged into `var target = [ ... ]` (e.g., `Enemies.LowestHP()`, `Enemies.TargetAttacker()`).
* `Condition`: Boolean expressions (e.g., `target.HealthPercent <= 0.30f`, `target.IsShielded == true`, `incomingDamage >= 8`).
* `Action`: Executable combat abilities (e.g., `target.TakeDamage(7)`, `target.PierceDamage(8)`, `player.AddShield(10)`).

---

## 4. UI Ergonomics & Readability Specifications (`CodeForge.UI`)

1. **Horizontal Inline Sockets:**
   * Sockets must be formatted as **horizontal inline capsules** matching text line height (`Height: ~32-36px`, `Width: auto-expand / min 160px`).
   * Eliminates the awkward vertical clipping (`[ Drop Tar... ]`).
2. **Visual Section Headers:**
   * Foldout or distinct card containers for:
     1. `Variables & Stats`
     2. `ExecuteTurn()`
     3. `OnTakeDamage()`
3. **Execution Trace Feedback:**
   * When `COMPILE & BATTLE` runs:
     - Active lines glow green sequentially.
     - Evaluated conditions pulse Green (`true`) or Red (`false`).
     - Chosen branch stays bright while unchosen branch dims.

---

## 5. Room & Enemy Progression

| Room | Title & Theme | Enemy Setup | Educational Focus | Unlock / Reward |
| :--- | :--- | :--- | :--- | :--- |
| **Room 1** | **"Hello World"** | 1x Training Slime (15 HP, 3 DMG flat) | Syntax of `ExecuteTurn()`, Target resolution, `if/else` | **Unlocks Section 1: Class Fields** + drafts 1 Float / 1 Int token |
| **Room 2** | **"The Variable Forge"** | 1x Shield Beetle (30 HP, rotates `[DEF 12]` / `[ATK 6]`) | How fields modify stats (`damageMultiplier`), bypassing shield with conditions | **Unlocks Section 3: `OnTakeDamage`** + drafts 1 Reaction token |
| **Room 3** | **"The Reaction Test"** | 1x Golem Charger (45 HP, charges 2 turns, hits for 20 DMG) | Event-driven defense: absorbing heavy hits via `OnTakeDamage()` | **Full Roguelike Unlocked** |
| **Room 4+**| **Scaling Dungeon** | Mixed swarms, glass cannons, elite tanks | Full build optimization (Thorns, Crit, Glass Cannon) | Rare / Epic / Legendary Token drafts |

---

## 6. Implementation Milestones for Architect Agent

### Milestone 1: Progressive Room Unlock System
- [ ] Implement `RoomProgressionManager` (tracks current room index: 1, 2, 3, 4+).
- [ ] In `CodeEditorPanelUI`:
  - Room 1: Only `ExecuteTurn()` is active. Fields and `OnTakeDamage` show commented locked state.
  - Room 2: Unlocks `Class Fields` UI block.
  - Room 3: Unlocks `OnTakeDamage()` UI block.

### Milestone 2: Class Fields & Variable Binding
- [ ] Add `damageMultiplier` (Float) and `baseShield` (Int) sockets to `CodeEditorPanelUI`.
- [ ] Connect values directly to `PlayerCombatController`:
  - `damageMultiplier` scales all player attacks.
  - `baseShield` gives starting armor at the beginning of each room.

### Milestone 3: Event-Driven `OnTakeDamage` Method
- [ ] Implement `OnTakeDamage(int incomingDamage)` in `PlayerCombatController`:
  - Triggers right before player HP is depleted during enemy attack turns.
  - Evaluates reaction condition (e.g., `incomingDamage >= 8`).
  - If true, executes defensive reaction action (e.g., `player.AddShield(10)`).
- [ ] Add educational console log:
  `[Event] OnTakeDamage(12) triggered! Condition 'incomingDamage >= 8' is TRUE -> Executed player.AddShield(10).`

### Milestone 4: UI Horizontal Layout & Typography Polish
- [ ] Restructure all `CodeSocketUI` elements into horizontal inline pills to eliminate text clipping.
- [ ] Ensure empty states render as clean dashed outlines (`[ Drop Condition ]`).
