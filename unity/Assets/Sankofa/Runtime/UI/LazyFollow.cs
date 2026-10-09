using UnityEngine;

namespace Sankofa.Runtime.UI
{
    /// <summary>
    /// Keeps a board in view without pinning it to the head: it stays where it is while the visitor looks within
    /// <see cref="slack"/> degrees of it, and glides round the seat to the edge of that range when they look further
    /// away, at the same distance. It follows a glance down to the hands as well as a turn to the side, within limits,
    /// and only slowly, so nothing moves with every glance.
    /// </summary>
    public sealed class LazyFollow : MonoBehaviour
    {
        [SerializeField] Transform head;
        [Tooltip("The seated eye, in the parent's space; the board turns round it.")]
        [SerializeField] Vector3 pivot = new Vector3(0, 1.15f, 0);
        [SerializeField] float slack = 25f;
        [SerializeField] float verticalSlack = 18f;
        [Tooltip("How far below and above level the board may go, in degrees.")]
        [SerializeField] Vector2 elevationRange = new Vector2(-38f, 16f);
        [SerializeField] float degreesPerSecond = 45f;

        float distance, bearing, elevation;

        void Start()
        {
            var offset = transform.localPosition - pivot;
            distance = offset.magnitude;
            bearing = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
            elevation = Mathf.Asin(Mathf.Clamp(offset.y / Mathf.Max(distance, 1e-3f), -1f, 1f)) * Mathf.Rad2Deg;
        }

        void LateUpdate()
        {
            if (head == null || transform.parent == null || distance < 1e-3f) return;
            var look = transform.parent.InverseTransformDirection(head.forward);
            var flat = new Vector2(look.x, look.z).magnitude;
            var moved = false;
            if (flat > 1e-2f)
            {
                var gaze = Mathf.Atan2(look.x, look.z) * Mathf.Rad2Deg;
                var off = Mathf.DeltaAngle(gaze, bearing);
                if (Mathf.Abs(off) > slack)
                {
                    bearing = Mathf.MoveTowardsAngle(bearing, gaze + Mathf.Sign(off) * slack, degreesPerSecond * Time.unscaledDeltaTime);
                    moved = true;
                }
            }
            var pitch = Mathf.Atan2(look.y, flat) * Mathf.Rad2Deg;
            var vertical = elevation - pitch;
            if (Mathf.Abs(vertical) > verticalSlack)
            {
                var target = Mathf.Clamp(pitch + Mathf.Sign(vertical) * verticalSlack, elevationRange.x, elevationRange.y);
                var next = Mathf.MoveTowards(elevation, target, degreesPerSecond * Time.unscaledDeltaTime);
                moved |= !Mathf.Approximately(next, elevation);
                elevation = next;
            }
            if (!moved) return;
            var b = bearing * Mathf.Deg2Rad;
            var e = elevation * Mathf.Deg2Rad;
            var direction = new Vector3(Mathf.Sin(b) * Mathf.Cos(e), Mathf.Sin(e), Mathf.Cos(b) * Mathf.Cos(e));
            transform.localPosition = pivot + direction * distance;
            transform.localRotation = Quaternion.LookRotation(direction, Vector3.up);
        }
    }
}
