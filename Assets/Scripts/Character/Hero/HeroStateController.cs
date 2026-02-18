using Madbox.Input;
using Madbox.Movement;
using UnityEngine;

namespace Madbox.Character
{
    public enum HeroState
    {
        Idle,
        Move,
        Attack
    }

    /// <summary>
    /// Centralizes hero state transitions and orchestrates movement/rotation/combat hooks.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HeroStateController : MonoBehaviour
    {
        [Header("Core")]
        [SerializeField] private JoystickInput inputSource;
        [SerializeField] private HeroMovement movement;
        [SerializeField] private HeroRotation rotation;
        [SerializeField] private CharacterAnimationDriver animationDriver;

        [Header("Tuning")]
        [SerializeField, Range(0f, 1f)] private float moveDeadzone = 0.05f;

        [Header("Future Hooks")]
        [SerializeField] private HeroTargetingService targetingService;
        [SerializeField] private HeroCombatService combatService;

        public HeroState CurrentState => _currentState;
        public event System.Action<HeroState> OnStateChanged;

        private HeroState _currentState = HeroState.Idle;
        private Transform _attackTarget;
        private bool _inputWarningShown;
        private bool _movementWarningShown;
        private bool _rotationWarningShown;
        private bool _targetingWarningShown;
        private bool _combatWarningShown;
        private bool _wasMovingLastFrame;

        private void Awake()
        {
            AutoAssignReferences();
        }

        private void Update()
        {
            if (!ValidateCoreReferences())
            {
                return;
            }

            MoveIntent intent = inputSource.CurrentIntent;
            bool isMovingNow = IsMoveIntentActive(intent);

            if (_wasMovingLastFrame && !isMovingNow)
            {
                targetingService?.RefreshTarget();
            }

            HeroState desiredState = EvaluateDesiredState(intent);

            if (desiredState != _currentState)
            {
                TransitionTo(desiredState);
            }

            TickCurrentState(intent);
            _wasMovingLastFrame = isMovingNow;
        }

        private HeroState EvaluateDesiredState(MoveIntent intent)
        {
            bool isMoving = IsMoveIntentActive(intent);
            if (isMoving)
            {
                return HeroState.Move;
            }

            bool hasTarget = targetingService != null && targetingService.HasValidTarget();
            return hasTarget ? HeroState.Attack : HeroState.Idle;
        }

        private void TransitionTo(HeroState nextState)
        {
            ExitState(_currentState);
            _currentState = nextState;
            EnterState(_currentState);
            OnStateChanged?.Invoke(_currentState);
        }

        private void EnterState(HeroState state)
        {
            switch (state)
            {
                case HeroState.Move:
                    combatService?.CancelAttack();
                    targetingService?.BreakLock();
                    _attackTarget = null;
                    break;

                case HeroState.Attack:
                    AcquireAttackTarget();
                    TryStartAttackOnCurrentTarget();
                    break;

                case HeroState.Idle:
                    StopMovement();
                    _attackTarget = null;
                    break;
            }
        }

        private void ExitState(HeroState state)
        {
            if (state == HeroState.Move)
            {
                StopMovement();
            }

            if (state == HeroState.Attack)
            {
                combatService?.CancelAttack();
            }
        }

        private void TickCurrentState(MoveIntent intent)
        {
            switch (_currentState)
            {
                case HeroState.Move:
                    TickMoveIntent(intent);
                    break;

                case HeroState.Attack:
                    SetIdleAnimation();
                    TickAttackIntent();
                    break;

                case HeroState.Idle:
                    SetIdleAnimation();
                    TickIdleIntent();
                    break;
            }
        }

        private void TickMoveIntent(MoveIntent intent)
        {
            movement.Move(intent.WorldDirection, intent.Strength);
            rotation.FaceDirection(intent.WorldDirection);
            animationDriver?.SetMoveAmount(intent.Strength);
        }

        private void TickAttackIntent()
        {
            Transform lockedTarget = combatService != null ? combatService.CurrentLockedTarget : null;

            if (!IsTargetValid(lockedTarget) && (combatService == null || !combatService.IsAttackInProgress))
            {
                AcquireAttackTarget();
                TryStartAttackOnCurrentTarget();
                lockedTarget = combatService != null ? combatService.CurrentLockedTarget : _attackTarget;
            }

            if (lockedTarget != null)
            {
                rotation.FaceTarget(lockedTarget);
            }
        }

        private void TickIdleIntent()
        {
            StopMovement();

            if (targetingService == null || !targetingService.HasValidTarget())
            {
                return;
            }

            Transform target = targetingService.GetCurrentTarget();
            rotation.FaceTarget(target);
        }

        private void StopMovement()
        {
            movement.Stop();
        }

        private void SetIdleAnimation()
        {
            animationDriver?.SetMoveAmount(0f);
        }

        private void AcquireAttackTarget()
        {
            _attackTarget = targetingService != null ? targetingService.GetCurrentTarget() : null;
        }

        private void TryStartAttackOnCurrentTarget()
        {
            if (combatService == null || !IsTargetValid(_attackTarget))
            {
                return;
            }

            combatService.TryStartAttack(_attackTarget);
        }

        private static bool IsTargetValid(Transform target)
        {
            return target != null && target.gameObject.activeInHierarchy;
        }

        private bool IsMoveIntentActive(MoveIntent intent)
        {
            return intent.IsMoving && intent.Strength > moveDeadzone && intent.WorldDirection.sqrMagnitude > 0.0001f;
        }

        private bool ValidateCoreReferences()
        {
            if (inputSource == null)
            {
                if (!_inputWarningShown)
                {
                    Debug.LogWarning("HeroStateController: inputSource is not assigned.");
                    _inputWarningShown = true;
                }

                return false;
            }

            if (movement == null)
            {
                if (!_movementWarningShown)
                {
                    Debug.LogWarning("HeroStateController: movement is not assigned.");
                    _movementWarningShown = true;
                }

                return false;
            }

            if (rotation == null)
            {
                if (!_rotationWarningShown)
                {
                    Debug.LogWarning("HeroStateController: rotation is not assigned.");
                    _rotationWarningShown = true;
                }

                return false;
            }

            if (targetingService == null)
            {
                if (!_targetingWarningShown)
                {
                    Debug.LogWarning("HeroStateController: targetingService is not assigned.");
                    _targetingWarningShown = true;
                }
            }

            if (combatService == null)
            {
                if (!_combatWarningShown)
                {
                    Debug.LogWarning("HeroStateController: combatService is not assigned.");
                    _combatWarningShown = true;
                }
            }

            return true;
        }

        private void AutoAssignReferences()
        {
            if (targetingService == null)
            {
                targetingService = GetComponent<HeroTargetingService>();
            }

            if (combatService == null)
            {
                combatService = GetComponent<HeroCombatService>();
            }
        }
    }
}
