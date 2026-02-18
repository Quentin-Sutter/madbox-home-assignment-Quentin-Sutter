namespace Madbox.Character
{
    /// <summary>
    /// Contract for pooled objects that need to restore runtime state when re-activated.
    /// </summary>
    public interface IPoolResettable
    {
        void ResetForPoolSpawn();
    }
}
