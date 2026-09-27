# ARCHITECTURAL PLAN & SPECIFICATION (v7.0)
## Project: CodeForge — Minimalist Starter Economy & Pure Assembly

---

## 1. Executive Summary & Design Pivot
* **The "Zero-Bloat" Starter Loop:** The player starts with **only `maxHealth = 20` pre-slotted**. All other sockets start **completely empty**.
* **Minimal Starter Shelf:** The player is given exactly **two tokens** in their starting inventory:
  1. `1x Int Token` (`Int 8`) $\rightarrow$ to be slotted into `Attack(target)`'s `damage` socket.
  2. `1x Action Token` (`Attack(target);`) $\rightarrow$ to be slotted into `ExecuteTurn()`'s action socket.
* **Safe Unslotted Defaults:** Empty sockets do not crash or halt compilation; they resolve to clean, safe fallbacks:
  - Empty `damageMultiplier` $\rightarrow$ defaults to `1.0f`
  - Empty `baseShield` $\rightarrow$ defaults to `0`
  - Empty `target` $\rightarrow$ defaults to `Enemies.Random()`
  - Empty `condition` $\rightarrow$ defaults to `true` (unconditional execution)
  - Empty `elseAction` $\rightarrow$ does nothing
  - Empty `OnTakeDamage` $\rightarrow$ does nothing
* **Enemy Fixes:** Revert charging position animation to prevent sprite drift and enemy overlap. Ensure fixed, clean horizontal/diagonal spacing for all room spawns.
* **Token Pruning:** Delete all pre-baked stat actions (`player.AddShield(10)`, `target.TakeDamage(14)`), duplicate `Int 20` tokens, and deprecated Stance assets.

---

## 2. Updated Script Architecture (`PlayerCombat.cs`)

```csharp
using UnityEngine;
using System.Collections.Generic;

public class PlayerCombat : MonoBehaviour
{
    // ========================================================
    // SECTION 1: STATS (Only maxHealth starts pre-slotted)
    // ========================================================
    [Header("Stats")]
    public int   maxHealth        = [ 20 ];          // Pre-slotted starter
    public float damageMultiplier = [ EMPTY -> 1.0f ]; // Empty default: 1.0x
    public int   baseShield       = [ EMPTY -> 0    ]; // Empty default: 0

    // ========================================================
    // SECTION 2: MODULAR COMBAT METHODS (Abilities)
    // ========================================================
    public void Attack(Enemy target)
    {
        int damage = [ INT_SOCKET ]; // Slot starter 'Int 8' here!
        target.TakeDamage(damage);
    }

    public void Defend()
    {
        int shield = [ INT_SOCKET ]; // Empty at start; unlock tokens later
        player.AddShield(shield);
    }

    // ========================================================
    // SECTION 3: LIFECYCLE INITIALIZATION
    // ========================================================
    void Start()
    {
        player.SetMaxHealth(maxHealth);
        player.AddStartingShield(baseShield);
    }

    // ========================================================
    // SECTION 4: MAIN TURN METHOD
    // ========================================================
    public void ExecuteTurn()
    {
        var target = [ TARGET_SOCKET ]; // Defaults to Enemies.Random() if empty

        if ( [ CONDITION_SOCKET ] )    // Defaults to 'true' if empty
        {
            [ ACTION_THEN_SOCKET ];    // Slot starter 'Attack(target);' here!
        }
        else
        {
            [ ACTION_ELSE_SOCKET ];    // Empty default: no-op
        }
    }

    // ========================================================
    // SECTION 5: EVENT CALLBACK (Defense on Enemy Turn)
    // ========================================================
    public void OnTakeDamage(int incomingDamage)
    {
        if ( [ REACTION_COND_SOCKET ] )
        {
            [ REACTION_ACTION_SOCKET ]; // Empty at start; unlock tokens later
        }
    }
}
```

---

## 3. Starter Token Shelf & Inventory

The player begins Room 1 with **ONLY** these 2 tokens in their inventory shelf:
1. `Int_8` (`int`: 8)
2. `Action_AttackTarget` (`action`: `Attack(target);`)

### What the Player Does on Turn 0:
1. Drags `Int 8` into `Attack(target) { int damage = [ 8 ]; }`.
2. Drags `Attack(target);` into `ExecuteTurn() { [ Attack(target); ] }`.
3. Hits **COMPILE & BATTLE**.
4. Defeats Room 1's Training Slime (15 HP) in 2–3 rounds!
5. **Reward Screen:** Drafts their first new tokens (e.g. `Defend();`, `Condition_ShieldCheck`, `Int 10`, or `Float 1.3`).

---

## 4. Token Cleanup List (`Assets/ScriptableObjects/Tokens/`)

Delete or purge from reward pool:
- ❌ All Stance assets: `Stance_Balanced`, `Stance_Berserk`, `Stance_Guardian`
- ❌ Hardcoded action assets: `Action_DefensiveGuard` (since `Defend()` replaces it), `Action_HeavyStrike`
- ❌ Duplicate `Int_20` tokens (keep only one in project for `maxHealth`)
- Keep modular actions: `Action_AttackTarget` (`Attack(target);`), `Action_Defend` (`Defend();`), `Action_PierceTarget` (`target.PierceDamage(8);`)

---

## 5. Enemy Positioning & Animation Fixes (`CodeForge.Combat`)

1. **Revert Charge Lunge Drift:**
   * Remove `PerformAttackLunge` from `EnemyIntentType.Charge` in `EnemyEntity.cs`. Charging enemies stay firmly in their spawn positions without position offsets.
2. **Fixed Enemy Spawn Spacing:**
   * Ensure enemy spawn coordinates in `CombatManager.cs` are spaced so bounding boxes and intent plates never overlap:
     - Single enemy (Rooms 1–3): `Vector3(2.5f, 0.8f, 0f)`
     - Two enemies (Room 4+):
       - Frontliner: `Vector3(1.6f, 0.4f, 0f)`
       - Backliner: `Vector3(3.2f, 1.4f, 0f)`
3. **Intent Plate Visibility:**
   * Intent badges render above the enemy cube with clear z-sorting so they never occlude adjacent enemies.

---

## 6. Implementation Milestones for Architect Agent

### Milestone 1: Enemy Spawning & Position Fix
- [ ] Remove charge lunge drift from `EnemyEntity.cs`.
- [ ] Adjust enemy spawn offsets in `CombatManager.cs` so enemies never overlap.

### Milestone 2: Empty Socket Fallbacks
- [ ] Ensure all sockets except `maxHealth` start completely empty.
- [ ] Implement safe fallbacks for empty sockets: `damageMultiplier = 1.0f`, `baseShield = 0`, `target = Random`, `condition = true`, `else = null`.

### Milestone 3: Starter Inventory Pruning
- [ ] Configure `starterTokens` in `CodeEditorPanelUI` to provide ONLY: `Int 8` and `Action_AttackTarget`.
- [ ] Pre-populate ONLY `maxHealth = 20`.

### Milestone 4: Asset & Token Pool Cleanup
- [ ] Delete deprecated Stance tokens and hardcoded stat action tokens (`Action_DefensiveGuard`, duplicate `Int 20`).
- [ ] Verify reward generation only drops valid modular actions (`Attack(target);`, `Defend();`, `Pierce(target);`), integers, floats, conditions, and targeting rules.
