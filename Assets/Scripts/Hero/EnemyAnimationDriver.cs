using UnityEngine;

namespace Madbox.Hero
{
    /// <summary>
    /// View-only animation driver for Beez enemies.
    /// Reacts to Health events and controls animator states by name.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyAnimationDriver : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private Health health;

        [Header("Animator States")]
        [SerializeField] private string idleState = "Anim_Beez_Idle";
        [SerializeField] private string moveState = "Anim_Beez_Move";
        [SerializeField] private string attackState = "Anim_Beez_Attack";
        [SerializeField] private string damageState = "Anim_Beez_Damage";
        [SerializeField] private string dieState = "Anim_Beez_Die";

        [Header("Transitions")]
        [SerializeField, Min(0f)] private float crossFadeDuration = 0.08f;

        private int _idleStateHash;
        private int _moveStateHash;
        private int _attackStateHash;
        private int _damageStateHash;
        private int _dieStateHash;

        private int _lastPlayedStateHash;
        private bool _isDead;
        private bool _isMoving;

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (health == null)
            {
                health = GetComponent<Health>();
            }

            CacheStateHashes();

            if (animator == null)
            {
                Debug.LogWarning("EnemyAnimationDriver: Animator is not assigned and no Animator was found on this GameObject.", this);
            }
        }

        private void OnEnable()
        {
            SubscribeToHealthEvents();
            SyncWithCurrentHealth();
        }

        private void OnDisable()
        {
            UnsubscribeFromHealthEvents();
        }

        /// <summary>
        /// Plays attack animation. Safe to call even if currently unused.
        /// </summary>
        public void TriggerAttack()
        {
            if (_isDead)
            {
                return;
            }

            PlayState(_attackStateHash, allowReplayCurrentState: true);
        }

        /// <summary>
        /// Optional locomotion hook for future movement support.
        /// </summary>
        public void SetMoving(bool isMoving)
        {
            if (_isDead || _isMoving == isMoving)
            {
                return;
            }

            _isMoving = isMoving;
            PlayState(_isMoving ? _moveStateHash : _idleStateHash);
        }

        private void HandleDamaged(int appliedDamage, int remainingHealth)
        {
            _ = appliedDamage;
            _ = remainingHealth;

            if (_isDead)
            {
                return;
            }

            PlayState(_damageStateHash, allowReplayCurrentState: true);
        }

        private void HandleDied()
        {
            if (_isDead)
            {
                return;
            }

            _isDead = true;
            PlayState(_dieStateHash, allowReplayCurrentState: true);
        }

        private void SubscribeToHealthEvents()
        {
            if (health == null)
            {
                return;
            }

            health.OnDamaged -= HandleDamaged;
            health.OnDamaged += HandleDamaged;

            health.OnDied -= HandleDied;
            health.OnDied += HandleDied;
        }

        private void UnsubscribeFromHealthEvents()
        {
            if (health == null)
            {
                return;
            }

            health.OnDamaged -= HandleDamaged;
            health.OnDied -= HandleDied;
        }

        private void SyncWithCurrentHealth()
        {
            bool isAlive = health == null || health.IsAlive;
            _isDead = !isAlive;

            if (_isDead)
            {
                PlayState(_dieStateHash, allowReplayCurrentState: true);
                return;
            }

            PlayState(_isMoving ? _moveStateHash : _idleStateHash);
        }

        private void PlayState(int stateHash, bool allowReplayCurrentState = false)
        {
            if (animator == null || stateHash == 0)
            {
                return;
            }

            if (!allowReplayCurrentState && _lastPlayedStateHash == stateHash)
            {
                return;
            }

            _lastPlayedStateHash = stateHash;
            animator.CrossFade(stateHash, crossFadeDuration, 0, 0f);
        }

        private void CacheStateHashes()
        {
            _idleStateHash = ToStateHash(idleState);
            _moveStateHash = ToStateHash(moveState);
            _attackStateHash = ToStateHash(attackState);
            _damageStateHash = ToStateHash(damageState);
            _dieStateHash = ToStateHash(dieState);
        }

        private static int ToStateHash(string stateName)
        {
            return string.IsNullOrWhiteSpace(stateName) ? 0 : Animator.StringToHash(stateName);
        }
    }
}
