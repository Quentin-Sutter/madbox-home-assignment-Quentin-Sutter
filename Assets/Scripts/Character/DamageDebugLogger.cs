using UnityEngine;

namespace Madbox.Character
{
    /// <summary>
    /// Optional observer used to validate damage events while tuning combat.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DamageDebugLogger : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private bool enableLogs = true;

        private void Awake()
        {
            if (health == null)
            {
                health = GetComponent<Health>();
            }

            if (health == null)
            {
                health = GetComponentInParent<Health>();
            }
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.OnDamaged += HandleDamaged;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.OnDamaged -= HandleDamaged;
            }
        }

        private void HandleDamaged(int amountApplied, int newHealth)
        {
            if (!enableLogs)
            {
                return;
            }

            Debug.Log($"{name} took {amountApplied} damage. Remaining health: {newHealth}.", this);
        }
    }
}
