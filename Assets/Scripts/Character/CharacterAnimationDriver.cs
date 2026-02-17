using UnityEngine;

namespace Madbox.Character
{
    /// <summary>
    /// Generic adapter between gameplay events and Animator parameters.
    /// Works for both hero and enemies that follow the same animation contract.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterAnimationDriver : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private AnimationClip attackClip;

        [Header("Parameter Names")]
        [SerializeField] private string moveSpeedParam = "MoveSpeed";
        [SerializeField] private string attackTriggerParam = "Attack";
        [SerializeField] private string damageTriggerParam = "Damage";
        [SerializeField] private string dieTriggerParam = "Die";
        [SerializeField] private string attackSpeedMultiplierParam = "AttackSpeedMultiplier";

        private int _moveSpeedHash;
        private int _attackTriggerHash;
        private int _damageTriggerHash;
        private int _dieTriggerHash;
        private int _attackSpeedMultiplierHash;

        private bool _hasMoveSpeedParam;
        private bool _hasAttackTriggerParam;
        private bool _hasDamageTriggerParam;
        private bool _hasDieTriggerParam;
        private bool _hasAttackSpeedMultiplierParam;
        private bool _hasDieTriggered;
        private float _baseAttackLengthSeconds = 1f;

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
            }

            if (animator == null)
            {
                Debug.LogWarning("CharacterAnimationDriver: Animator is not assigned and no Animator was found on this GameObject.", this);
                return;
            }

            if (attackClip == null)
            {
                Debug.LogWarning("CharacterAnimationDriver: Attack clip is not assigned. Falling back to a 1 second base attack length.", this);
            }
            else
            {
                _baseAttackLengthSeconds = Mathf.Max(0.01f, attackClip.length);
            }

            CacheHashes();
            CacheParameterAvailability();
        }

        public void SetMoveAmount(float normalized01)
        {
            if (!_hasMoveSpeedParam)
            {
                return;
            }

            animator.SetFloat(_moveSpeedHash, Mathf.Clamp01(normalized01));
        }

        public float ComputeAttackSpeedForCooldown(float cooldownSeconds)
        {
            if (cooldownSeconds <= 0f)
            {
                return 1f;
            }

            return Mathf.Max(0.01f, _baseAttackLengthSeconds / cooldownSeconds);
        }

        public void TriggerAttack(float attackSpeed = 1f)
        {
            if (_hasDieTriggered)
            {
                return;
            }

            SetAttackSpeedMultiplier(attackSpeed);

            if (_hasAttackTriggerParam)
            {
                animator.SetTrigger(_attackTriggerHash);
            }
        }

        public void SetAttackSpeedMultiplier(float multiplier)
        {
            if (!_hasAttackSpeedMultiplierParam)
            {
                return;
            }

            animator.SetFloat(_attackSpeedMultiplierHash, Mathf.Max(0f, multiplier));
        }

        public void TriggerDamage()
        {
            if (_hasDieTriggered || !_hasDamageTriggerParam)
            {
                return;
            }

            animator.SetTrigger(_damageTriggerHash);
        }

        public void ResetToIdle()
        {
            if (animator == null)
            {
                return;
            }

            _hasDieTriggered = false;

            if (_hasMoveSpeedParam)
            {
                animator.SetFloat(_moveSpeedHash, 0f);
            }

            if (_hasAttackSpeedMultiplierParam)
            {
                animator.SetFloat(_attackSpeedMultiplierHash, 1f);
            }

            if (_hasAttackTriggerParam)
            {
                animator.ResetTrigger(_attackTriggerHash);
            }

            if (_hasDamageTriggerParam)
            {
                animator.ResetTrigger(_damageTriggerHash);
            }

            if (_hasDieTriggerParam)
            {
                animator.ResetTrigger(_dieTriggerHash);
            }

            if (!animator.isActiveAndEnabled)
            {
                return;
            }

            animator.Rebind();
            animator.Update(0f);
        }

        public void TriggerDie()
        {
            if (_hasDieTriggered)
            {
                return;
            }

            _hasDieTriggered = true;

            if (_hasDieTriggerParam)
            {
                animator.SetTrigger(_dieTriggerHash);
            }
        }

        private void CacheHashes()
        {
            _moveSpeedHash = Animator.StringToHash(moveSpeedParam);
            _attackTriggerHash = Animator.StringToHash(attackTriggerParam);
            _damageTriggerHash = Animator.StringToHash(damageTriggerParam);
            _dieTriggerHash = Animator.StringToHash(dieTriggerParam);
            _attackSpeedMultiplierHash = Animator.StringToHash(attackSpeedMultiplierParam);
        }

        private void CacheParameterAvailability()
        {
            _hasMoveSpeedParam = HasParameter(moveSpeedParam, AnimatorControllerParameterType.Float);
            _hasAttackTriggerParam = HasParameter(attackTriggerParam, AnimatorControllerParameterType.Trigger);
            _hasDamageTriggerParam = HasParameter(damageTriggerParam, AnimatorControllerParameterType.Trigger);
            _hasDieTriggerParam = HasParameter(dieTriggerParam, AnimatorControllerParameterType.Trigger);
            _hasAttackSpeedMultiplierParam = HasParameter(attackSpeedMultiplierParam, AnimatorControllerParameterType.Float);
        }

        private bool HasParameter(string parameterName, AnimatorControllerParameterType expectedType)
        {
            if (string.IsNullOrWhiteSpace(parameterName))
            {
                return false;
            }

            for (int i = 0; i < animator.parameterCount; i++)
            {
                AnimatorControllerParameter parameter = animator.GetParameter(i);
                if (parameter.type == expectedType && parameter.name == parameterName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
