# ARCHITECTURAL PLAN & SPECIFICATION (v23.0)
## Project: CodeForge — Console Viewport Padding & Health-Gated `OnTakeDamage` Reaction Logic

---

## 1. Executive Summary & Design Evaluation

### 1.1 Issue 1: Console Viewport First Line Clipping
* **The Symptom:** In `ConsoleLogUI`, the very first line of output is consistently cut off at the top.
* **Root Cause Analysis:**
  1. `ConsoleViewport` uses `RectMask2D` with `offsetMax = new Vector2(0f, -35f)` and `pivot = (0.5f, 1f)`.
  2. The text component (`logTextDisplay`) has `rectTransform.anchoredPosition = Vector2.zero` and `margin = Vector4.zero`.
  3. When content is at the top (`verticalNormalizedPosition == 1f` or when lines are few), the ascenders of the top line of text (font size 15.5) collide with and are masked by the upper boundary of `ConsoleViewport`.
* **The Solution:**
  - Increase top padding in `logTextDisplay.margin`: `logTextDisplay.margin = new Vector4(6f, 8f, 6f, 6f);`.
  - Adjust `ConsoleViewport` `offsetMax` to `new Vector2(0f, -38f)` to ensure a comfortable 8px clearance below `ConsoleHeader` (which sits at $Y = -8$ to $-30$).
  - Offset `logTextDisplay.rectTransform.anchoredPosition` to `new Vector2(0f, -4f)`.
  - When line count is small (content height $\le$ viewport height), ensure `ScrollRect` sits at top (`verticalNormalizedPosition = 1f`).

---

### 1.2 Issue 2: Health-Gated `OnTakeDamage` (HP vs. Shield Damage Reaction)
* **The Problem:** 
  The player's `OnTakeDamage(int incomingDamage)` callback was executing the `THEN` branch unconditionally or even when damage was 100% absorbed by shield.
* **The Pedagogical Concept (The Boolean Identity Switch):**
  - In educational programming, `OnTakeDamage` tests a condition: **"Did I lose actual health points?"**
  - **Shield Absorbs Hit ($0\text{ HP}$ lost):**
    - The attack did NOT penetrate health $\rightarrow$ `tookHealthDamage = false`.
    - If `Bool_true` is slotted (default): `(false == true)` $\rightarrow$ **`FALSE`** $\rightarrow$ executes **`ELSE`** branch (e.g. `Attack(target);` — counter-attack!).
    - If `Bool_false` is slotted: `(false == false)` $\rightarrow$ **`TRUE`** $\rightarrow$ executes **`THEN`** branch!
  - **Attack Penetrates Shield ($>0\text{ HP}$ lost):**
    - The attack damaged the player's core health $\rightarrow$ `tookHealthDamage = true`.
    - If `Bool_true` is slotted: `(true == true)` $\rightarrow$ **`TRUE`** $\rightarrow$ executes **`THEN`** branch (e.g. emergency `Defend();`).
    - If `Bool_false` is slotted: `(true == false)` $\rightarrow$ **`FALSE`** $\rightarrow$ executes **`ELSE`** branch!
  - By simply swapping `true` and `false` in `if ( [bool] )`, the student flips the logic between defensive turtle and counter-puncher!

---

## 2. Technical Specifications & File Edits

### 2.1 `Assets/Scripts/UI/ConsoleLogUI.cs`
1. In `SetupConsoleHierarchy()`:
   - Adjust `viewportRt.offsetMax = new Vector2(0f, -38f);`.
   - Set `logTextDisplay.margin = new Vector4(6f, 8f, 6f, 6f);`.
   - Set `logTextDisplay.rectTransform.anchoredPosition = new Vector2(0f, -4f);`.
2. In `AppendMessage()`:
   - If `currentLineCount <= 6`, clamp scroll position to `1f` (top) rather than forcing bottom, preventing single/double lines from jumping or clipping.

---

