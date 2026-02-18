using UnityEngine;
using Madbox.Character;

namespace Madbox.Movement
{
    /// <summary>
    /// Executes hero translation from externally provided movement intent.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HeroMovement : MonoBehaviour, ISpeedModifierReceiver
    {
        [SerializeField, Min(0f)] private float baseMoveSpeed = 5f;

        public bool IsMoving { get; private set; }

        private CharacterController _characterController;
        private bool _controllerWarningShown;
        private float _speedMultiplier = 1f;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            if (_characterController == null && !_controllerWarningShown)
            {
                Debug.LogWarning("HeroMovement: CharacterController missing. Falling back to transform movement.");
                _controllerWarningShown = true;
            }
        }

        public void Move(Vector3 worldDirection, float strength)
        {
            Vector3 planarDirection = new Vector3(worldDirection.x, 0f, worldDirection.z);
            float clampedStrength = Mathf.Clamp01(strength);

            if (planarDirection.sqrMagnitude > 0.0001f)
            {
                planarDirection.Normalize();
            }
            else
            {
                planarDirection = Vector3.zero;
                clampedStrength = 0f;
            }

            IsMoving = clampedStrength > 0f;
            Vector3 velocity = planarDirection * (baseMoveSpeed * _speedMultiplier * clampedStrength);
            Vector3 displacement = velocity * Time.deltaTime;

            if (_characterController != null)
            {
                _characterController.Move(displacement);
                return;
            }

            transform.position += displacement;
        }


        public void SetSpeedMultiplier(float multiplier)
        {
            _speedMultiplier = Mathf.Max(0f, multiplier);
        }

        public void Stop()
        {
            IsMoving = false;
        }
    }
}
