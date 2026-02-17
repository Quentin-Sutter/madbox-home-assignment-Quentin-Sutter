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
        private bool _wasSwitchCooldownActive;
        private bool _wasAttackCooldownActive;

        private bool IsSwitchCooldownActive => Time.time < _switchCooldownEndTime;
        private bool IsAttackCooldownActive => Time.time < _attackCooldownEndTime;

        private void Awake()
        {
            if (weaponService == null)
            {
                weaponService = FindObjectOfType<WeaponService>();
            }

            if (heroCombatService == null)
            {
                heroCombatService = FindObjectOfType<HeroCombatService>();
            }

            WireButtons();
        }

        private void OnEnable()
        {
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
        }

        private void Update()
        {
            bool isSwitchCooldownActive = IsSwitchCooldownActive;
            bool isAttackCooldownActive = IsAttackCooldownActive;
            if (isSwitchCooldownActive == _wasSwitchCooldownActive &&
                isAttackCooldownActive == _wasAttackCooldownActive)
            {
                return;
            }

            _wasSwitchCooldownActive = isSwitchCooldownActive;
            _wasAttackCooldownActive = isAttackCooldownActive;
            RefreshButtonsState();
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
                binding.button.onClick.RemoveAllListeners();
                binding.button.onClick.AddListener(() => OnWeaponClicked(weaponIndex));
            }
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
            _wasSwitchCooldownActive = IsSwitchCooldownActive;
            RefreshButtonsState();
        }

        private void HandleAttackCooldownStarted(float duration)
        {
            _attackCooldownWeaponIndex = _currentWeaponIndex;
            _attackCooldownEndTime = Time.time + Mathf.Max(0f, duration);
            StartCooldownForIndex(_currentWeaponIndex, duration);
            _wasAttackCooldownActive = IsAttackCooldownActive;
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
    }
}
