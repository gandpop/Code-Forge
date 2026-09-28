# ARCHITECTURAL PLAN & SPECIFICATION (v10.0)
## Project: CodeForge — Mathematical Probability Scaling & Polish

---

## 1. Executive Summary & Design Solutions
* **The Float Chance Exploit Solution:** Float tokens (`1.25f`, `1.50f`, `2.0f`) are scaled into realistic probabilities using visible C# arithmetic in the editor script:
  - `public float critChance    = [ float ] * 0.10f;` (e.g. `1.25f * 0.10f` = 12.5% crit)
  - `public float evasionChance = [ float ] * 0.10f; // (Max: 50%)`
* **Transparent 50% Evasion Cap:** 
  - To communicate the 50% maximum dodge cap clearly to the player:
    1. The code row displays an inline comment: `// (Max: 50%)`.
    2. IntelliSense popover documentation states: `"Scales float token by 0.10f into dodge probability (e.g. 1.50f -> 15.0%). Capped at 50.0%."`
    3. Console log reports the exact calculation and clamp:
       `[Start] Evasion calculated as 15.0% (Max Cap: 50%).`
       If exceeded (e.g. 6.0f token): `[Warning] Evasion calculated as 60.0% -> Clamped to 50.0% maximum cap!`
* **Planning Phase Health Bar Sync:** `PlayerHealthPlateUI` must update immediately upon room start / planning phase to reflect `20 / 20 HP` instead of defaulting to `100 / 100 HP`.
* **Scroll Content Bottom Padding:** Add 45px bottom padding to the `Content` container of `EditorScroll` so `OnTakeDamage`'s `else` row does not collide with the bottom edge.

---

## 2. Updated Script Architecture (`PlayerCombat.cs`)

```csharp
using UnityEngine;
using System.Collections.Generic;

public class PlayerCombat : MonoBehaviour
{
    // ========================================================
    // SECTION 1: STATS & ATTRIBUTES
    // ========================================================
    [Header("Stats")]
    public int   maxHealth        = [ 20 ];
    public float damageMultiplier = [ EMPTY -> 1.0f ];
    public float critChance       = [ EMPTY -> 0.0f ] * 0.10f;
    public int   critDamage       = [ EMPTY -> 0    ];
    public float evasionChance    = [ EMPTY -> 0.0f ] * 0.10f; // (Max: 50%)
    public int   baseShield       = [ EMPTY -> 0    ];

    // ========================================================
    // SECTION 2: MODULAR COMBAT METHODS
    // ========================================================
    public void Attack(Enemy target)
    {
        int damage = [ INT_SOCKET ];
        target.TakeDamage(damage);

        if ( [ BOOL_SOCKET: applyBleed ] )
        {
            target.ApplyBleed([ INT: dmgPerTurn ], [ INT: duration ]);
        }
    }

    public void Defend()
    {
        int shield = [ INT_SOCKET ];
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
    // SECTION 5: EVENT CALLBACK (Defense on Enemy Turn)
    // ========================================================
    public void OnTakeDamage(int incomingDamage)
    {
        if ( [ REACTION_COND_SOCKET ] )
        {
            [ REACTION_ACTION_THEN ];
        }
        else
        {
            [ REACTION_ACTION_ELSE ];
        }
    }
}
```

---

## 3. Mathematical Probability & Clamp Logic (`CodeForge.Combat`)

In `PlayerCombatController.cs` and `CodeEditorPanelUI.cs`:

### 3.1 Crit Chance Calculation
```csharp
public float GetCritChance()
{
    if (critChanceSocket == null || critChanceSocket.AssignedToken == null) return 0.0f;
    float rawTokenVal = critChanceSocket.AssignedToken.floatValue;
    return rawTokenVal * 0.10f; // e.g. 1.25f * 0.10f = 0.125f (12.5%)
}
```

### 3.2 Evasion Chance Calculation with 50% Hard Cap
```csharp
public float GetEvasionChance()
{
    if (evasionChanceSocket == null || evasionChanceSocket.AssignedToken == null) return 0.0f;
    float rawTokenVal = evasionChanceSocket.AssignedToken.floatValue;
    float calculatedEvasion = rawTokenVal * 0.10f; // e.g. 1.50f * 0.10f = 0.15f (15%)

    if (calculatedEvasion > 0.50f)
    {
        ConsoleLogUI.Log($"<color=#E5C07B>[Warning] Evasion {calculatedEvasion * 100:0.0}% exceeds maximum limit. Clamped to 50.0%!</color>");
        return 0.50f;
    }

    return calculatedEvasion;
}
```

---

## 4. UI Polish Details (`CodeForge.UI`)

1. **Syntax Text Update on Sockets:**
   - Update the line text for `critChance`: `"public float critChance       ="` followed by socket, followed by `" * 0.10f;"`.
   - Update the line text for `evasionChance`: `"public float evasionChance    ="` followed by socket, followed by `" * 0.10f; // (Max: 50%)"`.
2. **Health Plate Initialization:**
   - In `PlayerHealthPlateUI.cs` or `CombatManager.EnterPlanningPhase()`:
     - Immediately invoke `UpdateHealthDisplay(player.CurrentHp, player.MaxHp)` so the HUD displays `20 / 20 HP` from the very first frame of planning.
3. **EditorScroll Padding:**
   - On the `Content` GameObject of `EditorScroll`, ensure the `VerticalLayoutGroup` has `padding.bottom = 45`.

---

## 5. Verification Checklist for Architect Agent
- [ ] Inspect Editor: Verify `critChance` displays `* 0.10f;` and `evasionChance` displays `* 0.10f; // (Max: 50%)`.
- [ ] Slot `1.25f` into `evasionChance`:
  - Run battle.
  - Verify console logs: `Evasion calculated as 12.5% (Max Cap: 50%)`.
- [ ] Verify HUD health displays `20 / 20 HP` on game load before clicking Compile.
- [ ] Scroll to the bottom of the script: Verify `OnTakeDamage` has 45px of breathing room from the bottom edge.
