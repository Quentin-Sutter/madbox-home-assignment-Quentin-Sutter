using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem;

namespace Madbox.Input
{
    /// <summary>
    /// Reads pointer input and produces movement intent + visual state.
    /// Input and gameplay are separated from UI rendering.
    /// </summary>
    public sealed class JoystickInput : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private Camera gameplayCamera;
        [SerializeField, Min(1f)] private float joystickRadius = 120f;
        [SerializeField, Range(0f, 1f)] private float deadzone = 0.1f;

        public MoveIntent CurrentIntent { get; private set; } = MoveIntent.Idle;
        public JoystickVisualState CurrentVisualState { get; private set; } = JoystickVisualState.Hidden;

        public bool IsActive => _isDragging;
        public Vector2 BaseScreenPosition => _pressStartScreenPos;
        public Vector2 Direction { get; private set; } = Vector2.zero;
        public float Magnitude01 { get; private set; }

        private bool _isDragging;
        private Vector2 _pressStartScreenPos;
        private bool _cameraWarningShown;
        private ActivePointerType _activePointerType;
        private int _activePointerId = InvalidPointerId;
        private InputAction _pointAction;
        private InputAction _pressAction;

        private const int MousePointerId = -1;
        private const int InvalidPointerId = int.MinValue;

        private enum ActivePointerType
        {
            None,
            Mouse,
            Touch
        }

        private void Awake()
        {
            if (gameplayCamera == null)
            {
                gameplayCamera = Camera.main;
            }

            SetupActions();
        }

        private void OnEnable()
        {
            SetupActions();

            _pressAction.started += OnPressStarted;
            _pressAction.canceled += OnPressCanceled;
            _pointAction.performed += OnPointPerformed;

            _pointAction.Enable();
            _pressAction.Enable();
        }

        private void OnDisable()
        {
            if (_pressAction != null)
            {
                _pressAction.started -= OnPressStarted;
                _pressAction.canceled -= OnPressCanceled;
                _pressAction.Disable();
            }

            if (_pointAction != null)
            {
                _pointAction.performed -= OnPointPerformed;
                _pointAction.Disable();
            }

            EndDrag();
        }


        private void OnDestroy()
        {
            _pressAction?.Dispose();
            _pointAction?.Dispose();
            _pressAction = null;
            _pointAction = null;
        }
        /// <summary>
        /// Action-based setup to keep pointer sources extensible (mouse, touch, pen, etc.)
        /// and compatible with rebinding workflows.
        /// </summary>
        private void SetupActions()
        {
            if (_pointAction != null && _pressAction != null)
            {
                return;
            }

            _pointAction = new InputAction(name: "Point", type: InputActionType.Value, binding: "<Pointer>/position");
            _pressAction = new InputAction(name: "Press", type: InputActionType.Button, binding: "<Pointer>/press");
        }

        private void OnPressStarted(InputAction.CallbackContext context)
        {
            if (_isDragging)
            {
                return;
            }

            int pointerId = GetPointerId(context);
            if (IsPointerOverUi(pointerId))
            {
                return;
            }

            Vector2 pressScreenPos = _pointAction.ReadValue<Vector2>();
            BeginDrag(pressScreenPos, GetPointerType(context), pointerId);
        }

        private void OnPointPerformed(InputAction.CallbackContext context)
        {
            if (!_isDragging)
            {
                return;
            }

            if (_activePointerType == ActivePointerType.Touch && !IsActiveTouchControl(context.control))
            {
                return;
            }

            UpdateDrag(context.ReadValue<Vector2>());
        }

        private void OnPressCanceled(InputAction.CallbackContext context)
        {
            if (!_isDragging)
            {
                return;
            }

            if (_activePointerType == ActivePointerType.Touch && !IsActiveTouchControl(context.control))
            {
                return;
            }

            EndDrag();
        }

        private ActivePointerType GetPointerType(InputAction.CallbackContext context)
        {
            return context.control?.device is Touchscreen ? ActivePointerType.Touch : ActivePointerType.Mouse;
        }

