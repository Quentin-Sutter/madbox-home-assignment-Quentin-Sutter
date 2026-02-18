using System;
using UnityEngine;
using UnityEngine.UI;
using Madbox.Character;

namespace Madbox.UI
{
    /// <summary>
    /// Wires weapon buttons to WeaponService and displays cooldowns from gameplay events.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SwitchWeaponUI : MonoBehaviour
    {
        [Serializable]
        private sealed class WeaponButtonBinding
        {
            public int weaponIndex;
            public Button button;
            public CooldownFillButton cooldownView;

            [NonSerialized] public ColorBlock defaultColors;
        }

        [SerializeField] private WeaponService weaponService;
        [SerializeField] private HeroCombatService heroCombatService;
        [SerializeField] private WeaponButtonBinding[] weaponButtons;
        [SerializeField] private Color selectedButtonColor = new Color(0.75f, 1f, 0.75f, 1f);

        private int _currentWeaponIndex = -1;
        private float _switchCooldownEndTime;
        private float _attackCooldownEndTime;
        private int _attackCooldownWeaponIndex = -1;
        private bool _missingReferencesWarningShown;

        private bool IsSwitchCooldownActive => Time.time < _switchCooldownEndTime;
        private bool IsAttackCooldownActive => Time.time < _attackCooldownEndTime;

        private void Awake()
        {
            AutoWireSerializedReferences();

            WireButtons();
            WarnIfReferencesMissing();
        } 

        private void OnEnable()
        {
            WarnIfReferencesMissing();

            if (weaponService != null)
            {
                weaponService.OnSwitchCooldownStarted += HandleSwitchCooldownStarted;
                weaponService.OnWeaponChanged += HandleWeaponChanged;
                _currentWeaponIndex = weaponService.CurrentWeaponIndex;
            }

            if (heroCombatService != null)
            {
                heroCombatService.OnAttackCooldownStarted += HandleAttackCooldownStarted;
            }

            SubscribeCooldownCompletionEvents();

            RefreshButtonsState();
        }

        private void OnDisable()
        {
            if (weaponService != null)
            {
                weaponService.OnSwitchCooldownStarted -= HandleSwitchCooldownStarted;
                weaponService.OnWeaponChanged -= HandleWeaponChanged;
            }

            if (heroCombatService != null)
            {
                heroCombatService.OnAttackCooldownStarted -= HandleAttackCooldownStarted;
            }

            UnsubscribeCooldownCompletionEvents();
        }

        private void WireButtons()
        {
            if (weaponButtons == null)
            {
                return;
            }

            for (int i = 0; i < weaponButtons.Length; i++)
            {
                WeaponButtonBinding binding = weaponButtons[i];
                if (binding == null || binding.button == null)
                {
                    continue;
                }

                binding.defaultColors = binding.button.colors;
                int weaponIndex = binding.weaponIndex;
                //binding.button.onClick.RemoveAllListeners();
                binding.button.onClick.AddListener(() => OnWeaponClicked(weaponIndex));
            }
        }

        private void AutoWireSerializedReferences()
        {
            weaponService = weaponService != null ? weaponService : GetComponent<WeaponService>();
            heroCombatService = heroCombatService != null ? heroCombatService : GetComponent<HeroCombatService>();

            if (weaponButtons != null && weaponButtons.Length > 0)
            {
                return;
            }

            Button[] childButtons = GetComponentsInChildren<Button>(true);
            if (childButtons == null || childButtons.Length == 0)
            {
                return;
            }

            weaponButtons = new WeaponButtonBinding[childButtons.Length];
            for (int i = 0; i < childButtons.Length; i++)
            {
                Button button = childButtons[i];
                weaponButtons[i] = new WeaponButtonBinding
                {
                    weaponIndex = i,
                    button = button,
                    cooldownView = button != null ? button.GetComponent<CooldownFillButton>() : null
                };
            }
        }

        private void WarnIfReferencesMissing()
        {
            if (_missingReferencesWarningShown)
            {
                return;
            }

            if (weaponService != null && heroCombatService != null)
            {
                return;
            }

            _missingReferencesWarningShown = true;
            Debug.LogWarning(
                $"{nameof(SwitchWeaponUI)} on '{name}' is missing serialized dependencies " +
                $"(weaponService: {(weaponService == null ? "missing" : "ok")}, " +
                $"heroCombatService: {(heroCombatService == null ? "missing" : "ok")}). " +
                "Assign both references in the inspector. UI binding will be skipped for missing dependencies.",
                this);
        }

        private void OnWeaponClicked(int weaponIndex)
        {
            weaponService?.RequestEquip(weaponIndex);
        }

        private void HandleWeaponChanged(WeaponData weapon, int index)
        {
            _currentWeaponIndex = index;
            RefreshButtonsState();
        }

        private void HandleSwitchCooldownStarted(float duration)
        {
            _switchCooldownEndTime = Time.time + Mathf.Max(0f, duration);
            StartCooldownForIndex(_currentWeaponIndex, duration);
            RefreshButtonsState();
        }

        private void HandleAttackCooldownStarted(float duration)
        {
            _attackCooldownWeaponIndex = _currentWeaponIndex;
            _attackCooldownEndTime = Time.time + Mathf.Max(0f, duration);
            StartCooldownForIndex(_currentWeaponIndex, duration);
            RefreshButtonsState();
        }

        private void HandleCooldownCompleted(CooldownFillButton cooldown)
        {
            if (cooldown == null)
            {
                return;
            }

            int weaponIndex = GetWeaponIndexByCooldown(cooldown);
            if (weaponIndex < 0)
            {
                return;
            }

            if (_attackCooldownWeaponIndex == weaponIndex)
            {
                _attackCooldownEndTime = 0f;
                _attackCooldownWeaponIndex = -1;
            }

            if (weaponIndex == _currentWeaponIndex)
            {
                _switchCooldownEndTime = 0f;
            }

            RefreshButtonsState();
        }

        private void StartCooldownForIndex(int weaponIndex, float duration)
        {
            CooldownFillButton cooldownView = GetCooldownView(weaponIndex);
            if (cooldownView == null)
            {
                return;
            }

            cooldownView.StartCooldown(duration);
        }

        private void RefreshButtonsState()
        {
            if (weaponButtons == null)
            {
                return;
            }

            bool switchCooldownActive = IsSwitchCooldownActive;
            bool attackCooldownActive = IsAttackCooldownActive;

            for (int i = 0; i < weaponButtons.Length; i++)
            {
                WeaponButtonBinding binding = weaponButtons[i];
                if (binding == null || binding.button == null)
                {
                    continue;
                }

                bool isSelected = binding.weaponIndex == _currentWeaponIndex;
                bool isOnAttackCooldown = attackCooldownActive && binding.weaponIndex == _attackCooldownWeaponIndex;
                bool isInteractable = !isSelected && !switchCooldownActive && !isOnAttackCooldown;

                binding.button.interactable = isInteractable;
                ApplySelectionVisual(binding, isSelected);
            }
        }

        private void ApplySelectionVisual(WeaponButtonBinding binding, bool isSelected)
        {
            ColorBlock colors = binding.defaultColors;
            if (isSelected)
            {
                colors.normalColor = selectedButtonColor;
                colors.highlightedColor = selectedButtonColor;
                colors.selectedColor = selectedButtonColor;
            }

            binding.button.colors = colors;
        }

        private CooldownFillButton GetCooldownView(int weaponIndex)
        {
            if (weaponButtons == null)
            {
                return null;
            }

            for (int i = 0; i < weaponButtons.Length; i++)
            {
                WeaponButtonBinding binding = weaponButtons[i];
                if (binding != null && binding.weaponIndex == weaponIndex)
                {
                    return binding.cooldownView;
                }
            }

            return null;
        }

        private int GetWeaponIndexByCooldown(CooldownFillButton cooldown)
        {
            if (weaponButtons == null)
            {
                return -1;
            }

            for (int i = 0; i < weaponButtons.Length; i++)
            {
                WeaponButtonBinding binding = weaponButtons[i];
                if (binding != null && binding.cooldownView == cooldown)
                {
                    return binding.weaponIndex;
                }
            }

            return -1;
        }

        private void SubscribeCooldownCompletionEvents()
        {
            if (weaponButtons == null)
            {
                return;
            }

            for (int i = 0; i < weaponButtons.Length; i++)
            {
                CooldownFillButton cooldown = weaponButtons[i]?.cooldownView;
                if (cooldown == null)
                {
                    continue;
                }

                cooldown.CooldownCompleted -= HandleCooldownCompleted;
                cooldown.CooldownCompleted += HandleCooldownCompleted;
            }
        }

        private void UnsubscribeCooldownCompletionEvents()
        {
            if (weaponButtons == null)
            {
                return;
            }

            for (int i = 0; i < weaponButtons.Length; i++)
            {
                CooldownFillButton cooldown = weaponButtons[i]?.cooldownView;
                if (cooldown == null)
                {
                    continue;
                }

                cooldown.CooldownCompleted -= HandleCooldownCompleted;
            }
        }
    }
}
