using System;
using System.Collections;
using UnityEngine;
using CodeForge.UI;

namespace CodeForge.Combat
{
    public enum EnemyIntentType
    {
        Attack,     // Standard damage
        HeavyHit,   // Charged devastating attack
        Shield,     // Raises armor
        Charge,     // Telegraphing heavy hit (0 DMG this turn)
        Buff        // Empowering stats
    }

    public enum EnemyArchetype
    {
        Default,
        TrainingSlime,   // 3 DMG flat every turn
        ShieldBeetle,    // Turn 1 [SHIELD 12], Turn 2 [ATK 6], repeat
        GlassCannon,     // 9 DMG flat every turn
        SlimeTank,       // 4 DMG flat every turn
        GolemCharger     // Turn 1 [CHARGE], Turn 2 [CHARGE], Turn 3 [HEAVY 22], repeat
    }

    [System.Serializable]
    public struct EnemyIntent
    {
        public EnemyIntentType intentType;
        public int projectedValue; // e.g., 14 DMG or 8 Shield

        public EnemyIntent(EnemyIntentType type, int value)
        {
            intentType = type;
            projectedValue = value;
        }

        public string GetBadgeText() => intentType switch
        {
            EnemyIntentType.Attack => $"[ATK {projectedValue}]",
            EnemyIntentType.HeavyHit => $"[HEAVY {projectedValue}]",
            EnemyIntentType.Shield => $"[DEF {projectedValue}]",
            EnemyIntentType.Charge => "[CHARGING!]",
            EnemyIntentType.Buff => $"[BUFF {projectedValue}]",
            _ => "[IDLE]"
        };
    }

    public class EnemyEntity : CombatEntity
    {
        [Header("Enemy Attributes")]
        public EnemyArchetype archetype = EnemyArchetype.Default;
        public float contactDamage = 10f;
        private CombatEntity playerTarget;

        public EnemyIntent CurrentIntent { get; private set; }
        public event Action<EnemyIntent> OnIntentChanged;

        public void Initialize(CombatEntity target)
        {
            playerTarget = target;
            gameObject.SetActive(true);
            EnsureIntentPlateUI();
            RollNextIntent(1);
        }

        private void EnsureIntentPlateUI()
        {
            var plate = GetComponentInChildren<EnemyIntentPlateUI>(true);
            if (plate == null)
            {
                var canvas = GetComponentInChildren<Canvas>(true);
                if (canvas != null)
                {
                    var plateObj = new GameObject("EnemyIntentPlate", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image), typeof(EnemyIntentPlateUI));
                    plateObj.transform.SetParent(canvas.transform, false);

                    var rect = plateObj.GetComponent<RectTransform>();
                    rect.anchorMin = new Vector2(0.5f, 1f);
                    rect.anchorMax = new Vector2(0.5f, 1f);
                    rect.pivot = new Vector2(0.5f, 0f);
                    rect.anchoredPosition = new Vector2(0f, 6f);
                    rect.sizeDelta = new Vector2(130f, 24f);

                    var bgImg = plateObj.GetComponent<UnityEngine.UI.Image>();
                    bgImg.color = new Color(0.12f, 0.12f, 0.16f, 0.85f);

                    var textObj = new GameObject("IntentText", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
                    textObj.transform.SetParent(plateObj.transform, false);

                    var textRect = textObj.GetComponent<RectTransform>();
                    textRect.anchorMin = Vector2.zero;
                    textRect.anchorMax = Vector2.one;
                    textRect.sizeDelta = Vector2.zero;

                    var tmp = textObj.GetComponent<TMPro.TextMeshProUGUI>();
                    tmp.alignment = TMPro.TextAlignmentOptions.Center;
                    tmp.fontSize = 12f;
                    tmp.color = Color.white;
                    tmp.textWrappingMode = TMPro.TextWrappingModes.NoWrap;

                    plate = plateObj.GetComponent<EnemyIntentPlateUI>();
                    plate.InitializeAtRuntime(this, tmp, bgImg);
                }
            }
            else
            {
                plate.BindEnemy(this);
            }
        }

