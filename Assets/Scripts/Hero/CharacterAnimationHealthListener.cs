using UnityEngine;

namespace Madbox.Hero
{
    /// <summary>
    /// For non-hero actors: forwards Health events to CharacterAnimationDriver.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterAnimationHealthListener : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private CharacterAnimationDriver animationDriver;

        private void Awake()
        {
            if (health == null)
            {
                health = GetComponent<Health>();
            }

            if (animationDriver == null)
            {
                animationDriver = GetComponent<CharacterAnimationDriver>() ?? GetComponentInChildren<CharacterAnimationDriver>();
            }
        }

        private void OnEnable()
        {
            if (health == null)
            {
                return;
            }

            health.OnDamaged += HandleDamaged;
            health.OnDied += HandleDied;
        }

        private void OnDisable()
        {
            if (health == null)
            {
                return;
            }

            health.OnDamaged -= HandleDamaged;
            health.OnDied -= HandleDied;
        }

        private void HandleDamaged(int appliedDamage, int remainingHealth)
        {
            _ = appliedDamage;
            _ = remainingHealth;
            animationDriver?.TriggerDamage();
        }

        private void HandleDied()
        {
            animationDriver?.TriggerDie();
        }
    }
}
