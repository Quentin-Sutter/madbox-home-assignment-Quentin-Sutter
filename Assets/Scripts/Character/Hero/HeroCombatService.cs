using UnityEngine;

namespace Madbox.Character
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
        [SerializeField, Min(0.1f)] private float defaultAttackAnimationSpeedMultiplier = 1f;
        [SerializeField, Min(0f)] private float defaultDamageDelaySeconds = 0.1f;

        private Transform _currentTarget;
        private float _nextAttackTime;
        private bool _isAttacking;

        private bool _damagePending;
        private float _damageApplyTime;

        private WeaponData _currentWeapon;

        private void Update()
        {
            if (_damagePending && Time.time >= _damageApplyTime)
            {
                ApplyDamage(_currentTarget);
                _damagePending = false;
            }

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

        public void SetWeapon(WeaponData weapon)
        {
            _currentWeapon = weapon;
        }

        private void PerformAttack()
        {
            float attackSpeedMultiplier = GetAttackSpeedMultiplier();
            float adjustedCooldown = attackCooldownSeconds / attackSpeedMultiplier;
            _nextAttackTime = Time.time + adjustedCooldown;

            animationDriver?.TriggerAttack(attackSpeedMultiplier);

            float adjustedDamageDelay = GetDamageDelaySeconds() / attackSpeedMultiplier;
            _damageApplyTime = Time.time + adjustedDamageDelay;
            _damagePending = true;
        }

        private float GetAttackSpeedMultiplier()
        {
            if (_currentWeapon == null)
            {
                return Mathf.Max(0.1f, defaultAttackAnimationSpeedMultiplier);
            }

            return Mathf.Max(0.1f, _currentWeapon.AttackAnimationSpeedMultiplier);
        }

        private float GetDamageDelaySeconds()
        {
            if (_currentWeapon == null)
            {
                return Mathf.Max(0f, defaultDamageDelaySeconds);
            }

            return Mathf.Max(0f, _currentWeapon.DamageDelaySeconds);
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
