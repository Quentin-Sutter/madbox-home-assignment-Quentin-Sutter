using System.Collections.Generic;
using UnityEngine;

namespace Madbox.Character
{
    /// <summary>
    /// Maintains a trigger-based set of potential enemies and exposes the closest valid target.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SphereCollider))]
    public sealed class HeroTargetingService : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float rangeRadius = 3f;

        private readonly HashSet<EnemyTargetable> _inRangeEnemies = new HashSet<EnemyTargetable>();
        private readonly List<EnemyTargetable> _cleanupBuffer = new List<EnemyTargetable>();

        private SphereCollider _rangeTrigger;
        private Transform _lockedTargetTransform;
        private EnemyTargetable _lockedTargetable;
        private bool _triggerWarningShown;

        private void Awake()
        {
            _rangeTrigger = GetComponent<SphereCollider>();
            EnsureTriggerSetup();
        }

        private void OnDisable()
        {
            ClearTrackedEnemies();
            UnsubscribeFromTargetInvalidation();
        }

        private void OnDestroy()
        {
            ClearTrackedEnemies();
            UnsubscribeFromTargetInvalidation();
        }

        private void OnValidate()
        {
            EnsureTriggerSetup();
        }

        public Transform GetCurrentTarget()
        {
            if (IsLockedTargetValid())
            {
                return _lockedTargetTransform;
            }

            RefreshTarget();
            return _lockedTargetTransform;
        }

        public bool HasValidTarget()
        {
            return GetCurrentTarget() != null;
        }

        public void BreakLock()
        {
            ClearLock();
        }

        public void RefreshTarget()
        {
            RebuildTrackedEnemiesIfNeeded();
            SetLockedTarget(FindClosestEnemy());
        }

        public void SetRange(float newRangeRadius)
        {
            rangeRadius = Mathf.Max(0.1f, newRangeRadius);
            EnsureTriggerSetup();

            if (!IsLockedTargetValid())
            {
                ClearLock();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            EnemyTargetable enemy = ResolveEnemyTargetable(other);
            if (!IsTargetValid(enemy))
            {
                return;
            }

            _inRangeEnemies.Add(enemy);
            RefreshTarget();
        }

        private void OnTriggerExit(Collider other)
        {
            EnemyTargetable enemy = ResolveEnemyTargetable(other);
            if (enemy == null)
            {
                return;
            }

            RemoveTrackedEnemy(enemy);
            if (_lockedTargetable == enemy)
            {
                ClearLock();
            }

            RefreshTarget();
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

        private bool IsLockedTargetValid()
        {
            return _lockedTargetable != null && IsEnemyValidAndInRange(_lockedTargetable) && _lockedTargetTransform != null;
        }

        private bool IsEnemyValidAndInRange(EnemyTargetable enemy)
        {
            if (!IsTargetValid(enemy))
            {
                return false;
            }

            float sqrRange = rangeRadius * rangeRadius;
            float sqrDistance = (enemy.transform.position - transform.position).sqrMagnitude;
            return sqrDistance <= sqrRange;
        }

        private static bool IsTargetValid(EnemyTargetable enemy)
        {
            if (enemy == null)
            {
                return false;
            }

            return enemy.IsTargetable;
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
                if (!IsTargetValid(enemy))
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

        private void RemoveTrackedEnemy(EnemyTargetable enemy)
        {
            if (enemy == null)
            {
                return;
            }

            _inRangeEnemies.Remove(enemy);
        }

        private void ClearTrackedEnemies()
        {
            _inRangeEnemies.Clear();
            ClearLock();
        }

        private void SetLockedTarget(EnemyTargetable nextTarget)
        {
            if (_lockedTargetable == nextTarget)
            {
                _lockedTargetTransform = nextTarget != null ? nextTarget.transform : null;
                return;
            }

            UnsubscribeFromTargetInvalidation();

            _lockedTargetable = nextTarget;
            _lockedTargetTransform = nextTarget != null ? nextTarget.transform : null;

            SubscribeToTargetInvalidation();
        }

        private void SubscribeToTargetInvalidation()
        {
            if (_lockedTargetable == null)
            {
                return;
            }

            _lockedTargetable.OnInvalidated += HandleLockedTargetInvalidated;
        }

        private void UnsubscribeFromTargetInvalidation()
        {
            if (_lockedTargetable == null)
            {
                return;
            }

            _lockedTargetable.OnInvalidated -= HandleLockedTargetInvalidated;
        }

        private void HandleLockedTargetInvalidated()
        {
            RemoveTrackedEnemy(_lockedTargetable);
            ClearLock();
            RefreshTarget();
        }

        private void ClearLock()
        {
            UnsubscribeFromTargetInvalidation();
            _lockedTargetable = null;
            _lockedTargetTransform = null;
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
