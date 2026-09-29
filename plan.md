# ARCHITECTURAL PLAN & SPECIFICATION (v17.0)
## Project: CodeForge — Multiplier Compounding, Flat Armor Int, Reward Duplication Elimination, Console Auto-Scroll & Popover Polish

---

## 1. Executive Summary & Design Evaluation

### 1.1 Pedagogical Critique: Float Identity & The Multiplier King
* **The Problem:** Using `float` for `damageReduction` as a decimal fraction (`0.10f` - `0.50f`) was cognitively jarring for novice programmers. Learners expect integer percentages or flat armor, and slotting `1.1f` into a damage reduction slot triggered awkward clamping warnings. With the removal of `0.x` fractional floats, learners risked feeling that floats lacked purpose in the script.
* **The Solution — Multiplicative Synergy:**
  1. **Convert `damageReduction` to Flat Armor (`int`):**
     - Variable definition: `int damageReduction = [ 3 ];`
     - Damage taken formula: `damageTaken = Mathf.Max(1, incomingDamage - damageReduction);`
     - Clear pedagogical convention: Integers are for discrete quantities (Health, Armor, Shield, Turn Counts, Dice).
  2. **Floats as Exponential Scaling Levers:**
     - Floats now exclusively represent continuous scale factors and multipliers:
       `float damageMultiplier = [ 1.25f ];`
       `float critMultiplier = [ 2.00f ];`
     - **Compounding Critical Strike Formula:**
       $$\text{Final Damage} = (\text{baseDamage} \times \text{damageMultiplier}) \times \text{critMultiplier}$$
       *Example:* Slotted `baseDamage = 10`, `damageMultiplier = 1.25f`, `critMultiplier = 1.80f`.
       Normal Strike: $10 \times 1.25 = 12.5 \approx 13\text{ DMG}$.
       Critical Strike: $(10 \times 1.25) \times 1.80 = 22.5 \approx 23\text{ DMG}$!
     - Multipliers compound multiplicatively rather than additively, turning `float` cards into the most exciting offensive build rewards in the game.

### 1.2 Token Documentation Popover 'X' Raycast Fix
* **The Issue:** Clicking the 'X' button on the `TokenInspectorPopoverUI` did nothing, while clicking outside dismissed the popover.
* **Root Cause:**
  1. The child GameObject `Txt` containing `TextMeshProUGUI` had `raycastTarget = true` by default, intercepting pointer clicks and preventing the underlying `Button` from receiving click events.
  2. If the popover already existed in the hierarchy at runtime, `BuildUIHierarchyIfNeeded()` returned early, bypassing the programmatic listener attachment for `closeButton`.
* **Fix:** Explicitly set `closeTxt.raycastTarget = false;` and ensure `closeButton.onClick.AddListener(Hide)` is idempotently hooked in `Awake()`.

### 1.3 Battle Console Auto-Scroll & Off-Screen Overflow Fix
* **The Issue:** After combat fills the console with ~15–20 lines of text, dragging an invalid token (e.g. `int` into `float` slot) produced no visible compiler error.
* **Root Cause:**
  1. In `ConsoleLogUI.Awake()`, `logTextDisplay.rectTransform` was stretched to static anchors (`0.02f` to `0.98f`) without a `ContentSizeFitter`.
  2. Because the content RectTransform height remained statically locked to the viewport height (~120px), `ScrollRect.verticalNormalizedPosition` could not scroll.
  3. Subsequent text lines rendered downward past the bottom clipping plane into invisible space.
* **Fix:** Add a `ContentSizeFitter` (`verticalFit = PreferredSize`) to `logTextDisplay`, set pivot to `(0.5f, 1f)` (top), assign `logScrollRect.viewport = logScrollRect.GetComponent<RectTransform>()`, and scroll to `verticalNormalizedPosition = 0f` whenever new messages are appended.

