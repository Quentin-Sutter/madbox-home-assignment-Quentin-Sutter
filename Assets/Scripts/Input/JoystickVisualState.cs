using UnityEngine;

namespace Madbox.Input
{
    /// <summary>
    /// Data needed by UI to render a floating joystick.
    /// </summary>
    public struct JoystickVisualState
    {
        public bool IsActive;
        public Vector2 BaseScreenPosition;
        public Vector2 Direction;
        public float Magnitude01;

        public JoystickVisualState(bool isActive, Vector2 baseScreenPosition, Vector2 direction, float magnitude01)
        {
            IsActive = isActive;
            BaseScreenPosition = baseScreenPosition;
            Direction = direction;
            Magnitude01 = Mathf.Clamp01(magnitude01);
        }

        public static JoystickVisualState Hidden => new(false, Vector2.zero, Vector2.zero, 0f);
    }
}
