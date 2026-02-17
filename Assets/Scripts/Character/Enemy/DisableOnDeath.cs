using UnityEngine;

namespace Madbox.Character
{
    /// <summary>
    /// Simple death reaction that disables the object when Health reaches zero.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DisableOnDeath : MonoBehaviour
    {
        [SerializeField] private Health health;

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
                health.OnDied += HandleDied;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.OnDied -= HandleDied;
            }
        }

        private void HandleDied()
        {
            gameObject.SetActive(false);
        }
    }
}
