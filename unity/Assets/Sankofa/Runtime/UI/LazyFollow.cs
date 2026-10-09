using UnityEngine;

namespace Sankofa.Runtime.UI
{
    /// <summary>
    /// Keeps a board in view without pinning it to the head: it stays where it is while the visitor looks within
    /// <see cref="slack"/> degrees of it, and glides round the seat to the edge of that range when they look further
    /// away, at the same distance and height. Only its bearing changes, slowly, so nothing moves with every glance.
    /// </summary>
    public sealed class LazyFollow : MonoBehaviour
    {
        [SerializeField] Transform head;
        [Tooltip("The seated eye, in the parent's space; the board turns round it.")]
        [SerializeField] Vector3 pivot = new Vector3(0, 1.15f, 0);
        [SerializeField] float slack = 25f;
        [SerializeField] float degreesPerSecond = 45f;

        float distance, height, bearing;

        void Start()
        {
            var offset = transform.localPosition - pivot;
            distance = new Vector2(offset.x, offset.z).magnitude;
            height = offset.y;
            bearing = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
        }

        void LateUpdate()
        {
            if (head == null || transform.parent == null || distance < 1e-3f) return;
            var look = transform.parent.InverseTransformDirection(head.forward);
            if (new Vector2(look.x, look.z).sqrMagnitude < 1e-4f) return;
            var gaze = Mathf.Atan2(look.x, look.z) * Mathf.Rad2Deg;
            var off = Mathf.DeltaAngle(gaze, bearing);
            if (Mathf.Abs(off) <= slack) return;
            var target = gaze + Mathf.Sign(off) * slack;
            bearing = Mathf.MoveTowardsAngle(bearing, target, degreesPerSecond * Time.unscaledDeltaTime);
            var radians = bearing * Mathf.Deg2Rad;
            var position = pivot + new Vector3(Mathf.Sin(radians) * distance, height, Mathf.Cos(radians) * distance);
            transform.localPosition = position;
            transform.localRotation = Quaternion.LookRotation(position - pivot, Vector3.up);
        }
    }
}
