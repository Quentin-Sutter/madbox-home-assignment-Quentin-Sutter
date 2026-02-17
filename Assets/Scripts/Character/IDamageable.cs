namespace Madbox.Character
{
    /// <summary>
    /// Tiny damage contract so combat can hit anything, not only enemies.
    /// </summary>
    public interface IDamageable
    {
        void ApplyDamage(int amount);
    }
}