### 1.4 Reward Draft Duplicate Card Elimination
* **The Issue:** Selecting 2 rewards and clicking `Accept Rewards` added 4 cards (the 2 chosen + 2 exact duplicates) to the inventory.
* **Root Cause:**
  1. The `ConfirmRewardsButton` in `MainPrototype.unity` had a persistent UnityEvent targeting `ConfirmSelection()`.
  2. `EnsureConfirmButton()` in `RewardPanelUI.cs` added a second dynamic listener via `confirmRewardsButton.onClick.AddListener(ConfirmSelection)`.
  3. `RemoveAllListeners()` does *not* strip persistent scene serialized calls, causing `ConfirmSelection()` to execute twice simultaneously.
  4. `selectedTokens` was not cleared between invocations, importing both tokens twice into `codeEditorUI.AddTokenToInventory()`.
* **Fix:** Add an `isConfirming` debounce guard, clear `selectedTokens` immediately, and avoid duplicate listener registrations. Additionally, ensure `DrawWeightedToken` pulls unique items from `eligibleTokens` so drafts never offer identical cards.

---

## 2. Technical Specifications & Architecture

### 2.1 Critical Strike & Damage Compounding (`ActionTokenSO.cs` & `PlayerCombatController.cs`)

#### 2.1.1 `ActionTokenSO.cs` Damage Pipeline
Update combat resolution in `ActionTokenSO.Execute(...)`:
```csharp
bool isCrit = context.Player != null && context.Player.CritChancePercent > 0 && (Random.Range(0, 100) < context.Player.CritChancePercent);
float dmgMult = context.Player != null ? context.Player.DamageMultiplier : 1.0f;
float critMult = isCrit ? (context.Player != null ? context.Player.CritMultiplier : 1.5f) : 1.0f;

// Compound formula: (base * damageMultiplier) * critMultiplier
float baseScaled = actionValue * dmgMult;
float finalDamage = baseScaled * critMult;

if (isCrit)
{
    ConsoleLogUI.Log($"<color=#E5C07B>[Combat] CRITICAL STRIKE! ({actionValue} * {dmgMult:0.0#}x) * {critMult:0.0#}x = {Mathf.RoundToInt(finalDamage)} DMG!</color>");
}
```

---

### 2.2 Flat Armor Damage Reduction (`CodeSocketRole.cs`, `CodeEditorPanelUI.cs`, `PlayerCombatController.cs`)

#### 2.2.1 Socket Role & DataType Updates
* Change `damageReductionSocket` expected type to `CodeTokenType.Int`.
* Update `CodeSocketRole.DamageReduction` handling across `CodeEditorPanelUI.cs` to return `int`:
```csharp
public int GetDamageReduction()
{
    if (damageReductionSocket != null && damageReductionSocket.AssignedToken != null)
    {
        return Mathf.Max(0, damageReductionSocket.AssignedToken.intValue);
    }
    return 0;
}
```

#### 2.2.2 Flat Armor Mitigation in `PlayerCombatController.cs`
```csharp
public void TakeDamage(float amount, bool isPiercing = false)
{
    if (amount <= 0f) return;

    // Check Evasion
    if (!isPiercing && EvasionChancePercent > 0 && Random.Range(0, 100) < EvasionChancePercent)
    {
        ConsoleLogUI.Log($"<color=#98C379>[Combat] Player EVADED incoming attack! ({EvasionChancePercent}% Evasion)</color>");
        return;
    }

    float incoming = amount;
    if (!isPiercing && DamageReduction > 0)
    {
        float reduced = Mathf.Max(1f, incoming - DamageReduction);
        ConsoleLogUI.Log($"[Combat] Armor mitigated {incoming - reduced:0} DMG ({DamageReduction} flat Armor). Incoming: {reduced:0} DMG.");
        incoming = reduced;
    }

    // Shield absorption then HP deduction...
}
```

---

### 2.3 Token Inspector Popover 'X' Button (`TokenInspectorPopoverUI.cs`)