        public void RollNextIntent(int turn)
        {
            EnemyIntent nextIntent;

            switch (archetype)
            {
                case EnemyArchetype.TrainingSlime:
                    nextIntent = new EnemyIntent(EnemyIntentType.Attack, Mathf.RoundToInt(contactDamage > 0f ? contactDamage : 3f));
                    break;

                case EnemyArchetype.ShieldBeetle:
                    // Odd turns: Shield 12, Even turns: Attack 6
                    if (turn % 2 == 1)
                        nextIntent = new EnemyIntent(EnemyIntentType.Shield, 12);
                    else
                        nextIntent = new EnemyIntent(EnemyIntentType.Attack, 6);
                    break;

                case EnemyArchetype.GlassCannon:
                    nextIntent = new EnemyIntent(EnemyIntentType.Attack, Mathf.RoundToInt(contactDamage > 0f ? contactDamage : 9f));
                    break;

                case EnemyArchetype.SlimeTank:
                    nextIntent = new EnemyIntent(EnemyIntentType.Attack, Mathf.RoundToInt(contactDamage > 0f ? contactDamage : 4f));
                    break;

                case EnemyArchetype.GolemCharger:
                    // Turns 1 & 2: Charge, Turn 3: Heavy 22
                    int cycle = ((turn - 1) % 3) + 1;
                    if (cycle == 3)
                        nextIntent = new EnemyIntent(EnemyIntentType.HeavyHit, 22);
                    else
                        nextIntent = new EnemyIntent(EnemyIntentType.Charge, 0);
                    break;

                default:
                    if (turn > 1 && turn % 3 == 0)
                    {
                        int heavyDmg = Mathf.RoundToInt(contactDamage * 1.8f);
                        nextIntent = new EnemyIntent(EnemyIntentType.HeavyHit, heavyDmg);
                    }
                    else if (turn > 1 && turn % 2 == 0 && HealthPercent < 0.7f && CurrentShield == 0)
                    {
                        nextIntent = new EnemyIntent(EnemyIntentType.Shield, 8);
                    }
                    else
                    {
                        nextIntent = new EnemyIntent(EnemyIntentType.Attack, Mathf.RoundToInt(contactDamage));
                    }
                    break;
            }

            CurrentIntent = nextIntent;
            OnIntentChanged?.Invoke(CurrentIntent);
        }

        public IEnumerator ExecuteEnemyTurn(CombatEntity target)
        {
            if (IsDead || target == null || target.IsDead) yield break;

            switch (CurrentIntent.intentType)
            {
                case EnemyIntentType.Attack:
                    yield return StartCoroutine(PerformAttackLunge(new Vector3(-0.4f, -0.2f, 0f), 0.12f));
                    if (target != null && !target.IsDead)
                    {
                        if (target is PlayerCombatController playerCombat)
                        {
                            yield return StartCoroutine(playerCombat.HandleIncomingDamageReaction(CurrentIntent.projectedValue));
                        }
                        target.TakeDamage(CurrentIntent.projectedValue);
                        ConsoleLogUI.Log($"[Enemy] {gameObject.name} attacks Player for {CurrentIntent.projectedValue} DMG!");
                    }
                    break;

                case EnemyIntentType.HeavyHit:
                    yield return StartCoroutine(PerformAttackLunge(new Vector3(-0.6f, -0.3f, 0f), 0.18f));
                    if (target != null && !target.IsDead)
                    {
                        if (target is PlayerCombatController playerCombat)
                        {
                            yield return StartCoroutine(playerCombat.HandleIncomingDamageReaction(CurrentIntent.projectedValue));
                        }
                        target.TakeDamage(CurrentIntent.projectedValue);
                        ConsoleLogUI.Log($"[Enemy] {gameObject.name} lands HEAVY STRIKE on Player for {CurrentIntent.projectedValue} DMG!");
                    }
                    break;

                case EnemyIntentType.Charge:
                    ConsoleLogUI.Log($"[Enemy] {gameObject.name} is gathering energy! [CHARGING!]");
                    yield return StartCoroutine(PerformAttackLunge(new Vector3(-0.15f, 0.1f, 0f), 0.2f));
                    yield return new WaitForSeconds(0.2f);
                    break;

                case EnemyIntentType.Shield:
                    AddShield(CurrentIntent.projectedValue);
                    ConsoleLogUI.Log($"[Enemy] {gameObject.name} reinforces with {CurrentIntent.projectedValue} Shield!");
                    yield return new WaitForSeconds(0.2f);
                    break;

                case EnemyIntentType.Buff:
                    contactDamage += CurrentIntent.projectedValue;
                    ConsoleLogUI.Log($"[Enemy] {gameObject.name} powers up! Contact damage increased by {CurrentIntent.projectedValue}.");
                    yield return new WaitForSeconds(0.2f);
                    break;
            }

            yield return new WaitForSeconds(0.2f);
        }

        private IEnumerator PerformAttackLunge(Vector3 offset, float duration = 0.12f)
        {
            Vector3 startPos = transform.position;
            Vector3 targetPos = startPos + offset;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(targetPos, startPos, elapsed / duration);
                yield return null;
            }

            transform.position = startPos;
        }
    }
}
