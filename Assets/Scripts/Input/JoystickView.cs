using UnityEngine;

namespace Madbox.Input
{
    /// <summary>
    /// Pure UI renderer for floating joystick visuals.
    /// Reads visual state from JoystickInput and updates RectTransforms.
    /// </summary>
    public sealed class JoystickView : MonoBehaviour
    {
        [SerializeField] private JoystickInput inputSource;
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform targetRect;
        [SerializeField] private RectTransform baseRect;
        [SerializeField] private RectTransform knobRect;

        private bool _missingRefsWarningShown;

        private void Awake()
        {
            if (canvas == null)
            {
                canvas = GetComponentInParent<Canvas>();
            }

            if (targetRect == null)
            {
                targetRect = canvas != null ? canvas.transform as RectTransform : null;
            }

            SetVisible(false);
        }

        private void LateUpdate()
        {
            if (inputSource == null || baseRect == null || knobRect == null || targetRect == null)
            {
                if (!_missingRefsWarningShown)
                {
                    Debug.LogWarning("JoystickView: Missing references. Assign inputSource, canvas/targetRect, baseRect and knobRect.");
                    _missingRefsWarningShown = true;
                }

                return;
            }

            var state = inputSource.CurrentVisualState;
            if (!state.IsActive)
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);

            Vector2 knobScreenPos = state.BaseScreenPosition + state.KnobScreenOffset;
            Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                targetRect,
                state.BaseScreenPosition,
                eventCamera,
                out Vector2 baseLocalPos);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                targetRect,
                knobScreenPos,
                eventCamera,
                out Vector2 knobLocalPos);

            baseRect.anchoredPosition = baseLocalPos;
            knobRect.anchoredPosition = knobLocalPos;
        }

        private void SetVisible(bool visible)
        {
            if (baseRect != null)
            {
                baseRect.gameObject.SetActive(visible);
            }

            if (knobRect != null)
            {
                knobRect.gameObject.SetActive(visible);
            }
        }
    }
}
