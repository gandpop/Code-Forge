# ARCHITECTURAL PLAN & SPECIFICATION (v9.0)
## Project: CodeForge — UI Row Instantiation & Full Screen Backdrop Fix

---

## 1. Executive Summary & Core Fixes
* **The "Missing Rows" Fix:** The C# logic for `critChance`, `critDamage`, `evasionChance`, `applyBleed`, and `reactionElse` is already implemented in `PlayerCombatController.cs` and `ActionTokenSO.cs`. However, their **UI GameObjects do not exist in the scene hierarchy**. The Architect must dynamically instantiate or assemble these UI row GameObjects into the `Content` container of `CodeEditorPanelUI` on `Awake()`.
* **The "Popover Backdrop" Fix:** The click-outside backdrop in `IntelliSensePopoverUI.cs` was incorrectly parented to the local popover box (460x327 px). It must be parented to the **Root Canvas** so it spans the full screen (`anchorMin = (0,0), anchorMax = (1,1)`) and reliably catches all outside clicks.

---

## 2. Full Script Architecture Required on Screen (`PlayerCombat.cs`)

When the player scrolls through the editor, all of the following lines must be physically visible and socketable:

```csharp
using UnityEngine;
using System.Collections.Generic;

public class PlayerCombat : MonoBehaviour
{
    // ========================================================
    // SECTION 1: STATS & ATTRIBUTES
    // ========================================================
    [Header("Stats")]
    public int   maxHealth        = [ 20 ];          // Pre-slotted starter
    public float damageMultiplier = [ EMPTY -> 1.0f ];
    public float critChance       = [ EMPTY -> 0.0f ]; // e.g. [ float ]
    public int   critDamage       = [ EMPTY -> 0    ]; // e.g. [ int ]
    public float evasionChance    = [ EMPTY -> 0.0f ]; // e.g. [ float ]
    public int   baseShield       = [ EMPTY -> 0    ]; // e.g. [ int ]

    // ========================================================
    // SECTION 2: MODULAR COMBAT METHODS
    // ========================================================
    public void Attack(Enemy target)
    {
        int damage = [ INT_SOCKET ]; // e.g. [ int ]
        target.TakeDamage(damage);

        if ( [ BOOL_SOCKET: applyBleed ] ) // e.g. [ bool ]
        {
            target.ApplyBleed([ INT: dmgPerTurn ], [ INT: duration ]);
        }
    }

    public void Defend()
    {
        int shield = [ INT_SOCKET ]; // e.g. [ int ]
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
        var target = [ TARGET_SOCKET ]; // Defaults to Enemies.Random()

        if ( [ CONDITION_SOCKET ] )    // Defaults to 'true'
        {
            [ ACTION_THEN_SOCKET ];    // e.g. [ Attack(target) ]
        }
        else
        {
            [ ACTION_ELSE_SOCKET ];    // e.g. [ Defend() ]
        }
    }

    // ========================================================
    // SECTION 5: EVENT CALLBACK (Defense on Enemy Turn)
    // ========================================================
    public void OnTakeDamage(int incomingDamage)
    {
        if ( [ REACTION_COND_SOCKET ] ) // e.g. [ incomingDamage >= 8 ]
        {
            [ REACTION_ACTION_THEN ];   // e.g. [ Defend() ]
        }
        else
        {
            [ REACTION_ACTION_ELSE ];   // e.g. [ Attack(target) ]
        }
    }
}
```

---

## 3. UI Row Assembly / Dynamic Generation (`CodeForge.UI`)

To ensure the missing lines appear without requiring manual Unity Editor GUI drag-and-drop:
* In `CodeEditorPanelUI.cs`:
  - When `EnsureAllSockets()` runs, if any socket (`critChanceSocket`, `critDamageSocket`, `evasionChanceSocket`, `applyBleedSocket`, `bleedDamageSocket`, `bleedDurationSocket`, `reactionElseActionSocket`) is `null`:
    - Clone an existing line row (e.g. clone the `damageMultiplier` row for floats, or `baseShield` row for ints).
    - Insert it at the correct sibling index in the `Content` container.
    - Set its text label:
      - `"public float critChance        ="`
      - `"public int   critDamage        ="`
      - `"public float evasionChance     ="`
      - `"if ( [bool] ) target.ApplyBleed( [int], [int] );"`
      - `"else { [action] }"` inside `OnTakeDamage`
    - Configure the cloned `CodeSocketUI`:
      - Set `socketRole` and `expectedType`.
      - Wire back-reference to `CodeEditorPanelUI`.

---

## 4. Full-Screen Backdrop Fix (`IntelliSensePopoverUI.cs`)

Fix `EnsureBackdrop()` in `IntelliSensePopoverUI.cs`:
```csharp
private void EnsureBackdrop()
{
    if (backdropObj != null) return;

    // Must be parented to the ROOT CANVAS, not the local popover rect!
    Canvas rootCanvas = GetComponentInParent<Canvas>();
    Transform parent = rootCanvas != null ? rootCanvas.transform : transform.parent;

    backdropObj = new GameObject("IntelliSenseBackdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
    backdropObj.transform.SetParent(parent, false);

    // Position directly behind popoverRect in hierarchy
    int popoverIndex = transform.GetSiblingIndex();
    backdropObj.transform.SetSiblingIndex(Mathf.Max(0, popoverIndex));

    var rect = backdropObj.GetComponent<RectTransform>();
    rect.anchorMin = Vector2.zero;
    rect.anchorMax = Vector2.one;
    rect.sizeDelta = Vector2.zero;
    rect.anchoredPosition = Vector2.zero;

    var img = backdropObj.GetComponent<Image>();
    img.color = new Color(0f, 0f, 0f, 0.001f); // Invisible raycast shield
    img.raycastTarget = true;

    var btn = backdropObj.GetComponent<Button>();
    btn.transition = Selectable.Transition.None;
    btn.onClick.AddListener(Hide);
}
```

---

## 5. Verification Checklist for Architect Agent
- [ ] Enter Play Mode:
  - Scroll down the script: Verify `critChance`, `critDamage`, `evasionChance`, `applyBleed`, and `reactionElse` are physically visible in the code editor.
  - Verify line numbers are continuous (`01`, `02` ... `40+`).
- [ ] Click any socket to open IntelliSense:
  - Click anywhere on the background (e.g. on the arena or empty space).
  - Verify the popover immediately closes.
- [ ] Click "Compile & Battle":
  - Verify battle resolves cleanly with `Int 8` in `Attack` and `Attack(target)` in `ExecuteTurn`.
