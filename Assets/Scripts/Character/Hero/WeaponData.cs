using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Madbox.Character
{
    /// <summary>
    /// Minimal weapon definition: visual reference + gameplay modifiers.
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponData", menuName = "Madbox/Hero/Weapon Data")]
    public sealed class WeaponData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string weaponId = "weapon";
        [SerializeField] private string displayName = "Weapon";

        [Header("Visual")]
        [SerializeField] private AssetReferenceGameObject visualPrefab;

        [Header("Gameplay")]
        [SerializeField, Min(0f)] private float switchCooldownSeconds = 0.2f;
        [SerializeField, Min(0.05f)] private float attackCooldownSeconds = 0.75f;
        [SerializeField, Min(0f)] private float ultimateCooldownSeconds = 3f;
        [SerializeField, Min(0.1f)] private float moveSpeedMultiplier = 1f;
        [SerializeField, Min(0.1f)] private float attackRange = 3f;
        [SerializeField, Min(0f)] private float damageDelaySeconds = 0.1f;

        public string WeaponId => weaponId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? weaponId : displayName;
        public AssetReferenceGameObject VisualPrefab => visualPrefab;
        public float SwitchCooldownSeconds => switchCooldownSeconds;
        public float AttackCooldownSeconds => attackCooldownSeconds;
        public float UltimateCooldownSeconds => ultimateCooldownSeconds;
        public float MoveSpeedMultiplier => moveSpeedMultiplier;
        public float AttackRange => attackRange;
        public float DamageDelaySeconds => damageDelaySeconds;
    }
}
