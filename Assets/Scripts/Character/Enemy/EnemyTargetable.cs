using System;
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
        public event Action OnInvalidated;

        public IDamageable Damageable { get; private set; }
        public bool IsTargetable => isActiveAndEnabled && gameObject.activeInHierarchy && (_health == null || _health.IsAlive);

        private UnityEngine.Object _damageableObject;
        private Health _health;
        private bool _missingDamageableWarningShown;
        private bool _isQuitting;

        private void Awake()
        {
            CacheDamageable();
            _health = GetComponentInParent<Health>();

            if (Damageable == null && !_missingDamageableWarningShown)
            {
                Debug.LogWarning($"EnemyTargetable on {name} has no IDamageable in self/parents.", this);
                _missingDamageableWarningShown = true;
            }
        }

        private void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        private void OnDisable()
        {
            // Pooled enemies can be re-enabled later; listeners must drop stale locks on disable.
            if (_isQuitting)
            {
                return;
            }

            OnInvalidated?.Invoke();
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
