using UnityEngine;

namespace Madbox.Hero
{
    /// <summary>
    /// Owns combat timing and damage application.
    /// Animation playback is delegated to CharacterAnimationDriver.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HeroCombatService : MonoBehaviour, IHeroCombatService
    {
        [SerializeField, Min(0.05f)] private float attackCooldownSeconds = 0.75f;
        [SerializeField, Min(1)] private int attackDamage = 1;
        [SerializeField] private CharacterAnimationDriver animationDriver;
        [SerializeField, Min(0f)] private float attackSpeedMultiplier = 1f;
        [SerializeField] private bool useAnimationEventForDamage;

        private Transform _currentTarget;
        private float _nextAttackTime;
        private bool _isAttacking;
        private bool _damagePending;

        private void Update()
        {
            if (!_isAttacking)
            {
                return;
            }

            if (!IsTargetValid(_currentTarget))
            {
                CancelAttack();
                return;
            }

            if (Time.time >= _nextAttackTime)
            {
                PerformAttack();
            }
        }

        public bool TryStartAttack(Transform target)
        {
            if (!IsTargetValid(target) || Time.time < _nextAttackTime)
            {
                return false;
            }

            _currentTarget = target;
            _isAttacking = true;
            PerformAttack();
            return true;
        }

        public void CancelAttack()
        {
            _currentTarget = null;
            _isAttacking = false;
            _damagePending = false;
        }

        /// <summary>
        /// Optional animation event callback: call this from the attack clip to apply damage.
        /// </summary>
        public void AnimationEvent_DealDamage()
        {
            if (!_isAttacking || !_damagePending || !IsTargetValid(_currentTarget))
            {
                return;
            }

            ApplyDamage(_currentTarget);
            _damagePending = false;
        }

        private void PerformAttack()
        {
            _nextAttackTime = Time.time + attackCooldownSeconds;
            animationDriver?.TriggerAttack(attackSpeedMultiplier);

            if (useAnimationEventForDamage)
            {
                _damagePending = true;
                return;
            }

            ApplyDamage(_currentTarget);
        }

        private void ApplyDamage(Transform target)
        {
            if (!IsTargetValid(target))
            {
                return;
            }

            if (target.TryGetComponent(out EnemyTargetable enemyTargetable) &&
                enemyTargetable.TryGetDamageable(out IDamageable cachedDamageable))
            {
                cachedDamageable.ApplyDamage(attackDamage);
                return;
            }

            IDamageable damageable = target.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.ApplyDamage(attackDamage);
                return;
            }

            Debug.Log($"HeroCombatService: Attack landed on {target.name}.", this);
        }

        private static bool IsTargetValid(Transform target)
        {
            return target != null && target.gameObject.activeInHierarchy;
        }
    }
}
