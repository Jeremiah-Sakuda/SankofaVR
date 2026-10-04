using System;
using UnityEngine;

namespace Sankofa.Runtime.App
{
    /// <summary>
    /// Places the hearth in front of wherever the visitor sits: on the floor below their head, facing the way they
    /// face. Placed once head tracking settles, and again on recentre. The camera itself is never moved.
    /// </summary>
    public sealed class WorldAnchor : MonoBehaviour
    {
        [SerializeField] Transform head;
        [SerializeField] float settleSeconds = 0.4f;

        float settled;

        public bool Placed { get; private set; }
        public event Action PlacedEvent;

        void OnEnable()
        {
            if (OVRManager.display != null) OVRManager.display.RecenteredPose += Place;
        }

        void OnDisable()
        {
            if (OVRManager.display != null) OVRManager.display.RecenteredPose -= Place;
        }

        void Update()
        {
            if (Placed) return;
            // An untracked head stays exactly at the rig origin; wait for a real pose before placing the world.
            var tracked = head.localPosition.sqrMagnitude > 1e-6f || head.localRotation != Quaternion.identity;
            settled = tracked ? settled + Time.unscaledDeltaTime : 0;
            if (settled >= settleSeconds || Time.realtimeSinceStartup > 5f) Place();
        }

        public void Place()
        {
            var forward = head.forward;
            forward.y = 0;
            if (forward.sqrMagnitude < 1e-4f) forward = Vector3.forward;
            var p = head.position;
            transform.SetPositionAndRotation(new Vector3(p.x, 0, p.z), Quaternion.LookRotation(forward.normalized, Vector3.up));
            var first = !Placed;
            Placed = true;
            if (first) PlacedEvent?.Invoke();
        }
    }
}
