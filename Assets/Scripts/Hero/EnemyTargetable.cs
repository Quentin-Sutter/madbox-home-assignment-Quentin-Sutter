using UnityEngine;

namespace Madbox.Hero
{
    /// <summary>
    /// Marker component used by hero targeting to identify enemy roots.
    /// Also caches a damage receiver to avoid repeated lookups at hit time.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyTargetable : MonoBehaviour
    {
        public IDamageable Damageable { get; private set; }

        private bool _missingDamageableWarningShown;

        private void Awake()
        {
            Damageable = GetComponent<IDamageable>() ?? GetComponentInParent<IDamageable>();

            if (Damageable == null && !_missingDamageableWarningShown)
            {
                Debug.LogWarning($"EnemyTargetable on {name} has no IDamageable in self/parents.", this);
                _missingDamageableWarningShown = true;
            }
        }
    }
}
