using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Madbox.Movement;

namespace Madbox.Character
{
    /// <summary>
    /// Loads weapon visuals once (Addressables), caches instances, and applies weapon gameplay modifiers.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponService : MonoBehaviour
    {
        [SerializeField] private WeaponData[] weapons;
        [SerializeField] private Transform weaponSocket;
        [SerializeField] private int defaultWeaponIndex;

        [Header("System References")]
        [SerializeField] private HeroMovement heroMovement;
        [SerializeField] private HeroTargetingService heroTargetingService;
        [SerializeField] private HeroCombatService heroCombatService;
        [SerializeField] private HeroStateController heroStateController;

        public WeaponData CurrentWeapon { get; private set; }
        public int CurrentWeaponIndex { get; private set; } = -1;

        public event Action<WeaponData, int> OnWeaponChanged;
        public event Action<float> OnSwitchCooldownStarted;

        private readonly Dictionary<WeaponData, GameObject> _visualInstancesByWeapon = new Dictionary<WeaponData, GameObject>();
        private readonly Dictionary<WeaponData, AsyncOperationHandle<GameObject>> _loadHandlesByWeapon = new Dictionary<WeaponData, AsyncOperationHandle<GameObject>>();
        private bool _weaponVisualVisible;
        private float _nextSwitchTime;

        private async void Start()
        {
            AutoAssignReferences();
            await PreloadVisualsAsync();

            if (weapons == null || weapons.Length == 0)
            {
                return;
            }

            int clampedDefaultIndex = Mathf.Clamp(defaultWeaponIndex, 0, weapons.Length - 1);
            EquipWeapon(clampedDefaultIndex);
            SyncWeaponVisualVisibilityWithState();
        }

        private void Update()
        {
            SyncWeaponVisualVisibilityWithState();
        }

        private void OnDestroy()
        {
            foreach (KeyValuePair<WeaponData, AsyncOperationHandle<GameObject>> handlePair in _loadHandlesByWeapon)
            {
                if (handlePair.Value.IsValid())
                {
                    Addressables.Release(handlePair.Value);
                }
            }

            _loadHandlesByWeapon.Clear();
            _visualInstancesByWeapon.Clear();
        }

        public bool RequestEquip(int index)
        {
            if (index == CurrentWeaponIndex)
            {
                return false;
            }

            if (Time.time < _nextSwitchTime)
            {
                return false;
            }

            if (!EquipWeapon(index))
            {
                return false;
            }

            float duration = Mathf.Max(0f, CurrentWeapon != null ? CurrentWeapon.SwitchCooldownSeconds : 0f);
            _nextSwitchTime = Time.time + duration;
            OnSwitchCooldownStarted?.Invoke(duration);
            return true;
        }

        public bool EquipWeapon(int index)
        {
            if (weapons == null || index < 0 || index >= weapons.Length)
            {
                return false;
            }

            return EquipWeapon(index, weapons[index]);
        }

        public bool EquipWeapon(WeaponData weapon)
        {
            if (weapon == null || weapons == null)
            {
                return false;
            }

            for (int i = 0; i < weapons.Length; i++)
            {
                if (weapons[i] == weapon)
                {
                    return EquipWeapon(i, weapon);
                }
            }

            Debug.LogWarning("WeaponService: Tried to equip weapon not present in configured list.", this);
            return false;
        }

        private bool EquipWeapon(int index, WeaponData weapon)
        {
            if (weapon == null)
            {
                Debug.LogWarning("WeaponService: Cannot equip a null weapon.", this);
                return false;
            }

            CurrentWeapon = weapon;
            CurrentWeaponIndex = index;
            ApplyWeaponGameplayModifiers(weapon);
            SetActiveVisualForWeapon(weapon);
            OnWeaponChanged?.Invoke(weapon, index);
            return true;
        }

        private async System.Threading.Tasks.Task PreloadVisualsAsync()
        {
            if (weapons == null || weapons.Length == 0)
            {
                return;
            }

            for (int i = 0; i < weapons.Length; i++)
            {
                WeaponData weapon = weapons[i];
                if (weapon == null || _visualInstancesByWeapon.ContainsKey(weapon))
                {
                    continue;
                }

                AssetReferenceGameObject visualReference = weapon.VisualPrefab;
                if (visualReference == null || !visualReference.RuntimeKeyIsValid())
                {
                    Debug.LogWarning($"WeaponService: Weapon '{weapon.name}' has no valid visual reference.", this);
                    continue;
                }

                AsyncOperationHandle<GameObject> loadHandle = visualReference.LoadAssetAsync<GameObject>();
                _loadHandlesByWeapon[weapon] = loadHandle;

                try
                {
                    GameObject prefab = await loadHandle.Task;
                    if (prefab == null)
                    {
                        Debug.LogError($"WeaponService: Loaded prefab is null for weapon '{weapon.name}'.", this);
                        continue;
                    }

                    Transform parent = weaponSocket != null ? weaponSocket : transform;
                    GameObject instance = Instantiate(prefab, parent);
                    instance.SetActive(false);
                    _visualInstancesByWeapon[weapon] = instance;
                }
                catch (Exception exception)
                {
                    Debug.LogError($"WeaponService: Failed to load weapon '{weapon.name}'. {exception.Message}", this);
                }
            }
        }

        private void ApplyWeaponGameplayModifiers(WeaponData weapon)
        {
            heroMovement?.SetSpeedMultiplier(weapon.MoveSpeedMultiplier);
            heroTargetingService?.SetRange(weapon.AttackRange);
            heroCombatService?.SetWeapon(weapon);
        }

        private void SetActiveVisualForWeapon(WeaponData activeWeapon)
        {
            foreach (KeyValuePair<WeaponData, GameObject> pair in _visualInstancesByWeapon)
            {
                if (pair.Value == null)
                {
                    continue;
                }
                pair.Value.SetActive(_weaponVisualVisible && pair.Key == activeWeapon);
            }
        }

        private void SyncWeaponVisualVisibilityWithState()
        {
            bool shouldShowVisual = heroStateController != null && heroStateController.CurrentState == HeroState.Attack;
            if (_weaponVisualVisible == shouldShowVisual)
            {
                return;
            }

            _weaponVisualVisible = shouldShowVisual;
            SetActiveVisualForWeapon(CurrentWeapon);
        }

        private void AutoAssignReferences()
        {
            if (heroMovement == null)
            {
                heroMovement = GetComponent<HeroMovement>();
            }

            if (heroTargetingService == null)
            {
                heroTargetingService = GetComponent<HeroTargetingService>();
            }

            if (heroCombatService == null)
            {
                heroCombatService = GetComponent<HeroCombatService>();
            }

            if (heroStateController == null)
            {
                heroStateController = GetComponent<HeroStateController>();
            }
        }
    }
}
