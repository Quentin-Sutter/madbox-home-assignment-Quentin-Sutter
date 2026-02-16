using UnityEngine;

namespace Madbox.Hero
{
    /// <summary>
    /// Minimal auto-attack service with cooldown and optional animation hooks.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HeroCombatService : MonoBehaviour, IHeroCombatService
    {
        [SerializeField, Min(0.05f)] private float attackCooldownSeconds = 0.75f;
        [SerializeField, Min(1)] private int attackDamage = 1;
        [SerializeField] private Animator animator;
        [SerializeField] private string attackTriggerName = "Attack";
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

            if (animator != null && !string.IsNullOrEmpty(attackTriggerName))
            {
                animator.SetTrigger(attackTriggerName);
            }

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

            EnemyHealth enemyHealth = target.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.ApplyDamage(attackDamage);
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
