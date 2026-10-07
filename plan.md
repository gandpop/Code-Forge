# ARCHITECTURAL PLAN & SPECIFICATION (v35.0)
## Project: CodeForge — Zero-Tofu Font Polish & Frictionless Room 1 Onboarding

---

## 1. Executive Summary & Root Cause Analysis

### 1.1 Root Cause 1: Unicode Emojis & Symbols Rendering as Missing Glyph Squares ("Tofu")
Unity TextMeshPro font assets (`LiberationSans SDF` and `BoldPixels_SDF`) only contain standard ASCII characters. When scripts attempt to render Unicode emojis (such as `🔒`, `🗑️`, `⚠️`, `🔄`, `📋`, `⏸`, `✕`, and bullets `•`), TextMeshPro cannot find the glyphs and substitutes Unicode character `\u25A1` (white rectangle square `□`), flooding the Unity Console with missing character warnings.

### 1.2 Root Cause 2: Room 1 Validation Friction (`Attack.damage` unassigned)
To reduce cognitive overload for new players, the `Attack()` method is folded by default in Room 1. However, `ValidatePreBattle()` requires `attackDamageSocket` to have a token assigned. If it is empty, compiling fails with:
`[Compiler Error] PlayerCombat.cs: Use of unassigned variable 'Attack.damage'. Please assign a token before compiling!`
Since `Attack()` is collapsed, a new player does not realize they need to expand `Attack()` and slot a number before they can even play Room 1.
**The Fix:** Pre-slot the starter damage token (`Int_8`, 8 DMG) into `attackDamageSocket` in `PrePopulateDefaultTokens()`. In Room 1, the player only needs to drag `Attack(target);` into `ExecuteTurn()`, creating a smooth, intuitive "Hello World" onboarding experience.

---

## 2. Technical Specifications & File Edits

### 2.1 Complete Unicode / Emoji Stripping (Pure ASCII Typography)

Replace all non-ASCII symbols with clean, authentic IDE programming typography:

1. **`Assets/Scripts/UI/EditorLineRowUI.cs` (Locked Method Rows):**
   - Replace `🔒 [{reasonTag}]` with:
     ```csharp
     codeTmp.text = $"<color=#E5C07B>[LOCKED: {reasonTag}]</color>  <color=#569CD6>{cleanCode}</color> <color=#5C6370>{{ ... }}</color>";
     ```

2. **`Assets/Scripts/UI/ShelfDiscardSlotUI.cs` (Trash Box):**
   - Replace `🗑️` and `✕` with:
     ```csharp
     promptText.text = "<b>DISCARD</b>\n<color=#E06C75><b>[ X ]</b></color>\n<size=75%><color=#858585>Drop token here</color></size>";
     ```

3. **`Assets/Scripts/UI/DiscardConfirmationModalUI.cs` (Confirm Modal):**
   - Replace `🗑️ CONFIRM TOKEN DISCARD` with:
     ```csharp
     headerText.text = "<color=#E06C75><b>CONFIRM TOKEN DISCARD</b></color>";
     ```

4. **`Assets/Scripts/UI/RewardPanelUI.cs` (Reward Screen):**
   - In unspent choice modal: Replace `⚠️ UNSPENT REWARD CHOICES` with:
     `headTmp.text = "<color=#FFCC00><b>[!] UNSPENT REWARD CHOICES</b></color>";`
   - In build peek: Replace `📋 Current Build & Shelf` with:
     `sb.AppendLine("<color=#61AFEF><b>// Current Build & Shelf</b></color>");`
   - Replace bullet points `•` with standard ASCII dashes `-`.

5. **`Assets/Scripts/UI/DefeatModalUI.cs` (Runtime Error Modal):**
   - Replace `⚠️ RUNTIME ERROR` with:
     `headerText.text = "<color=#FFCC00>[!] RUNTIME ERROR: EXECUTION TIMEOUT</color>";`
   - Replace `🔄 Retry Room` with:
     `txt.text = "Retry Room";`

6. **`Assets/Scripts/UI/DebuggerLocalsUI.cs` (Breakpoint Title):**
   - Replace `⏸ PAUSED AT BREAKPOINT` with:
     `titleText.text = $"<color=#E5C07B>[PAUSED AT BREAKPOINT]</color> <color=#858585>|</color> Line {line:D2}";`

7. **`Assets/Scripts/UI/DraggableTokenCardUI.cs` & `CodeEditorPanelUI.cs`:**
   - Replace bullet `•` with vertical bar `|` or dash `-`:
     `cardText.text = $"<size=85%><color={rarityHex}><b>[{Token.rarity.ToString().ToUpper()}]</b></color> | <color={typeColor}><b>{typeName}</b></color></size>\n...";`
   - In shelf header: Replace `•` with `-`:
     `shelfHeaderText.text = "// Token Inventory Shelf (Click to inspect - Drag to socket)";`

---

### 2.2 Frictionless Room 1 Onboarding Pre-Population

In `Assets/Scripts/UI/CodeEditorPanelUI.cs`:
- In `PrePopulateDefaultTokens()`:
  - Pre-slot `defaultAttackDamage` (or `Int_8`) into `attackDamageSocket` if empty:
    ```csharp
    public void PrePopulateDefaultTokens()
    {
        SlotDefaultIfEmpty(MaxHealthSocketUI, defaultMaxHealth);
        SlotDefaultIfEmpty(attackDamageSocket, defaultAttackDamage);
    }
    ```
  - Ensure `defaultAttackDamage` is referenced or loaded from `Int_8`.
  - When the player starts Room 1, `maxHealth` (20 HP) and `Attack.damage` (8 DMG) are already configured. The player's single task is dragging `Attack(target);` into `ExecuteTurn()` and clicking "Compile & Battle", guaranteeing a seamless first win.

---

## 3. Verification & Acceptance Checklist

- [ ] **Zero Missing Glyph Warnings:** Unity console produces 0 `The character with Unicode value was not found` warnings.
- [ ] **No Square Boxes / Tofu:** All locked headers, buttons, trash cards, and modals render crisp text without square symbols.
- [ ] **Room 1 Flow:** New game in Room 1 has 8 DMG pre-slotted in `Attack.damage`; slotting `Attack(target);` into `ExecuteTurn()` compiles and clears Room 1 with zero errors.
- [ ] **Token Discard Confirmation:** Discard confirmation modal displays clean text without missing glyphs.
