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
        Buff,       // Empowering stats
        Debuff      // Weakens player
    }

    public enum EnemyArchetype
    {
        Default,
        TrainingSlime,   // 3 DMG flat every turn
        ShieldBeetle,    // Turn 1 [SHIELD 12], Turn 2 [ATK 6], repeat
        GlassCannon,     // 9 DMG flat every turn
        SlimeTank,       // 4 DMG flat every turn
        GolemCharger,    // Turn 1 [CHARGE], Turn 2 [CHARGE], Turn 3 [HEAVY 22], repeat
        MemoryLeak,      // 5 DMG, heals on unshielded hit, every 3 turns permanently +2 DMG
        SyntaxGlitch     // Rotates between [DEBUFF] (reduces player dmgMult by 0.2x) and [ATK 8]
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
            EnemyIntentType.Shield => $"[SHIELD {projectedValue}]",
            EnemyIntentType.Charge => projectedValue > 0 ? $"[CHARGING {projectedValue}]" : "[CHARGING!]",
            EnemyIntentType.Buff => $"[BUFF +{projectedValue}]",
            EnemyIntentType.Debuff => "[DEBUFF]",
            _ => "[IDLE]"
        };
    }

    public class EnemyEntity : CombatEntity
    {
        [Header("Enemy Attributes")]
        public EnemyArchetype archetype = EnemyArchetype.Default;
        public float contactDamage = 10f;
        private CombatEntity playerTarget;

        private Vector3 originalLocalPos;
        private bool isLunging = false;

        [Header("Status Effects")]
        public int currentBleedDamage = 0;
        public int currentBleedDuration = 0;

        public void ApplyBleed(int damagePerTurn, int duration)
        {
            if (IsDead || duration <= 0 || damagePerTurn <= 0) return;
            currentBleedDamage = Mathf.Max(currentBleedDamage, damagePerTurn);
            currentBleedDuration = Mathf.Max(currentBleedDuration, duration);
            ConsoleLogUI.Log($"<color=#E06C75>[Status] {gameObject.name} inflicted with Bleed ({currentBleedDamage} DMG/turn for {currentBleedDuration} turns)!</color>");
        }

        public EnemyIntent CurrentIntent { get; private set; }
        public event Action<EnemyIntent> OnIntentChanged;

        private Transform visualChild;
        private Vector3 visualOriginalPos;

        protected override void Awake()
        {
            base.Awake();
            originalLocalPos = transform.localPosition;
            EnsureVisualChild();
        }

        public void EnsureVisualChild()
        {
            if (visualChild != null) return;

            visualChild = transform.Find("Visual") ?? transform.Find("Sprite") ?? transform.Find("Cube");
            if (visualChild == null)
            {
                var meshFilter = GetComponent<MeshFilter>();
                var meshRenderer = GetComponent<MeshRenderer>();
                if (meshFilter != null && meshRenderer != null)
                {
                    GameObject vObj = new GameObject("Visual", typeof(MeshFilter), typeof(MeshRenderer));
                    vObj.transform.SetParent(transform, false);
                    vObj.transform.localPosition = Vector3.zero;
                    vObj.transform.localRotation = Quaternion.identity;
                    vObj.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);

                    var childMF = vObj.GetComponent<MeshFilter>();
                    childMF.sharedMesh = meshFilter.sharedMesh;
                    var childMR = vObj.GetComponent<MeshRenderer>();
                    childMR.sharedMaterials = meshRenderer.sharedMaterials;

                    Destroy(meshRenderer);
                    Destroy(meshFilter);
                    visualChild = vObj.transform;
                }
            }
            if (visualChild != null)
            {
                visualChild.localScale = new Vector3(1.3f, 1.3f, 1.3f);
                visualOriginalPos = visualChild.localPosition;
            }
        }

        private void Update()
        {
            EnsureVisualChild();
            Transform vTr = visualChild != null ? visualChild : transform;
            Vector3 basePos = visualChild != null ? visualOriginalPos : originalLocalPos;

            if (CurrentIntent.intentType == EnemyIntentType.Charge && !IsDead && !isLunging)
            {
                float shakeX = (Mathf.PerlinNoise(Time.time * 30f, 0f) - 0.5f) * 0.12f;
                float shakeY = (Mathf.PerlinNoise(0f, Time.time * 30f) - 0.5f) * 0.12f;
                vTr.localPosition = basePos + new Vector3(shakeX, shakeY, 0f);
            }
            else if (!isLunging && vTr.localPosition != basePos)
            {
                vTr.localPosition = basePos;
            }
        }

        public void Initialize(CombatEntity target)
        {
            playerTarget = target;
            originalLocalPos = transform.localPosition;
            currentBleedDamage = 0;
            currentBleedDuration = 0;
            gameObject.SetActive(true);
            EnsureIntentPlateUI();
            RollNextIntent(1);
        }

        public override void AdvanceRoomReset()
        {
            currentBleedDamage = 0;
            currentBleedDuration = 0;
            base.AdvanceRoomReset();
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
                    // Odd turns: Shield 12, Even turns: Attack contactDamage (default 6)
                    if (turn % 2 == 1)
                        nextIntent = new EnemyIntent(EnemyIntentType.Shield, 12);
                    else
                        nextIntent = new EnemyIntent(EnemyIntentType.Attack, Mathf.RoundToInt(contactDamage > 0f ? contactDamage : 6f));
                    break;

                case EnemyArchetype.GlassCannon:
                    nextIntent = new EnemyIntent(EnemyIntentType.Attack, Mathf.RoundToInt(contactDamage > 0f ? contactDamage : 9f));
                    break;

                case EnemyArchetype.SlimeTank:
                    nextIntent = new EnemyIntent(EnemyIntentType.Attack, Mathf.RoundToInt(contactDamage > 0f ? contactDamage : 4f));
                    break;

                case EnemyArchetype.GolemCharger:
                    // Turns 1 & 2: Charge, Turn 3: Heavy contactDamage (default 18)
                    int cycle = ((turn - 1) % 3) + 1;
                    if (cycle == 3)
                        nextIntent = new EnemyIntent(EnemyIntentType.HeavyHit, Mathf.RoundToInt(contactDamage > 0f ? contactDamage : 18f));
                    else
                        nextIntent = new EnemyIntent(EnemyIntentType.Charge, 0);
                    break;

                case EnemyArchetype.MemoryLeak:
                    // Turn % 3 == 0: Buff +2 DMG permanently. Otherwise Attack with contactDamage (starts at 5).
                    if (turn > 1 && turn % 3 == 0)
                    {
                        nextIntent = new EnemyIntent(EnemyIntentType.Buff, 2);
                    }
                    else
                    {
                        nextIntent = new EnemyIntent(EnemyIntentType.Attack, Mathf.RoundToInt(contactDamage > 0f ? contactDamage : 5f));
                    }
                    break;

                case EnemyArchetype.SyntaxGlitch:
                    // Odd turns: [DEBUFF] (reduces player damage multiplier by 0.2x). Even turns: [ATK contactDamage] (default 8).
                    if (turn % 2 == 1)
                    {
                        nextIntent = new EnemyIntent(EnemyIntentType.Debuff, 1);
                    }
                    else
                    {
                        nextIntent = new EnemyIntent(EnemyIntentType.Attack, Mathf.RoundToInt(contactDamage > 0f ? contactDamage : 8f));
                    }
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

            // Tick Bleed at start of enemy turn
            if (currentBleedDuration > 0)
            {
                TakeDamage(currentBleedDamage);
                currentBleedDuration--;
                ConsoleLogUI.Log($"<color=#E06C75>[Status] {gameObject.name} suffers {currentBleedDamage} Bleed damage! ({currentBleedDuration} turns remain)</color>");
                yield return new WaitForSeconds(0.25f);
                if (IsDead) yield break;
            }

            switch (CurrentIntent.intentType)
            {
                case EnemyIntentType.Attack:
                    if (target != null && !target.IsDead)
                    {
                        float hpBefore = target.CurrentHp;
                        yield return StartCoroutine(PerformAttackLunge(new Vector3(-0.4f, -0.2f, 0f), 0.10f, () =>
                        {
                            target.TakeDamage(CurrentIntent.projectedValue);
                            ConsoleLogUI.Log($"[Enemy] {gameObject.name} attacks Player for {CurrentIntent.projectedValue} DMG!");
                        }));

                        float hpLost = Mathf.Max(0f, hpBefore - target.CurrentHp);

                        if (archetype == EnemyArchetype.MemoryLeak && hpLost > 0f)
                        {
                            Heal(hpLost);
                            ConsoleLogUI.Log($"<color=#98C379>[Enemy] MemoryLeak absorbed {hpLost:0} HP from Player! Healed +{hpLost:0} HP.</color>");
                        }

                        if (target is PlayerCombatController playerCombat && !playerCombat.IsDead)
                        {
                            yield return StartCoroutine(playerCombat.HandleIncomingDamageReaction(Mathf.RoundToInt(hpLost)));
                        }
                    }
                    break;

                case EnemyIntentType.HeavyHit:
                    if (target != null && !target.IsDead)
                    {
                        float hpBefore = target.CurrentHp;
                        yield return StartCoroutine(PerformAttackLunge(new Vector3(-0.6f, -0.3f, 0f), 0.14f, () =>
                        {
                            target.TakeDamage(CurrentIntent.projectedValue);
                            ConsoleLogUI.Log($"[Enemy] {gameObject.name} lands HEAVY STRIKE on Player for {CurrentIntent.projectedValue} DMG!");
                        }));

                        float hpLost = Mathf.Max(0f, hpBefore - target.CurrentHp);

                        if (target is PlayerCombatController playerCombat && !playerCombat.IsDead)
                        {
                            yield return StartCoroutine(playerCombat.HandleIncomingDamageReaction(Mathf.RoundToInt(hpLost)));
                        }
                    }
                    break;

                case EnemyIntentType.Charge:
                    ConsoleLogUI.Log($"[Enemy] {gameObject.name} is gathering energy! [CHARGING!]");
                    yield return new WaitForSeconds(0.4f);
                    break;

                case EnemyIntentType.Shield:
                    AddShield(CurrentIntent.projectedValue);
                    ConsoleLogUI.Log($"[Enemy] {gameObject.name} reinforces with {CurrentIntent.projectedValue} Shield!");
                    yield return new WaitForSeconds(0.2f);
                    break;

                case EnemyIntentType.Buff:
                    contactDamage += CurrentIntent.projectedValue;
                    ConsoleLogUI.Log($"<color=#FFA94D>[Enemy] {gameObject.name} scales permanently! Contact damage increased by +{CurrentIntent.projectedValue} (Now: {contactDamage:0} DMG).</color>");
                    yield return new WaitForSeconds(0.2f);
                    break;

                case EnemyIntentType.Debuff:
                    if (target is PlayerCombatController player)
                    {
                        player.DamageMultiplier = Mathf.Max(0.2f, player.DamageMultiplier - 0.2f);
                        ConsoleLogUI.Log($"<color=#C678DD>[Enemy] {gameObject.name} injected a Syntax Glitch! Player Damage Multiplier reduced by 0.2x (Now: {player.DamageMultiplier:0.0#}x)!</color>");
                    }
                    yield return new WaitForSeconds(0.25f);
                    break;
            }

            yield return new WaitForSeconds(0.2f);
        }

        private IEnumerator PerformAttackLunge(Vector3 offset, float duration = 0.10f, Action onApex = null)
        {
            isLunging = true;
            EnsureVisualChild();
            Transform targetTr = visualChild != null ? visualChild : transform;
            Vector3 startPos = visualChild != null ? visualOriginalPos : originalLocalPos;
            Vector3 targetPos = startPos + offset;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                targetTr.localPosition = Vector3.Lerp(startPos, targetPos, elapsed / duration);
                yield return null;
            }
            targetTr.localPosition = targetPos;

            onApex?.Invoke();

            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                targetTr.localPosition = Vector3.Lerp(targetPos, startPos, elapsed / duration);
                yield return null;
            }

            targetTr.localPosition = startPos;
            isLunging = false;
        }
    }
}
