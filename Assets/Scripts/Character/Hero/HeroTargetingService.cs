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

        private SphereCollider _rangeTrigger;
        private EnemyTargetable _lockedTarget;
        private bool _triggerWarningShown;

        private void Awake()
        {
            _rangeTrigger = GetComponent<SphereCollider>();
            EnsureTriggerSetup();
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

            RebuildTrackedEnemiesIfNeeded();
            _lockedTarget = FindClosestEnemy();
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

            _inRangeEnemies.Add(enemy);

            if (!IsEnemyValidAndInRange(_lockedTarget))
            {
                _lockedTarget = FindClosestEnemy();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            EnemyTargetable enemy = ResolveEnemyTargetable(other);
            if (enemy == null)
            {
                return;
            }

            _inRangeEnemies.Remove(enemy);
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
            if (enemy == null || !enemy.gameObject.activeInHierarchy)
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

            _inRangeEnemies.RemoveWhere(IsInvalidEnemy);
        }

        private static bool IsInvalidEnemy(EnemyTargetable enemy)
        {
            return enemy == null || !enemy.gameObject.activeInHierarchy;
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
