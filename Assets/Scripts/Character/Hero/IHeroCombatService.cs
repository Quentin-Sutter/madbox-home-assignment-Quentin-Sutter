using UnityEngine;

namespace Madbox.Character
{
    public interface IHeroCombatService
    {
        bool TryStartAttack(Transform target);
        void CancelAttack();
        Transform CurrentLockedTarget { get; }
        bool IsAttackInProgress { get; }
    }
}
