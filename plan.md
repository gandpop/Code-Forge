# ARCHITECTURAL PLAN & SPECIFICATION (v30.0)
## Project: CodeForge — Universal Text & Typography Editability in Editor & Inspector

---

## 1. Executive Summary & Problem Diagnosis

### 1.1 Root Cause: User Blocked From Editing Text in Unity Editor
The user explicitly requested:
> *"I also wanna be able to edit all text which i cant. Make the prompt"*

During investigation, we identified why editing text string values and properties (font size, font, color, alignment) was completely blocked:
1. **Continuous Edit-Mode Overwrite Loops (`Update()` in Edit Mode):**
   In `PlayerHealthPlateUI.cs`, an `#if UNITY_EDITOR Update()` method runs at 60 FPS in Edit Mode, constantly calling `RefreshDisplay()` and `EnsureFont()`. This forcibly overwrote `hpText.text = $"<size=115%><b>PLAYER</b></size> 20/20 HP"` and `hpText.fontSize = 18f;` every frame (every 16ms). Any text typed or property adjusted by the user in the Inspector was instantly erased.
2. **Aggressive `[ExecuteAlways]` Component Resets:**
   In `ConsoleLogUI.cs`, `SetupConsoleHierarchy()` was overwriting `headerText.text`, `headerText.fontSize`, `headerText.font`, `logTextDisplay.fontSize`, `lineSpacing`, and `margin`.
3. **Prefab & Entity Overwrites:**
   In `EnemyHealthBarUI.cs` and `EnemyEntity.cs`, `nameAndHpText` and `IntentText` were having their fonts, alignments, and text strings continuously overwritten by scripts.

---

### 1.2 The Architecture Fix: Universal Designer Freedom (Inspector-First)
To make **ALL text editable** by the user in the Unity Editor:
1. **Kill Continuous Edit-Mode Text Overwrites:**
   - Remove the edit-mode `Update()` loop from `PlayerHealthPlateUI.cs`. Text content updates (`hpText.text = ...`) must **only** occur during runtime (`Application.isPlaying`) when health or shield actually changes.
   - In Edit Mode, text typed by the designer in the Inspector must remain untouched and persistent.
2. **Preserve Designer Text Strings:**
   - For `ConsoleHeader`: never overwrite `headerText.text` if the user has written a custom header string.
   - For `HPText`: preserve whatever text string or preview the designer sets in the Inspector during edit mode.
   - For `Label` in `EnemyPrefab.prefab`: allow the designer to set whatever placeholder text and styling they prefer.
3. **Preserve Designer Typography Properties:**
   - Scripts must **never** hardcode `fontSize`, `font`, `alignment`, `color`, `margins`, or `lineSpacing` on existing TextMeshPro components.
   - Scripts only supply safe defaults when a component is created from scratch (e.g. `if (tmp.fontSize <= 0f)`).
4. **Battle Console Font Reversion:**
   - Revert `ConsoleHeader` and `LogTextDisplay` back to standard Unity default font (`LiberationSans SDF`).

---

## 2. Technical Specifications & File Edits

### 2.1 `Assets/Scripts/UI/PlayerHealthPlateUI.cs`
- **Remove Edit-Mode `Update()` Loop:**
  - Delete lines 240–250 (`#if UNITY_EDITOR private void Update() { ... } #endif`). This stops the 60 FPS edit-mode loop that wiped out user typing and font size tweaks.
- **In `EnsureFont()`:**
  - Do NOT hardcode `hpText.fontSize = 18f;` (or line 132 `hpText.fontSize = 20f;`).
  - Only assign `hpText.font = pixelFont;` if `hpText.font == null`.
- **In `RefreshDisplay()`:**
  - When in edit mode (`!Application.isPlaying`), do NOT overwrite `hpText.text`. Allow the designer to type any preview or label text in the Inspector.
  - When in play mode (`Application.isPlaying`), update `hpText.text` dynamically based on current HP/Shield without altering the component's font size or styling.

### 2.2 `Assets/Scripts/UI/ConsoleLogUI.cs`
- **Remove `pixelFont` & Revert to Old Font:**
  - Remove `[SerializeField] private TMP_FontAsset pixelFont;` and the `#if UNITY_EDITOR` block loading `BoldPixels_SDF.asset`.
  - Revert `headerText.font` and `logTextDisplay.font` to `TMPro.TMP_Settings.defaultFontAsset` (`LiberationSans SDF`).
- **Make Text Content & Properties Editable:**
  - In `SetupConsoleHierarchy()`:
    - Only assign default `headerText.text` if `string.IsNullOrEmpty(headerText.text)`. Never overwrite existing text typed by the user.
    - Only set default `headerText.fontSize = 14f;` if `headerText.fontSize <= 0f`.
    - Only set default `logTextDisplay.fontSize = 14.5f;` if `logTextDisplay.fontSize <= 0f`.
    - Only set default `logTextDisplay.lineSpacing = 4f;` if `logTextDisplay.lineSpacing == 0f`.
    - Do NOT hardcode or reset `logTextDisplay.margin` if already configured in the Inspector.

### 2.3 `Assets/Scripts/UI/EnemyHealthBarUI.cs`
- In `EnsureFontAndFill()`:
  - Only assign `nameAndHpText.font = pixelFont` if `nameAndHpText.font == null`.
  - Do NOT overwrite `nameAndHpText.fontSize`, `alignment`, or `textWrappingMode` in code.
- In `RefreshDisplay()`:
  - If `!Application.isPlaying`, do NOT overwrite `nameAndHpText.text` so the designer can freely edit and preview enemy health text in `EnemyPrefab.prefab`.
  - Maintain right-to-left health bar fill setup:
    `fillImage.sprite = PlayerHealthPlateUI.GetOrCreateSquareSprite();`
    `fillImage.type = Image.Type.Filled;`
    `fillMethod = Image.FillMethod.Horizontal;`
    `fillOrigin = (int)Image.OriginHorizontal.Left;`

### 2.4 `Assets/Scripts/Combat/EnemyEntity.cs`
- In `EnsureIntentPlateUI()`:
  - Only assign `tmp.fontSize = 12f;` and initial font when creating `IntentText` for the first time.
  - If `IntentText` already exists on the prefab or canvas, **do not** overwrite its `text`, `fontSize`, `alignment`, or `font`.

---

## 3. Verification & Acceptance Checklist

- [x] **All Text Content Editable in Inspector:**
  - User can select `ConsoleHeader` and edit the text string without it being reset.
  - User can select `HPText` and edit the text string without it being wiped out by an edit-mode `Update()` loop.
  - User can select `EnemyPrefab -> Label` and edit the text string without script overwrite.
- [x] **All Text Properties Editable in Inspector:**
  - User can adjust `fontSize`, `font`, `color`, `alignment`, `lineSpacing`, and `margins` on any text object, and the values persist cleanly.
- [x] **Battle Console Reverted:**
  - `ConsoleLogUI` uses TextMeshPro's default font (`LiberationSans SDF`) matching the Unity IDE console.
- [x] **In-Arena Typography Intact:**
  - Player HP, Enemy HP, and Intent Plates default to `BoldPixels_SDF` but respect designer modifications.
- [x] **Right-to-Left Enemy Health Bar:**
  - Decreases from right to left upon taking damage.
- [x] **Golem Grounded:**
  - Golem stays solid during charge turns without Perlin jitter.
