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

        public event Action<WeaponData> OnWeaponChanged;

        private readonly Dictionary<WeaponData, GameObject> _visualInstancesByWeapon = new Dictionary<WeaponData, GameObject>();
        private readonly Dictionary<WeaponData, AsyncOperationHandle<GameObject>> _loadHandlesByWeapon = new Dictionary<WeaponData, AsyncOperationHandle<GameObject>>();
        private bool _weaponVisualVisible;


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

        int weaponNumber = 0;
        float weaponTimer = 2.0f;
        private void Update()
        {
            weaponTimer -= Time.deltaTime;

            if (weaponTimer <= 0)
            { 
                EquipWeapon(weaponNumber++);
                weaponTimer = 2.0f;
                if (weaponNumber == 3) weaponNumber = -1;
            }

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

        public bool EquipWeapon(int index)
        {
            if (weapons == null || index < 0 || index >= weapons.Length)
            {
                return false;
            }

            return EquipWeapon(weapons[index]);
        }

        public bool EquipWeapon(WeaponData weapon)
        {
            if (weapon == null)
            {
                Debug.LogWarning("WeaponService: Cannot equip a null weapon.", this);
                return false;
            }

            CurrentWeapon = weapon;
            ApplyWeaponGameplayModifiers(weapon);
            SetActiveVisualForWeapon(weapon);
            OnWeaponChanged?.Invoke(weapon);
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
