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
        public Vector2 KnobScreenOffset;

        public JoystickVisualState(bool isActive, Vector2 baseScreenPosition, Vector2 knobScreenOffset)
        {
            IsActive = isActive;
            BaseScreenPosition = baseScreenPosition;
            KnobScreenOffset = knobScreenOffset;
        }

        public static JoystickVisualState Hidden => new(false, Vector2.zero, Vector2.zero);
    }
}
