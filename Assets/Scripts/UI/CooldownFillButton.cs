using UnityEngine;
using UnityEngine.UI;

namespace Madbox.UI
{
    /// <summary>
    /// Generic UI cooldown view. Disables a button and animates a fill image until ready.
    /// </summary>
    public sealed class CooldownFillButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image fillImage;

        private float _cooldownEndTime;
        private float _cooldownDuration;
        private bool _isCoolingDown;

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            SetReadyState();
        }

        private void Update()
        {
            if (!_isCoolingDown)
            {
                return;
            }

            float remaining = _cooldownEndTime - Time.time;
            if (remaining <= 0f)
            {
                SetReadyState();
                return;
            }

            if (fillImage != null)
            {
                // 1 = full cooldown remaining, 0 = ready.
                fillImage.fillAmount = Mathf.Clamp01(remaining / _cooldownDuration);
            }
        }

        public void StartCooldown(float durationSeconds)
        {
            if (durationSeconds <= 0f)
            {
                SetReadyState();
                return;
            }

            _cooldownDuration = durationSeconds;
            _cooldownEndTime = Time.time + durationSeconds;
            _isCoolingDown = true;

            if (button != null)
            {
                button.interactable = false;
            }

            if (fillImage != null)
            {
                fillImage.fillAmount = 1f;
            }
        }

        private void SetReadyState()
        {
            _isCoolingDown = false;
            _cooldownDuration = 0f;
            _cooldownEndTime = 0f;

            if (button != null)
            {
                button.interactable = true;
            }

            if (fillImage != null)
            {
                fillImage.fillAmount = 0f;
            }
        }
    }
}