        private int GetPointerId(InputAction.CallbackContext context)
        {
            if (context.control?.device is not Touchscreen)
            {
                return MousePointerId;
            }

            if (context.control.parent is TouchControl touch)
            {
                return touch.touchId.ReadValue();
            }

            if (Touchscreen.current != null)
            {
                return Touchscreen.current.primaryTouch.touchId.ReadValue();
            }

            return InvalidPointerId;
        }

        private bool IsActiveTouchControl(InputControl control)
        {
            if (control?.parent is not TouchControl touch)
            {
                // Best effort for <Pointer>-based callbacks where the touch id may not be exposed.
                return true;
            }

            return touch.touchId.ReadValue() == _activePointerId;
        }

        private bool IsPointerOverUi(int pointerId)
        {
            EventSystem eventSystem = EventSystem.current;
            return eventSystem != null && eventSystem.IsPointerOverGameObject(pointerId);
        }

        private void BeginDrag(Vector2 pressScreenPos, ActivePointerType pointerType, int pointerId)
        {
            _isDragging = true;
            _activePointerType = pointerType;
            _activePointerId = pointerId;
            _pressStartScreenPos = pressScreenPos;
            Direction = Vector2.zero;
            Magnitude01 = 0f;
            CurrentVisualState = new JoystickVisualState(true, _pressStartScreenPos, Direction, Magnitude01);
            CurrentIntent = MoveIntent.Idle;
        }

        private void UpdateDrag(Vector2 currentScreenPos)
        {
            Vector2 delta = currentScreenPos - _pressStartScreenPos;
            float rawMagnitude = delta.magnitude;
            Vector2 direction = rawMagnitude > 0.0001f ? delta / rawMagnitude : Vector2.zero;
            float normalizedMagnitude = Mathf.Clamp01(rawMagnitude / joystickRadius);

            bool isMoving = normalizedMagnitude > deadzone && direction.sqrMagnitude > 0f;
            float adjustedStrength = isMoving
                ? Mathf.InverseLerp(deadzone, 1f, normalizedMagnitude)
                : 0f;

            Direction = direction;
            Magnitude01 = normalizedMagnitude;

            Vector3 worldDirection = isMoving ? ToWorldDirection(Direction) : Vector3.zero;

            CurrentIntent = new MoveIntent(worldDirection, adjustedStrength, isMoving);
            CurrentVisualState = new JoystickVisualState(true, _pressStartScreenPos, Direction, Magnitude01);
        }

        private void EndDrag()
        {
            _isDragging = false;
            _activePointerType = ActivePointerType.None;
            _activePointerId = InvalidPointerId;
            Direction = Vector2.zero;
            Magnitude01 = 0f;
            CurrentIntent = MoveIntent.Idle;
            CurrentVisualState = JoystickVisualState.Hidden;
        }

        private Vector3 ToWorldDirection(Vector2 screenDirection)
        {
            if (gameplayCamera == null)
            {
                gameplayCamera = Camera.main;
                if (gameplayCamera == null)
                {
                    if (!_cameraWarningShown)
                    {
                        Debug.LogWarning("JoystickInput: gameplayCamera missing, using world axes fallback.");
                        _cameraWarningShown = true;
                    }

                    return new Vector3(screenDirection.x, 0f, screenDirection.y).normalized;
                }
            }

            Vector3 forward = gameplayCamera.transform.forward;
            forward.y = 0f;
            forward.Normalize();

            Vector3 right = gameplayCamera.transform.right;
            right.y = 0f;
            right.Normalize();

            if (forward.sqrMagnitude < 0.0001f || right.sqrMagnitude < 0.0001f)
            {
                return new Vector3(screenDirection.x, 0f, screenDirection.y).normalized;
            }

            Vector3 worldDirection = (right * screenDirection.x) + (forward * screenDirection.y);
            return worldDirection.sqrMagnitude > 0.0001f ? worldDirection.normalized : Vector3.zero;
        }
    }
}
