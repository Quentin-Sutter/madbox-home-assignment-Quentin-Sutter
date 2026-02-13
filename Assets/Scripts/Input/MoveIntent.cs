using UnityEngine;

namespace Madbox.Input
{
    /// <summary>
    /// Lightweight movement intent consumed by gameplay systems.
    /// </summary>
    public struct MoveIntent
    {
        public Vector3 WorldDirection;
        public float Strength;
        public bool IsMoving;

        public MoveIntent(Vector3 worldDirection, float strength, bool isMoving)
        {
            WorldDirection = new Vector3(worldDirection.x, 0f, worldDirection.z);
            Strength = Mathf.Clamp01(strength);
            IsMoving = isMoving;
        }

        public static MoveIntent Idle => new(Vector3.zero, 0f, false);
    }
}
