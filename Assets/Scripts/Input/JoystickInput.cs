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
        }

        private void Update()
        {
            if (_isDragging)
            {
                UpdateActiveDrag();
                return;
            }

            TryStartDragFromTouch();
            if (_isDragging)
            {
                return;
            }

            TryStartDragFromMouse();
        }

        private void TryStartDragFromTouch()
        {
            if (Touchscreen.current == null)
            {
                return;
            }

            var touches = Touchscreen.current.touches;
            for (int i = 0; i < touches.Count; i++)
            {
                TouchControl touch = touches[i];
                if (!touch.press.wasPressedThisFrame)
                {
                    continue;
                }

                int pointerId = touch.touchId.ReadValue();
                if (IsPointerOverUi(pointerId))
                {
                    continue;
                }

                BeginDrag(touch.position.ReadValue(), ActivePointerType.Touch, pointerId);
                return;
            }
        }

        private void TryStartDragFromMouse()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            {
                return;
            }

            if (IsPointerOverUi(MousePointerId))
            {
                return;
            }

            BeginDrag(mouse.position.ReadValue(), ActivePointerType.Mouse, MousePointerId);
        }

        private void UpdateActiveDrag()
        {
            switch (_activePointerType)
            {
                case ActivePointerType.Mouse:
                    UpdateMouseDrag();
                    break;
                case ActivePointerType.Touch:
                    UpdateTouchDrag();
                    break;
                default:
                    EndDrag();
                    break;
            }
        }

        private void UpdateMouseDrag()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || mouse.leftButton.wasReleasedThisFrame || !mouse.leftButton.isPressed)
            {
                EndDrag();
                return;
            }

            UpdateDrag(mouse.position.ReadValue());
        }

        private void UpdateTouchDrag()
        {
            if (!TryGetTouchById(_activePointerId, out TouchControl touch))
            {
                EndDrag();
                return;
            }

            if (!touch.press.isPressed)
            {
                EndDrag();
                return;
            }

            UpdateDrag(touch.position.ReadValue());
        }

        private bool TryGetTouchById(int touchId, out TouchControl touchControl)
        {
            touchControl = default;

            if (Touchscreen.current == null)
            {
                return false;
            }

            var touches = Touchscreen.current.touches;
            for (int i = 0; i < touches.Count; i++)
            {
                TouchControl touch = touches[i];
                if (touch.touchId.ReadValue() != touchId)
                {
                    continue;
                }

                touchControl = touch;
                return true;
            }

            return false;
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