### 2.2 `Assets/Scripts/Combat/PlayerCombatController.cs`
1. In `HandleIncomingDamageReaction(int incomingDamage, CodeEditorPanelUI editorUI = null, List<EnemyEntity> activeEnemies = null)`:
   - Ensure the passed `incomingDamage` represents actual health lost (`hpLost`).
   - Update the console log to clearly explain the evaluation:
     ```csharp
     string dmgDesc = incomingDamage > 0 ? $"{incomingDamage} HP lost" : "0 HP lost (shield absorbed all damage)";
     ConsoleLogUI.Log($"[Event] OnTakeDamage({dmgDesc}): Condition '{condSyntax}' evaluated to <b>{(evalResult ? "<color=#98C379>TRUE</color>" : "<color=#E06C75>FALSE</color>")}</b> -> Executing {branchName} branch '{actionStr}'.");
     ```

---

### 2.3 `Assets/Scripts/UI/CodeEditorPanelUI.cs`
1. **Default Pre-slotted Tokens:**
   - Change `[SerializeField] private ConditionTokenSO defaultReactionCondition;` to `[SerializeField] private CodeTokenSO defaultReactionCondition;`.
   - Add `[SerializeField] private ActionTokenSO defaultReactionElseAction;`.
   - In `PrePopulateDefaultTokens()`:
     ```csharp
     SlotDefaultIfEmpty(reactionConditionSocket, defaultReactionCondition); // Assigns Bool_true
     SlotDefaultIfEmpty(reactionActionSocket, defaultReactionAction);       // Assigns Action_Defend
     SlotDefaultIfEmpty(reactionElseActionSocket, defaultReactionElseAction); // Assigns Action_Attack
     ```
2. **Evaluation in `EvaluateSingleSocket()`:**
   - Handle empty socket fallback: if socket is empty or null, treat slotted value as `true`.
   - When evaluating `CodeSocketRole.ReactionCondition` or `ReactionCondition2`:
     ```csharp
     bool tookHealthDamage = context != null && context.IncomingDamage > 0;
     bool expectedBool = socket.AssignedToken != null ? socket.AssignedToken.boolValue : true;
     bool result = (tookHealthDamage == expectedBool);
     return result;
     ```
3. **Starter Tokens:**
   - Ensure `Bool_false` is included in starter tokens / reward pool so the player can test swapping `if (true)` and `if (false)`.

---

### 2.4 `Assets/Scripts/Combat/EnemyEntity.cs`
- Verify lines 304–322 and lines 329–341:
  - Both `Attack` and `HeavyHit` calculate:
    ```csharp
    float hpBefore = target.CurrentHp;
    // ... deal damage ...
    float hpLost = Mathf.Max(0f, hpBefore - target.CurrentHp);
    if (target is PlayerCombatController playerCombat && !playerCombat.IsDead)
    {
        yield return StartCoroutine(playerCombat.HandleIncomingDamageReaction(Mathf.RoundToInt(hpLost)));
    }
    ```

---

## 3. Verification & Acceptance Checklist

- [x] **Console Viewport Top Line Padding:**
  - Launch battle and verify line 1 (e.g. `[Start] Initialized Player: MaxHP=20...`) has at least 8px top padding and is never clipped by `RectMask2D`.
- [x] **Shield Absorbed Attack (0 HP Lost):**
  - Player has 6 Shield, Enemy hits for 4 DMG (all absorbed, 0 HP lost).
  - With `if (true)` slotted: Condition evaluates to `FALSE`. Player executes `ELSE` branch (`Attack(target)`).
- [x] **Health Damage Taken (>0 HP Lost):**
  - Player has 0 Shield, Enemy hits for 4 DMG (4 HP lost).
  - With `if (true)` slotted: Condition evaluates to `TRUE`. Player executes `THEN` branch (`Defend(defendShield)`).
- [x] **Boolean Inversion with `if (false)`:**
  - Slot `false` into `if (...)` socket.
  - When Shield absorbs damage (0 HP lost): Condition evaluates to `TRUE`. Player executes `THEN` branch.
  - When HP is lost: Condition evaluates to `FALSE`. Player executes `ELSE` branch.
- [x] **Starter Inventory:**
  - Player starts with both `Bool_true` (pre-slotted in `OnTakeDamage`) and `Bool_false` in shelf inventory.
