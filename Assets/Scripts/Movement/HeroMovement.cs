using Madbox.Input;
using UnityEngine;

namespace Madbox.Movement
{
    /// <summary>
    /// Consumes MoveIntent and moves the hero independently from input detection/UI.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HeroMovement : MonoBehaviour
    {
        [SerializeField] private JoystickInput inputSource;
        [SerializeField, Min(0f)] private float baseMoveSpeed = 5f;
        [SerializeField] private bool rotateToMovement = true;
        [SerializeField, Min(0f)] private float rotationLerpSpeed = 12f;

        public bool IsMoving { get; private set; }

        private CharacterController _characterController;
        private bool _inputWarningShown;
        private bool _controllerWarningShown;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            if (_characterController == null && !_controllerWarningShown)
            {
                Debug.LogWarning("HeroMovement: CharacterController missing. Falling back to transform movement.");
                _controllerWarningShown = true;
            }
        }

        private void Update()
        {
            if (inputSource == null)
            {
                IsMoving = false;
                if (!_inputWarningShown)
                {
                    Debug.LogWarning("HeroMovement: inputSource is not assigned.");
                    _inputWarningShown = true;
                }

                return;
            }

            MoveIntent intent = inputSource.CurrentIntent;
            IsMoving = intent.IsMoving && intent.Strength > 0f && intent.WorldDirection.sqrMagnitude > 0f;

            Vector3 velocity = intent.WorldDirection * (baseMoveSpeed * intent.Strength);
            Vector3 displacement = velocity * Time.deltaTime;

            if (_characterController != null)
            {
                _characterController.Move(displacement);
            }
            else
            {
                transform.position += displacement;
            }

            if (rotateToMovement && IsMoving)
            {
                Quaternion targetRotation = Quaternion.LookRotation(intent.WorldDirection, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationLerpSpeed * Time.deltaTime);
            }
        }
    }
}
