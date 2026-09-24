using System;
using UnityEngine;

namespace CodeForge.Combat
{
    public abstract class CombatEntity : MonoBehaviour
    {
        [Header("Entity Base Stats")]
        [SerializeField] protected float maxHp = 100f;
        public float MaxHp => maxHp;
        public float CurrentHp { get; protected set; }
        public bool IsDead => CurrentHp <= 0f;

        [Header("Shield")]
        public int CurrentShield { get; protected set; } = 0;
        public bool IsShielded => CurrentShield > 0;
        public float HealthPercent => maxHp > 0f ? Mathf.Clamp01(CurrentHp / maxHp) : 0f;

        public event Action<float, float> OnHealthChanged; // current, max
        public event Action<int> OnShieldChanged;           // current shield
        public event Action OnDied;

        protected virtual void Awake()
        {
            CurrentHp = maxHp;
            CurrentShield = 0;
        }

        public virtual void AddShield(int amount)
        {
            if (IsDead) return;
            CurrentShield = Mathf.Max(0, CurrentShield + amount);
            OnShieldChanged?.Invoke(CurrentShield);
        }

        public virtual void TakeDamage(float amount, bool isPiercing = false)
        {
            if (IsDead) return;

            float remainingDamage = amount;

            if (!isPiercing && CurrentShield > 0)
            {
                int shieldAbsorbed = Mathf.Min(CurrentShield, Mathf.RoundToInt(remainingDamage));
                CurrentShield -= shieldAbsorbed;
                remainingDamage -= shieldAbsorbed;
                OnShieldChanged?.Invoke(CurrentShield);
            }

            if (remainingDamage > 0f)
            {
                CurrentHp = Mathf.Max(0f, CurrentHp - remainingDamage);
            }

            OnHealthChanged?.Invoke(CurrentHp, maxHp);

            if (CurrentHp <= 0f)
            {
                OnDied?.Invoke();
                Die();
            }
        }

        protected virtual void Die()
        {
            gameObject.SetActive(false);
        }

        public virtual void ResetToMaxHp()
        {
            CurrentHp = maxHp;
            CurrentShield = 0;
            gameObject.SetActive(true);
            OnHealthChanged?.Invoke(CurrentHp, maxHp);
            OnShieldChanged?.Invoke(CurrentShield);
        }

        public virtual void AdvanceRoomReset()
        {
            // Reactivate entity without restoring HP to max (health persists across rooms)
            CurrentShield = 0;
            gameObject.SetActive(true);
            OnHealthChanged?.Invoke(CurrentHp, maxHp);
            OnShieldChanged?.Invoke(CurrentShield);
        }

        public virtual void SetMaxHp(float hp)
        {
            maxHp = hp;
            CurrentHp = hp;
            OnHealthChanged?.Invoke(CurrentHp, maxHp);
        }
    }
}
