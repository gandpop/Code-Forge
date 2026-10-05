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
        TrainingSlime = 0,
        Skeleton = 1,
        SlimeTank = 2,
        StoneGolem = 3,
        DemonicEye = 4,
        Vampire = 5,
        [System.Obsolete("Use StoneGolem instead")] GolemCharger = 10,
        [System.Obsolete("Use DemonicEye instead")] Default = 11,
        [System.Obsolete("Use DemonicEye instead")] GlitchBug = 12,
        [System.Obsolete("Use Vampire instead")] MemoryLeak = 13,
        [System.Obsolete("Use Skeleton instead")] ShieldBeetle = 14,
        [System.Obsolete("Deprecated")] GlassCannon = 15,
        [System.Obsolete("Deprecated")] SyntaxGlitch = 16
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
        public EnemyArchetype archetype = EnemyArchetype.DemonicEye;
        public int slotIndex = 1;
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

        [Header("Sprite Visuals")]
        [SerializeField] private RuntimeAnimatorController slimeController;
        [SerializeField] private RuntimeAnimatorController skeletonController;
        [SerializeField] private RuntimeAnimatorController stoneGolemController;
        [SerializeField] private RuntimeAnimatorController demonicEyeController;
        [SerializeField] private RuntimeAnimatorController vampireController;

        [SerializeField] private Sprite slimeSprite;
        [SerializeField] private Sprite skeletonSprite;
        [SerializeField] private Sprite stoneGolemSprite;
        [SerializeField] private Sprite demonicEyeSprite;
        [SerializeField] private Sprite vampireSprite;
        [SerializeField] private Material spriteMaterial;

        [Header("Visual Switcher")]
        [SerializeField] private GameObject meshVisual;
        [SerializeField] private GameObject spriteVisual;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Animator spriteAnimator;
        [SerializeField] private Canvas healthBarCanvas;

        private Transform visualChild;
        private Vector3 visualOriginalPos;

        protected override void Awake()
        {
            base.Awake();
            originalLocalPos = transform.localPosition;
            ApplyArchetype();
        }

        public void EnsureVisualChild()
        {
            if (visualChild != null) return;
            if (spriteVisual != null && spriteVisual.activeSelf) visualChild = spriteVisual.transform;
            else if (meshVisual != null && meshVisual.activeSelf) visualChild = meshVisual.transform;
            else visualChild = transform.Find("SpriteVisual") ?? transform.Find("Visual") ?? transform;
            visualOriginalPos = visualChild.localPosition;
        }

        public void ApplyArchetype()
        {
            if (meshVisual == null)
            {
                var mv = transform.Find("Visual") ?? transform.Find("MeshVisual");
                if (mv != null) meshVisual = mv.gameObject;
            }
            if (spriteVisual == null)
            {
                var sv = transform.Find("SpriteVisual");
                if (sv != null)
                {
                    spriteVisual = sv.gameObject;
                }
                else
                {
                    var newObj = new GameObject("SpriteVisual", typeof(SpriteRenderer), typeof(Animator));
                    newObj.transform.SetParent(transform, false);
                    spriteVisual = newObj;
                }
            }

            if (spriteRenderer == null && spriteVisual != null) spriteRenderer = spriteVisual.GetComponent<SpriteRenderer>();
            if (spriteAnimator == null && spriteVisual != null) spriteAnimator = spriteVisual.GetComponent<Animator>();
            if (healthBarCanvas == null) healthBarCanvas = GetComponentInChildren<Canvas>(true);

#if UNITY_EDITOR
            if (slimeController == null)
                slimeController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Enemies/SlimeAnimatorController.controller");
            if (skeletonController == null)
                skeletonController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Enemies/SkeletonAnimatorController.controller");
            if (stoneGolemController == null)
                stoneGolemController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Enemies/StoneGolemAnimatorController.controller");
            if (demonicEyeController == null)
                demonicEyeController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Enemies/DemonicEyeAnimatorController.controller");
            if (vampireController == null)
                vampireController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Enemies/VampireAnimatorController.controller");

            if (slimeSprite == null)
                slimeSprite = LoadFirstSprite("Assets/Sprites/Enemies/SlimeIdle.png");
            if (skeletonSprite == null)
                skeletonSprite = LoadFirstSprite("Assets/Sprites/Enemies/SkeletonIdle.png");
            if (stoneGolemSprite == null)
                stoneGolemSprite = LoadFirstSprite("Assets/Sprites/Enemies/StoneGolem.png");
            if (demonicEyeSprite == null)
                demonicEyeSprite = LoadFirstSprite("Assets/Sprites/Enemies/DemonicEye.png");
            if (vampireSprite == null)
                vampireSprite = LoadFirstSprite("Assets/Sprites/Enemies/Vampire.png");

            if (spriteMaterial == null)
                spriteMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sprite_Unlit.mat");
#endif

            Sprite chosenSprite = null;
            RuntimeAnimatorController chosenController = null;
            Vector3 chosenScale = Vector3.one;
            float localY = 0.50f;
            float intentBadgeY = 1.5f;

            switch (archetype)
            {
                case EnemyArchetype.StoneGolem:
                case EnemyArchetype.GolemCharger:
                    chosenSprite = stoneGolemSprite;
                    chosenController = stoneGolemController;
                    chosenScale = new Vector3(3.6f, 3.6f, 1f);
                    localY = 0.40f;
                    intentBadgeY = 2.45f;
                    break;

                case EnemyArchetype.DemonicEye:
                case EnemyArchetype.Default:
                case EnemyArchetype.GlitchBug:
                case EnemyArchetype.GlassCannon:
                case EnemyArchetype.SyntaxGlitch:
                    chosenSprite = demonicEyeSprite;
                    chosenController = demonicEyeController;
                    chosenScale = new Vector3(2.2f, 2.2f, 1f);
                    localY = 0.70f;
                    intentBadgeY = 2.05f;
                    break;

                case EnemyArchetype.Vampire:
                case EnemyArchetype.MemoryLeak:
                    chosenSprite = vampireSprite;
                    chosenController = vampireController;
                    chosenScale = new Vector3(2.6f, 2.6f, 1f);
                    localY = 0.50f;
                    intentBadgeY = 2.05f;
                    break;

                case EnemyArchetype.SlimeTank:
                    chosenSprite = slimeSprite;
                    chosenController = slimeController;
                    chosenScale = new Vector3(3.0f, 3.0f, 1f);
                    localY = 0.40f;
                    intentBadgeY = 2.10f;
                    break;

                case EnemyArchetype.Skeleton:
                case EnemyArchetype.ShieldBeetle:
                    chosenSprite = skeletonSprite;
                    chosenController = skeletonController;
                    chosenScale = new Vector3(2.6f, 2.6f, 1f);
                    localY = 0.50f;
                    intentBadgeY = 2.05f;
                    break;

                case EnemyArchetype.TrainingSlime:
                default:
                    chosenSprite = slimeSprite;
                    chosenController = slimeController;
                    chosenScale = new Vector3(2.2f, 2.2f, 1f);
                    localY = 0.40f;
                    intentBadgeY = 1.70f;
                    break;
            }

            if (meshVisual != null) meshVisual.SetActive(false);
            if (spriteVisual != null)
            {
                spriteVisual.SetActive(true);
                spriteVisual.transform.localScale = chosenScale;
                spriteVisual.transform.localPosition = new Vector3(0f, localY, 0f);
            }

            if (spriteRenderer != null)
            {
                if (chosenSprite != null) spriteRenderer.sprite = chosenSprite;
                if (spriteMaterial != null) spriteRenderer.sharedMaterial = spriteMaterial;
                spriteRenderer.sortingOrder = 5;
            }

            if (spriteAnimator != null)
            {
                if (chosenController != null)
                {
                    spriteAnimator.runtimeAnimatorController = chosenController;
                    spriteAnimator.enabled = true;
                }
                else
                {
                    spriteAnimator.enabled = false;
                }
            }

            if (healthBarCanvas != null)
            {
                healthBarCanvas.overrideSorting = true;
                healthBarCanvas.sortingOrder = 20;
                healthBarCanvas.transform.localPosition = new Vector3(0f, intentBadgeY, 0f);
            }

            visualChild = spriteVisual != null ? spriteVisual.transform : transform;
            visualOriginalPos = visualChild.localPosition;
        }

        public void SetupVisualForArchetype()
        {
            ApplyArchetype();
        }

