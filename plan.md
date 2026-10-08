# ARCHITECTURAL PLAN & SPECIFICATION (v36.0)
## Project: CodeForge — Robust 20-Second Global Combat Watchdog Timer (Anti-Softlock)

---

## 1. Executive Summary & Root Cause Analysis

### 1.1 The Issue
The user noticed during pre-build testing that the anti-softlock / infinite loop detection was not triggering when expected.

### 1.2 Root Cause Analysis
In the previous implementation (`CombatManager.cs`):
```csharp
private IEnumerator ExecutePlayerTurnSafeguarded(...)
{
    float startTime = Time.time;
    const float timeoutSeconds = 3.0f;
    ...
```
The timeout was placed **only** inside the single-round player turn pipeline. 
1. **Per-Round Reset:** The 3-second timer reset on every single round. If a player was trapped in an infinite combat loop or stalemate (e.g., repeating shield/heal while the enemy deals zero net damage over 50 rounds), each individual player turn took only ~0.4s. The 3.0-second timer was never breached, allowing combat to run endlessly without triggering the safeguard.
2. **Hangs Outside the Pipeline:** If combat froze due to an animation wait, an enemy action deadlock, or an unhandled coroutine hang, `ExecutePlayerTurnSafeguarded` was not running, so the timeout could not fire.

### 1.3 The Solution: 20-Second Dedicated Watchdog Coroutine
Implement a **dedicated, independent watchdog timer coroutine** on `CombatManager` that tracks overall combat execution time:
- When the player clicks "Compile & Battle" and enters `GamePhase.Running`, start a separate `CombatWatchdogTimer(20.0f)`.
- If combat does not reach Victory or Defeat within **20 seconds**, the watchdog timer immediately intercepts execution.
- It safely halts `combatCoroutine`, logs an error to the Battle Console, and opens `DefeatModalUI.ShowRuntimeError(...)` displaying the soft-lock prompt with a `[Retry Room]` button.
- If combat finishes naturally (Victory or Defeat) before 20 seconds, the watchdog coroutine is cancelled cleanly.

---

## 2. Technical Specifications & File Edits

### 2.1 `Assets/Scripts/Combat/CombatManager.cs`

1. **Serialized Watchdog Settings & State:**
   ```csharp
   [Header("Anti-Softlock Safeguard")]
   [SerializeField] private float combatTimeoutDuration = 20.0f;
   private Coroutine watchdogCoroutine;
   ```

2. **Watchdog Lifecycle in `StartCombatExecution()` / `EnterPlanningPhase()` / `HandleVictory()` / `HandleDefeat()`:**
   - In `StartCombatExecution()`:
     ```csharp
     StopWatchdog();
     watchdogCoroutine = StartCoroutine(CombatWatchdogRoutine(combatTimeoutDuration));
     ```
   - In `EnterPlanningPhase()`, `HandleVictory()`, and `HandleDefeat()`:
     ```csharp
     StopWatchdog();
     ```
   - Helper method:
     ```csharp
     private void StopWatchdog()
     {
         if (watchdogCoroutine != null)
         {
             StopCoroutine(watchdogCoroutine);
             watchdogCoroutine = null;
         }
     }
     ```

3. **Independent Watchdog Implementation:**
   ```csharp
   private IEnumerator CombatWatchdogRoutine(float timeoutSeconds)
   {
       yield return new WaitForSeconds(timeoutSeconds);

       if (currentPhase == GamePhase.Running)
       {
           ConsoleLogUI.Log($"<color=#FF4444>[Error] Runtime Exception: Combat execution exceeded {timeoutSeconds:0}s timeout (Soft-lock detected).</color>");
           TriggerRuntimeError($"Combat execution exceeded {timeoutSeconds:0} seconds without resolving.\nExecution halted to prevent soft-lock. Click Retry Room to refactor your code.");
       }
   }
   ```

4. **In `TriggerRuntimeError(string errorMessage)`:**
   - Ensure `StopWatchdog();` is called.
   - Halt `combatCoroutine`.
   - Set `currentPhase = GamePhase.Defeat;`.
   - Display `DefeatModalUI.ShowRuntimeError(errorMessage);`.

---

## 3. Verification & Acceptance Checklist

- [ ] **Independent Timer:** Watchdog runs as a separate coroutine on `CombatManager`, immune to internal pipeline hangs.
- [ ] **20-Second Trigger:** If combat continues for 20 seconds without a winner, execution halts automatically and the prompt appears.
- [ ] **Prompt Display:** `DefeatModalUI` opens showing:
  - Header: `[!] RUNTIME ERROR: EXECUTION TIMEOUT`
  - Body: `Combat execution exceeded 20 seconds without resolving...`
  - Button: `Retry Room`
- [ ] **Retry Functionality:** Clicking `Retry Room` restores player HP to full, resets enemies in current room, and unlocks the code editor for refactoring.
- [ ] **Clean Normal Combat:** Fast battles that finish under 20 seconds cancel the watchdog cleanly without triggering errors.
