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

            Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    targetRect,
                    state.BaseScreenPosition,
                    eventCamera,
                    out Vector2 baseLocalPos))
            {
                return;
            }

            // Keep all UI movement in local canvas units so this works with any resolution
            // and with Canvas Scaler (Scale With Screen Size).
            float baseRadius = Mathf.Min(baseRect.rect.width, baseRect.rect.height) * 0.5f;
            float knobRadius = Mathf.Min(knobRect.rect.width, knobRect.rect.height) * 0.5f;
            float visualRadius = Mathf.Max(0f, baseRadius - knobRadius);

            Vector2 knobOffset = state.Direction * (visualRadius * state.Magnitude01);

            baseRect.anchoredPosition = baseLocalPos;
            knobRect.anchoredPosition = baseLocalPos + knobOffset;
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
