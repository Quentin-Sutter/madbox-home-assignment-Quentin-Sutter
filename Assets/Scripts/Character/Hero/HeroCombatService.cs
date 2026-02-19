using System;
using UnityEngine;

namespace Madbox.Character
{
    /// <summary>
    /// Handles attack cadence and damage application. Visual state is delegated to CharacterAnimationDriver.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HeroCombatService : MonoBehaviour
    {
        [SerializeField] private CharacterAnimationDriver animationDriver;
        [SerializeField] private HeroTargetingService targetingService;

        public event Action<float> OnAttackCooldownStarted;

        public Transform CurrentLockedTarget => _currentTarget;
        public bool IsAttackInProgress => _attackInProgress;

        private Transform _currentTarget;
        private float _nextAttackTime;
        private bool _isAttacking;

        private bool _damagePending;
        private float _damageApplyTime;
        private int _pendingDamageVersion;

        private WeaponData _currentWeapon;

        private bool _attackInProgress;
        private float _attackEndTime;

        private bool _targetingWarningShown;

        private void Awake()
        {
            AutoAssignReferences();
        }

        private void Update()
        {
            _currentTarget = GetTargetFromTargeting();

            if (_damagePending && Time.time >= _damageApplyTime)
            {
                int pendingVersion = _pendingDamageVersion;
                ApplyPendingDamage(pendingVersion);
            }

            if (_attackInProgress && Time.time >= _attackEndTime)
            {
                _attackInProgress = false;
            }

            if (!_isAttacking)
            {
                return;
            }

            if (!_attackInProgress && !IsTargetValid(_currentTarget))
            {
                CancelAttack(); 
                return;
            }

            if (!_attackInProgress && Time.time >= _nextAttackTime)
            {
                PerformAttack();
            }
        }

        public bool TryStartAttack(Transform target)
        {
            _currentTarget = GetTargetFromTargeting();

            if (IsTargetValid(_currentTarget))
            {
                _isAttacking = true;
                return true;
            }

            if (!IsTargetValid(target))
            {
                target = _currentTarget;
            }

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
            _attackInProgress = false;
            _damagePending = false;
            _pendingDamageVersion++;
        }

        public void SetWeapon(WeaponData weapon)
        {
            _currentWeapon = weapon;
        }

        private void PerformAttack()
        {
            if (_currentWeapon == null)
            {
                return;
            }

            float cooldown = Mathf.Max(0.01f, _currentWeapon.AttackCooldownSeconds);
            _nextAttackTime = Time.time + cooldown;
            _attackEndTime = _nextAttackTime;
            _attackInProgress = true;

            float animationSpeed = animationDriver != null
                ? animationDriver.ComputeAttackSpeedForCooldown(cooldown)
                : 1f;

            animationDriver?.TriggerAttack(animationSpeed);
            OnAttackCooldownStarted?.Invoke(cooldown);

            float hitDelay = cooldown * Mathf.Clamp01(_currentWeapon.HitTimeNormalized);
            _damageApplyTime = Time.time + hitDelay;
            _damagePending = true;
            _pendingDamageVersion++;
        }

        private void ApplyPendingDamage(int pendingVersion)
        {
            if (!_damagePending || pendingVersion != _pendingDamageVersion)
            {
                return;
            }

            _damagePending = false;
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
                cachedDamageable.ApplyDamage(_currentWeapon.DamageOnHit);
                return;
            }

            IDamageable damageable = target.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.ApplyDamage(_currentWeapon.DamageOnHit);
                return;
            }

            Debug.Log($"HeroCombatService: Attack landed on {target.name}.", this);
        }

        private Transform GetTargetFromTargeting()
        {
            if (targetingService == null)
            {
                if (!_targetingWarningShown)
                {
                    Debug.LogWarning("HeroCombatService: targetingService is not assigned.", this);
                    _targetingWarningShown = true;
                }

                return null;
            }

            Transform nextTarget = targetingService.GetCurrentTarget();
            return IsTargetValid(nextTarget) ? nextTarget : null;
        }

        private void AutoAssignReferences()
        {
            if (targetingService == null)
            {
                targetingService = GetComponent<HeroTargetingService>();
            }

            if (animationDriver == null)
            {
                animationDriver = GetComponent<CharacterAnimationDriver>();
            }
        }

        private static bool IsTargetValid(Transform target)
        {
            if (target == null)
            {
                return false;
            }

            if (target.TryGetComponent(out EnemyTargetable enemyTargetable))
            {
                return enemyTargetable.IsTargetable;
            }

            return target.gameObject.activeInHierarchy;
        }
    }
}
