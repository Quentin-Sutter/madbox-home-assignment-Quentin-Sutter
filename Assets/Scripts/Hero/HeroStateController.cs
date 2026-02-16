using Madbox.Input;
using Madbox.Movement;
using UnityEngine;

namespace Madbox.Hero
{
    public interface IHeroTargetingService
    {
        Transform GetCurrentTarget();
        bool HasValidTarget();
        void BreakLock();
    }

    public interface IHeroCombatService
    {
        bool TryStartAttack(Transform target);
        void CancelAttack();
    }

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

        [Header("Tuning")]
        [SerializeField, Range(0f, 1f)] private float moveDeadzone = 0.05f;

        [Header("Future Hooks")]
        [SerializeField] private MonoBehaviour targetingServiceSource;
        [SerializeField] private MonoBehaviour combatServiceSource;

        public HeroState CurrentState => _currentState;

        private HeroState _currentState = HeroState.Idle;
        private Transform _attackTarget;
        private IHeroTargetingService _targetingService;
        private IHeroCombatService _combatService;

        private bool _inputWarningShown;
        private bool _movementWarningShown;
        private bool _rotationWarningShown;

        private void Awake()
        {
            _targetingService = targetingServiceSource as IHeroTargetingService;
            _combatService = combatServiceSource as IHeroCombatService;
        }

        private void Update()
        {
            if (!ValidateCoreReferences())
            {
                return;
            }

            MoveIntent intent = inputSource.CurrentIntent;
            HeroState desiredState = EvaluateDesiredState(intent);

            if (desiredState != _currentState)
            {
                TransitionTo(desiredState);
            }

            TickCurrentState(intent);
        }

        private HeroState EvaluateDesiredState(MoveIntent intent)
        {
            bool isMoving = intent.IsMoving && intent.Strength > moveDeadzone && intent.WorldDirection.sqrMagnitude > 0.0001f;
            if (isMoving)
            {
                return HeroState.Move;
            }

            bool hasTarget = _targetingService != null && _targetingService.HasValidTarget();
            return hasTarget ? HeroState.Attack : HeroState.Idle;
        }

        private void TransitionTo(HeroState nextState)
        {
            ExitState(_currentState);
            _currentState = nextState;
            EnterState(_currentState);
        }

        private void EnterState(HeroState state)
        {
            switch (state)
            {
                case HeroState.Move:
                    _combatService?.CancelAttack();
                    _targetingService?.BreakLock();
                    _attackTarget = null;
                    break;

                case HeroState.Attack:
                    AcquireAttackTarget();
                    TryStartAttackOnCurrentTarget();
                    break;

                case HeroState.Idle:
                    movement.Stop();
                    _attackTarget = null;
                    break;
            }
        }

        private void ExitState(HeroState state)
        {
            if (state == HeroState.Move)
            {
                movement.Stop();
            }
        }

        private void TickCurrentState(MoveIntent intent)
        {
            switch (_currentState)
            {
                case HeroState.Move:
                    movement.Move(intent.WorldDirection, intent.Strength);
                    rotation.FaceDirection(intent.WorldDirection);
                    break;

                case HeroState.Attack:
                    TickAttackState();
                    break;

                case HeroState.Idle:
                    TickIdleState();
                    break;
            }
        }

        private void TickAttackState()
        {
            if (!IsTargetValid(_attackTarget))
            {
                AcquireAttackTarget();
                TryStartAttackOnCurrentTarget();
            }

            if (IsTargetValid(_attackTarget))
            {
                rotation.FaceTarget(_attackTarget);
            }
        }

        private void TickIdleState()
        {
            movement.Stop();

            if (_targetingService == null || !_targetingService.HasValidTarget())
            {
                return;
            }

            Transform target = _targetingService.GetCurrentTarget();
            rotation.FaceTarget(target);
        }

        private void AcquireAttackTarget()
        {
            _attackTarget = _targetingService != null ? _targetingService.GetCurrentTarget() : null;
        }

        private void TryStartAttackOnCurrentTarget()
        {
            if (_combatService == null || !IsTargetValid(_attackTarget))
            {
                return;
            }

            _combatService.TryStartAttack(_attackTarget);
        }

        private static bool IsTargetValid(Transform target)
        {
            return target != null && target.gameObject.activeInHierarchy;
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

            return true;
        }
    }
}
