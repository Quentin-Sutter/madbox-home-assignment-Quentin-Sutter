using UnityEngine;

namespace Madbox.Hero
{
    /// <summary>
    /// View-only animation bridge for hero locomotion and reactive triggers.
    /// Required Animator parameters by default:
    /// - Float: MoveSpeed (0..1)
    /// - Trigger: Damage
    /// - Trigger: Die
    /// Attack trigger remains owned by HeroCombatService (default "Attack").
    /// For hit-frame timing, add an animation event in the attack clip that calls
    /// HeroCombatService.AnimationEvent_DealDamage().
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HeroAnimationDriver : MonoBehaviour
    {
        [SerializeField] private Animator animator;

        [Header("Parameter Names")]
        [SerializeField] private string moveSpeedParam = "MoveSpeed";
        [SerializeField] private string damageTriggerParam = "Damage";
        [SerializeField] private string dieTriggerParam = "Die";
        [SerializeField] private string attackSpeedMultiplierParam = "AttackSpeedMultiplier";

        private int _moveSpeedHash;
        private int _damageTriggerHash;
        private int _dieTriggerHash;
        private int _attackSpeedMultiplierHash;
        private bool _hasDieTriggered;

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            CacheHashes();

            if (animator == null)
            {
                Debug.LogWarning("HeroAnimationDriver: Animator is not assigned and no Animator was found on this GameObject.", this);
            }
        }

        public void SetMoveAmount(float normalized01)
        {
            if (animator == null)
            {
                return;
            }

            animator.SetFloat(_moveSpeedHash, Mathf.Clamp01(normalized01));
        }

        public void TriggerDamage()
        {
            if (animator == null || _hasDieTriggered)
            {
                return;
            }

            animator.SetTrigger(_damageTriggerHash);
        }

        public void TriggerDie()
        {
            if (animator == null || _hasDieTriggered)
            {
                return;
            }

            _hasDieTriggered = true;
            animator.SetTrigger(_dieTriggerHash);
        }

        public void SetAttackSpeedMultiplier(float multiplier)
        {
            if (animator == null)
            {
                return;
            }

            animator.SetFloat(_attackSpeedMultiplierHash, Mathf.Max(0f, multiplier));
        }

        private void CacheHashes()
        {
            _moveSpeedHash = Animator.StringToHash(moveSpeedParam);
            _damageTriggerHash = Animator.StringToHash(damageTriggerParam);
            _dieTriggerHash = Animator.StringToHash(dieTriggerParam);
            _attackSpeedMultiplierHash = Animator.StringToHash(attackSpeedMultiplierParam);
        }
    }
}
