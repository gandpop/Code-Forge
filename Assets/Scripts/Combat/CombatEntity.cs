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

        public event Action<float, float> OnHealthChanged; // current, max
        public event Action OnDied;

        protected virtual void Awake()
        {
            CurrentHp = maxHp;
        }

        public virtual void TakeDamage(float amount)
        {
            if (IsDead) return;
            CurrentHp = Mathf.Max(0f, CurrentHp - amount);
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
            gameObject.SetActive(true);
            OnHealthChanged?.Invoke(CurrentHp, maxHp);
        }

        public virtual void AdvanceRoomReset()
        {
            // Reactivate entity without restoring HP to max (health persists across rooms)
            gameObject.SetActive(true);
            OnHealthChanged?.Invoke(CurrentHp, maxHp);
        }

        public virtual void SetMaxHp(float hp)
        {
            maxHp = hp;
            CurrentHp = hp;
            OnHealthChanged?.Invoke(CurrentHp, maxHp);
        }
    }
}
