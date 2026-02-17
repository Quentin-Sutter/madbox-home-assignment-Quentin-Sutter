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
        }

        [SerializeField] private WeaponService weaponService;
        [SerializeField] private HeroCombatService heroCombatService;
        [SerializeField] private WeaponButtonBinding[] weaponButtons;

        private int _currentWeaponIndex;

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
            }

            if (heroCombatService != null)
            {
                heroCombatService.OnAttackCooldownStarted += HandleAttackCooldownStarted;
            }
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
        }

        private void HandleSwitchCooldownStarted(float duration)
        {
            StartCooldownForIndex(_currentWeaponIndex, duration);
        }

        private void HandleAttackCooldownStarted(float duration)
        {
            StartCooldownForIndex(_currentWeaponIndex, duration);
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
