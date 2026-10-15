using System;
using UnityEngine;

namespace Sankofa.Runtime.App
{
    /// <summary>
    /// Places the virtual stage around the seated eye, facing the way the visitor faces. Full VR uses a virtual
    /// floor: normalising eye height keeps touch controls/gifts within the designed reach for different bodies.
    /// Placed once tracking settles and again on recenter. The camera itself is never moved.
    /// </summary>
    public sealed class WorldAnchor : MonoBehaviour
    {
        [SerializeField] Transform head;
        [SerializeField] float settleSeconds = 0.4f;
        [SerializeField] float designedEyeHeight = 1.15f;

        float settled;
        float enabledAt, seatedBaseHeight;
        bool awaitingFirstTrackedPosition;

        public bool Placed { get; private set; }

        float heightOffset;
        float reachOffset;
        public float ReachOffset
        {
            get => reachOffset;
            set
            {
                if (Placed) transform.position += transform.forward * (value - reachOffset);
                reachOffset = value;
            }
        }

        /// <summary>Raises or lowers the whole world for the visitor's height and reach, keeping where it faces.</summary>
        public float HeightOffset
        {
            get => heightOffset;
            set
            {
                heightOffset = value;
                if (!Placed) return;
                var p = transform.position;
                transform.position = new Vector3(p.x, seatedBaseHeight + heightOffset, p.z);
            }
        }
        public event Action PlacedEvent;

        void OnEnable()
        {
            enabledAt = Time.realtimeSinceStartup;
            if (OVRManager.display != null) OVRManager.display.RecenteredPose += Place;
        }

        void OnDisable()
        {
            if (OVRManager.display != null) OVRManager.display.RecenteredPose -= Place;
        }

        void Update()
        {
            if (Placed)
            {
                if (!awaitingFirstTrackedPosition) return;
                // The editor/simulator may initialise OpenXR after the fallback timeout. Correct that initial
                // zero-pose placement once a seated head arrives; subsequent head motion never moves the world.
                settled = head.position.y > 0.3f ? settled + Time.unscaledDeltaTime : 0;
                if (settled >= settleSeconds) Place();
                return;
            }
            // An untracked head stays exactly at the rig origin; wait for a real pose before placing the world.
            var tracked = head.localPosition.sqrMagnitude > 1e-6f || head.localRotation != Quaternion.identity;
            settled = tracked ? settled + Time.unscaledDeltaTime : 0;
            if (settled >= settleSeconds || Time.realtimeSinceStartup - enabledAt > 5f) Place();
        }

        public void Place()
        {
            var forward = head.forward;
            forward.y = 0;
            if (forward.sqrMagnitude < 1e-4f) forward = Vector3.forward;
            var p = head.position;
            awaitingFirstTrackedPosition = p.y <= 0.3f;
            if (awaitingFirstTrackedPosition) settled = 0;
            // Zero-height untracked/editor poses retain the comparison floor until an actual pose is supplied.
            seatedBaseHeight = p.y > 0.3f ? p.y - designedEyeHeight : 0;
            transform.SetPositionAndRotation(new Vector3(p.x, seatedBaseHeight + heightOffset, p.z) + forward.normalized * reachOffset, Quaternion.LookRotation(forward.normalized, Vector3.up));
            var first = !Placed;
            Placed = true;
            if (first) PlacedEvent?.Invoke();
        }
    }
}
