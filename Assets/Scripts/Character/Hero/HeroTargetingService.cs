using System.Collections.Generic;
using UnityEngine;

namespace Madbox.Character
{
    /// <summary>
    /// Maintains a trigger-based set of potential enemies and exposes the closest valid target.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SphereCollider))]
    public sealed class HeroTargetingService : MonoBehaviour, IHeroTargetingService
    {
        [SerializeField, Min(0.1f)] private float rangeRadius = 3f;

        private readonly HashSet<EnemyTargetable> _inRangeEnemies = new HashSet<EnemyTargetable>();
        private readonly Dictionary<EnemyTargetable, Health> _trackedEnemyHealth = new Dictionary<EnemyTargetable, Health>();
        private readonly List<EnemyTargetable> _cleanupBuffer = new List<EnemyTargetable>();

        private SphereCollider _rangeTrigger;
        private EnemyTargetable _lockedTarget;
        private bool _triggerWarningShown;
        private bool _missingHealthWarningShown;

        private void Awake()
        {
            _rangeTrigger = GetComponent<SphereCollider>();
            EnsureTriggerSetup();
        }

        private void OnDisable()
        {
            ClearTrackedEnemies();
        }

        private void OnDestroy()
        {
            ClearTrackedEnemies();
        }

        private void OnValidate()
        {
            EnsureTriggerSetup();
        }

        public Transform GetCurrentTarget()
        {
            if (IsEnemyValidAndInRange(_lockedTarget))
            {
                return _lockedTarget.transform;
            }

            RefreshTarget();
            return _lockedTarget != null ? _lockedTarget.transform : null;
        }

        public bool HasValidTarget()
        {
            return GetCurrentTarget() != null;
        }

        public void BreakLock()
        {
            _lockedTarget = null;
        }

        public void RefreshTarget()
        {
            RebuildTrackedEnemiesIfNeeded();
            _lockedTarget = FindClosestEnemy();
        }

        public void SetRange(float newRangeRadius)
        {
            rangeRadius = Mathf.Max(0.1f, newRangeRadius);
            EnsureTriggerSetup();

            if (_lockedTarget != null && !IsEnemyValidAndInRange(_lockedTarget))
            {
                _lockedTarget = null;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            EnemyTargetable enemy = ResolveEnemyTargetable(other);
            if (enemy == null || !enemy.gameObject.activeInHierarchy)
            {
                return;
            }

            if (_inRangeEnemies.Add(enemy))
            {
                SubscribeToEnemyDeath(enemy);
            }

            if (!IsEnemyValidAndInRange(_lockedTarget))
            {
                RefreshTarget();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            EnemyTargetable enemy = ResolveEnemyTargetable(other);
            if (enemy == null)
            {
                return;
            }

            RemoveTrackedEnemy(enemy);
            if (_lockedTarget == enemy)
            {
                _lockedTarget = null;
            }
        }

        private EnemyTargetable FindClosestEnemy()
        {
            EnemyTargetable closest = null;
            float closestSqrDistance = float.MaxValue;
            Vector3 origin = transform.position;

            foreach (EnemyTargetable enemy in _inRangeEnemies)
            {
                if (!IsEnemyValidAndInRange(enemy))
                {
                    continue;
                }

                float sqrDistance = (enemy.transform.position - origin).sqrMagnitude;
                if (sqrDistance >= closestSqrDistance)
                {
                    continue;
                }

                closest = enemy;
                closestSqrDistance = sqrDistance;
            }

            return closest;
        }

        private bool IsEnemyValidAndInRange(EnemyTargetable enemy)
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy || !IsEnemyAlive(enemy))
            {
                return false;
            }

            float sqrRange = rangeRadius * rangeRadius;
            float sqrDistance = (enemy.transform.position - transform.position).sqrMagnitude;
            return sqrDistance <= sqrRange;
        }

        private void RebuildTrackedEnemiesIfNeeded()
        {
            if (_inRangeEnemies.Count == 0)
            {
                return;
            }

            _cleanupBuffer.Clear();

            foreach (EnemyTargetable enemy in _inRangeEnemies)
            {
                if (IsInvalidEnemy(enemy) || !IsEnemyAlive(enemy))
                {
                    _cleanupBuffer.Add(enemy);
                }
            }

            for (int i = 0; i < _cleanupBuffer.Count; i++)
            {
                RemoveTrackedEnemy(_cleanupBuffer[i]);
            }

            _cleanupBuffer.Clear();
        }

        private static bool IsInvalidEnemy(EnemyTargetable enemy)
        {
            return enemy == null || !enemy.gameObject.activeInHierarchy;
        }

        private bool IsEnemyAlive(EnemyTargetable enemy)
        {
            if (enemy == null)
            {
                return false;
            }

            if (_trackedEnemyHealth.TryGetValue(enemy, out Health health) && health != null)
            {
                return health.IsAlive;
            }

            return true;
        }

        private void SubscribeToEnemyDeath(EnemyTargetable enemy)
        {
            if (enemy == null || _trackedEnemyHealth.ContainsKey(enemy))
            {
                return;
            }

            Health enemyHealth = enemy.GetComponentInParent<Health>();
            if (enemyHealth == null)
            {
                if (!_missingHealthWarningShown)
                {
                    Debug.LogWarning("HeroTargetingService: EnemyTargetable has no Health in self/parents.", this);
                    _missingHealthWarningShown = true;
                }

                return;
            }

            enemyHealth.OnDied += OnTrackedEnemyDied;
            _trackedEnemyHealth.Add(enemy, enemyHealth);
        }

        private void RemoveTrackedEnemy(EnemyTargetable enemy)
        {
            if (enemy == null)
            {
                return;
            }

            if (_trackedEnemyHealth.TryGetValue(enemy, out Health health) && health != null)
            {
                health.OnDied -= OnTrackedEnemyDied;
            }

            _trackedEnemyHealth.Remove(enemy);
            _inRangeEnemies.Remove(enemy);
        }

        private void ClearTrackedEnemies()
        {
            if (_trackedEnemyHealth.Count > 0)
            {
                foreach (KeyValuePair<EnemyTargetable, Health> pair in _trackedEnemyHealth)
                {
                    if (pair.Value != null)
                    {
                        pair.Value.OnDied -= OnTrackedEnemyDied;
                    }
                }

                _trackedEnemyHealth.Clear();
            }

            _inRangeEnemies.Clear();
            _lockedTarget = null;
        }

        private void OnTrackedEnemyDied()
        {
            RefreshTarget();
        }

        private EnemyTargetable ResolveEnemyTargetable(Collider other)
        {
            if (other == null)
            {
                return null;
            }

            if (other.TryGetComponent(out EnemyTargetable enemy))
            {
                return enemy;
            }

            return other.GetComponentInParent<EnemyTargetable>();
        }

        private void EnsureTriggerSetup()
        {
            if (_rangeTrigger == null)
            {
                _rangeTrigger = GetComponent<SphereCollider>();
            }

            if (_rangeTrigger == null)
            {
                if (!_triggerWarningShown)
                {
                    Debug.LogWarning("HeroTargetingService: SphereCollider trigger is required.");
                    _triggerWarningShown = true;
                }

                return;
            }

            _rangeTrigger.isTrigger = true;
            _rangeTrigger.radius = rangeRadius;
        }
    }
}
