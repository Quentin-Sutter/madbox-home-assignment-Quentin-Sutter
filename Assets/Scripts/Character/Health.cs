using System;
using UnityEngine;

namespace Madbox.Character
{
    /// <summary>
    /// Generic health for any actor. It only owns HP math and emits events for reactions.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Health : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1)] private int maxHealth = 3;

        public int CurrentHealth { get; private set; }
        public bool IsAlive => CurrentHealth > 0;

        // Kept intentionally small: one event for hit feedback and one for death reactions.
        public event Action<int, int> OnDamaged;
        public event Action OnDied;

        private bool _hasDied;

        private void Awake()
        {
            CurrentHealth = maxHealth;
            _hasDied = false;
        }

        public void ApplyDamage(int amount)
        {
            if (!isActiveAndEnabled || amount <= 0 || _hasDied)
            {
                return;
            }

            int previousHealth = CurrentHealth;
            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);

            int appliedAmount = previousHealth - CurrentHealth;
            if (appliedAmount <= 0)
            {
                return;
            }

            OnDamaged?.Invoke(appliedAmount, CurrentHealth);

            if (CurrentHealth == 0)
            {
                _hasDied = true;
                OnDied?.Invoke();
            }
        }

        public void ResetToMax()
        {
            CurrentHealth = maxHealth;
            _hasDied = false;
        }
    }
}
