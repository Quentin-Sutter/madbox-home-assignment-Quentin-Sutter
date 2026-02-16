using UnityEngine;
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

        private void Awake()
        {
            if (gameplayCamera == null)
            {
                gameplayCamera = Camera.main;
            }
        }

        private void Update()
        {
            var pointer = Pointer.current;
            if (pointer == null)
            {
                EndDrag();
                return;
            }

            var pressControl = pointer.press;
            Vector2 pointerPos = pointer.position.ReadValue();

            if (pressControl.wasPressedThisFrame)
            {
                BeginDrag(pointerPos);
            }

            if (pressControl.isPressed && _isDragging)
            {
                UpdateDrag(pointerPos);
            }

            if (pressControl.wasReleasedThisFrame)
            {
                EndDrag();
            }
        }

        private void BeginDrag(Vector2 pressScreenPos)
        {
            _isDragging = true;
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
