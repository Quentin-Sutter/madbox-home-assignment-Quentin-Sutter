using UnityEngine;

namespace Madbox.Character
{
    /// <summary>
    /// Marker component used by hero targeting to identify enemy roots.
    /// Also caches a damage receiver to avoid repeated lookups at hit time.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyTargetable : MonoBehaviour
    {
        public IDamageable Damageable { get; private set; }

        private Object _damageableObject;
        private bool _missingDamageableWarningShown;

        private void Awake()
        {
            CacheDamageable();

            if (Damageable == null && !_missingDamageableWarningShown)
            {
                Debug.LogWarning($"EnemyTargetable on {name} has no IDamageable in self/parents.", this);
                _missingDamageableWarningShown = true;
            }
        }

        public bool TryGetDamageable(out IDamageable damageable)
        {
            if (!IsCachedDamageableAlive())
            {
                CacheDamageable();
            }

            damageable = Damageable;
            return IsCachedDamageableAlive();
        }

        private bool IsCachedDamageableAlive()
        {
            return _damageableObject != null && Damageable != null;
        }

        private void CacheDamageable()
        {
            Component ownDamageableComponent = GetComponent(typeof(IDamageable)) as Component;
            if (ownDamageableComponent != null)
            {
                _damageableObject = ownDamageableComponent;
                Damageable = ownDamageableComponent as IDamageable;
                return;
            }

            Component parentDamageableComponent = GetComponentInParent(typeof(IDamageable)) as Component;
            if (parentDamageableComponent != null)
            {
                _damageableObject = parentDamageableComponent;
                Damageable = parentDamageableComponent as IDamageable;
                return;
            }

            _damageableObject = null;
            Damageable = null;
        }
    }
}
