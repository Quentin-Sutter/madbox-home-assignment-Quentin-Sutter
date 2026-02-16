using UnityEngine;

namespace Madbox.Hero
{
    /// <summary>
    /// Executes smooth horizontal rotation towards a direction or target.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HeroRotation : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float turnSpeed = 12f;

        public void FaceDirection(Vector3 direction)
        {
            Vector3 planarDirection = new Vector3(direction.x, 0f, direction.z);
            if (planarDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(planarDirection.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        public void FaceTarget(Transform target)
        {
            if (target == null)
            {
                return;
            }

            Vector3 toTarget = target.position - transform.position;
            FaceDirection(toTarget);
        }
    }
}