#if UNITY_EDITOR
        private static Sprite LoadFirstSprite(string path)
        {
            var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var a in assets)
            {
                if (a is Sprite s) return s;
            }
            return null;
        }
#endif

        private void Update()
        {
            EnsureVisualChild();
            Transform vTr = visualChild != null ? visualChild : transform;
            Vector3 basePos = visualChild != null ? visualOriginalPos : originalLocalPos;

            if (!isLunging && vTr.localPosition != basePos)
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
            SetupVisualForArchetype();
            EnsureIntentPlateUI();
            var healthBar = GetComponentInChildren<EnemyHealthBarUI>(true);
            if (healthBar != null) healthBar.BindEntity(this);
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
            var canvas = GetComponentInChildren<Canvas>(true);
            if (canvas != null)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = 20;
            }

            var plate = GetComponentInChildren<EnemyIntentPlateUI>(true);
            if (plate == null)
            {
                if (canvas != null)
                {
                    Transform existingPlateTr = canvas.transform.Find("EnemyIntentPlate");
                    GameObject plateObj = existingPlateTr != null ? existingPlateTr.gameObject : null;
                    if (plateObj == null)
                    {
                        plateObj = new GameObject("EnemyIntentPlate", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image), typeof(EnemyIntentPlateUI));
                        plateObj.transform.SetParent(canvas.transform, false);

                        var rect = plateObj.GetComponent<RectTransform>();
                        rect.anchorMin = new Vector2(0.5f, 1f);
                        rect.anchorMax = new Vector2(0.5f, 1f);
                        rect.pivot = new Vector2(0.5f, 0f);
                        rect.anchoredPosition = new Vector2(0f, 6f);
                        rect.sizeDelta = new Vector2(145f, 28f);

                        var bgImg = plateObj.GetComponent<UnityEngine.UI.Image>();
                        bgImg.color = new Color(0.12f, 0.12f, 0.16f, 0.85f);
                    }

                    plate = plateObj.GetComponent<EnemyIntentPlateUI>();
                    if (plate == null) plate = plateObj.AddComponent<EnemyIntentPlateUI>();

                    Transform existingTextTr = plateObj.transform.Find("IntentText");
                    TMPro.TextMeshProUGUI tmp = existingTextTr != null ? existingTextTr.GetComponent<TMPro.TextMeshProUGUI>() : null;
                    if (tmp == null)
                    {
                        var textObj = new GameObject("IntentText", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
                        textObj.transform.SetParent(plateObj.transform, false);

                        var textRect = textObj.GetComponent<RectTransform>();
                        textRect.anchorMin = Vector2.zero;
                        textRect.anchorMax = Vector2.one;
                        textRect.sizeDelta = Vector2.zero;

                        tmp = textObj.GetComponent<TMPro.TextMeshProUGUI>();
                        tmp.alignment = TMPro.TextAlignmentOptions.Center;
                        tmp.fontSize = 16f;
                        tmp.color = Color.white;
                        tmp.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
#if UNITY_EDITOR
                        var font = UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/Fonts/BoldPixels_SDF.asset");
                        if (font != null) tmp.font = font;
#endif
                    }

                    var bg = plateObj.GetComponent<UnityEngine.UI.Image>();
                    plate.InitializeAtRuntime(this, tmp, bg);
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

                case EnemyArchetype.Skeleton:
                case EnemyArchetype.ShieldBeetle:
                    // Odd turns: Shield 12, Even turns: Attack contactDamage (default 6)
                    if (turn % 2 == 1)
                        nextIntent = new EnemyIntent(EnemyIntentType.Shield, 12);
                    else
                        nextIntent = new EnemyIntent(EnemyIntentType.Attack, Mathf.RoundToInt(contactDamage > 0f ? contactDamage : 6f));
                    break;

                case EnemyArchetype.SlimeTank:
                    nextIntent = new EnemyIntent(EnemyIntentType.Attack, Mathf.RoundToInt(contactDamage > 0f ? contactDamage : 4f));
                    break;

                case EnemyArchetype.StoneGolem:
                case EnemyArchetype.GolemCharger:
                    // Turns 1 & 2: Charge, Turn 3: Heavy contactDamage (default 18)
                    int cycle = ((turn - 1) % 3) + 1;
                    if (cycle == 3)
                        nextIntent = new EnemyIntent(EnemyIntentType.HeavyHit, Mathf.RoundToInt(contactDamage > 0f ? contactDamage : 18f));
                    else
                        nextIntent = new EnemyIntent(EnemyIntentType.Charge, 0);
                    break;

                case EnemyArchetype.DemonicEye:
                case EnemyArchetype.Default:
                case EnemyArchetype.GlitchBug:
                case EnemyArchetype.GlassCannon:
                case EnemyArchetype.SyntaxGlitch:
                    // Odd turns: [DEBUFF] (reduces player damage multiplier by 0.2x). Even turns: [ATK contactDamage] (default 6).
                    if (turn % 2 == 1)
                    {
                        nextIntent = new EnemyIntent(EnemyIntentType.Debuff, 1);
                    }
                    else
                    {
                        nextIntent = new EnemyIntent(EnemyIntentType.Attack, Mathf.RoundToInt(contactDamage > 0f ? contactDamage : 6f));
                    }
                    break;

                case EnemyArchetype.Vampire:
                case EnemyArchetype.MemoryLeak:
                    // Turn > 1 && Turn % 3 == 0: Buff +2 DMG permanently. Otherwise Attack with contactDamage (starts at 5-6).
                    if (turn > 1 && turn % 3 == 0)
                    {
                        nextIntent = new EnemyIntent(EnemyIntentType.Buff, 2);
                    }
                    else
                    {
                        nextIntent = new EnemyIntent(EnemyIntentType.Attack, Mathf.RoundToInt(contactDamage > 0f ? contactDamage : 5f));
                    }
                    break;

                default:
                    nextIntent = new EnemyIntent(EnemyIntentType.Attack, Mathf.RoundToInt(contactDamage > 0f ? contactDamage : 5f));
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
                        yield return StartCoroutine(PerformAttackLunge(new Vector3(-0.5f, -0.3f, 0f), 0.10f, () =>
                        {
                            target.TakeDamage(CurrentIntent.projectedValue);
                            ConsoleLogUI.Log($"[Enemy] {gameObject.name} attacks Player for {CurrentIntent.projectedValue} DMG!");
                        }));

                        float hpLost = Mathf.Max(0f, hpBefore - target.CurrentHp);

                        if ((archetype == EnemyArchetype.Vampire || archetype == EnemyArchetype.MemoryLeak) && hpLost > 0f)
                        {
                            Heal(hpLost);
                            ConsoleLogUI.Log($"<color=#98C379>[Enemy] Vampire absorbed {hpLost:0} HP from Player! Healed +{hpLost:0} HP.</color>");
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
                        yield return StartCoroutine(PerformAttackLunge(new Vector3(-0.5f, -0.3f, 0f), 0.14f, () =>
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
                        ConsoleLogUI.Log($"<color=#C678DD>[Enemy] {gameObject.name} inflicted an Evil Glare! Player Damage Multiplier reduced by 0.2x (Now: {player.DamageMultiplier:0.0#}x)!</color>");
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