```csharp
private void Awake()
{
    if (instance == null) instance = this;
    else if (instance != this) { Destroy(gameObject); return; }

    BuildUIHierarchyIfNeeded();
    ApplyLayoutDimensions();
    EnsureBackdrop();

    // Guarantee close button is wired and non-blocking
    if (closeButton != null)
    {
        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(Hide);

        var childTxt = closeButton.GetComponentInChildren<TextMeshProUGUI>();
        if (childTxt != null)
        {
            childTxt.raycastTarget = false; // Prevent eating pointer clicks
        }
    }
}
```

---

### 2.4 Console Log UI Auto-Scroll & Viewport Setup (`ConsoleLogUI.cs`)

```csharp
private void Awake()
{
    instance = this;
    if (logScrollRect == null)
    {
        logScrollRect = GetComponentInChildren<ScrollRect>(true) ?? GetComponentInParent<ScrollRect>();
    }

    if (logTextDisplay != null)
    {
        logTextDisplay.fontSize = 15.5f;
        logTextDisplay.lineSpacing = 4f;

        var fitter = logTextDisplay.GetComponent<ContentSizeFitter>();
        if (fitter == null) fitter = logTextDisplay.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        var rt = logTextDisplay.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = Vector2.zero;
    }

    if (logScrollRect != null && logTextDisplay != null)
    {
        logScrollRect.content = logTextDisplay.rectTransform;
        if (logScrollRect.viewport == null)
        {
            logScrollRect.viewport = logScrollRect.GetComponent<RectTransform>();
        }
    }
}
```

---

### 2.5 Reward Selection Duplication Elimination (`RewardPanelUI.cs`)

```csharp
private bool isConfirming = false;

public void ShowRewardPrompt(int roomIndex)
{
    isConfirming = false;
    selectedTokens.Clear();
    // ...
}

public void ConfirmSelection()
{
    if (isConfirming) return;
    if (selectedTokens.Count == 0)
    {
        ConsoleLogUI.Log("<color=#E5C07B>[Reward] Please select at least 1 token before accepting!</color>");
        return;
    }

    isConfirming = true;
    if (codeEditorUI == null) codeEditorUI = FindFirstObjectByType<CodeEditorPanelUI>(FindObjectsInactive.Include);

    // Copy selected tokens and clear to prevent duplicate delivery
    var tokensToAdd = new List<CodeTokenSO>(selectedTokens);
    selectedTokens.Clear();

    foreach (var token in tokensToAdd)
    {
        if (token != null && codeEditorUI != null)
        {
            codeEditorUI.AddTokenToInventory(token);
        }
    }

    ConsoleLogUI.Log($"[Reward] Successfully drafted {tokensToAdd.Count} token(s) into inventory!");
    Hide();

    if (CombatManager.Instance != null)
    {
        CombatManager.Instance.AdvanceToNextRoom();
    }
}
```

---

## 3. Verification & Acceptance Checklist

- [ ] **Crit Multiplier Compounding:** Slotting `damageMultiplier = 1.25f` and `critMultiplier = 2.0f` on a 10 damage attack deals $(10 \times 1.25) \times 2.0 = 25\text{ DMG}$ on crit, clearly reported in the combat log.
- [ ] **Flat Armor `damageReduction`:** Slotting an integer token into `damageReduction` subtracts flat damage per incoming hit (minimum 1 DMG). No float clamping or decimal conversion.
- [ ] **Token Inspector 'X' Button:** Clicking the top-right 'X' button immediately closes the popover. Clicking outside also closes the popover.
- [ ] **Console Error Visibility:** When dragging an `int` token into a `float` socket after battle (when console has 20+ lines), the viewport scrolls to the bottom so `[Compiler Error] CS0029` is immediately visible.
- [ ] **Reward Draft No Duplicates:** Drafting 2 reward cards adds exactly 2 cards to the inventory shelf (no 4-card duplication).
