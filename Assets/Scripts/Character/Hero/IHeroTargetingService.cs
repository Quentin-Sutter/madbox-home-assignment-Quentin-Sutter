using UnityEngine;

namespace Madbox.Character
{
    public interface IHeroTargetingService
    {
        Transform GetCurrentTarget();
        bool HasValidTarget();
        void BreakLock();
        void RefreshTarget();
    }
}
