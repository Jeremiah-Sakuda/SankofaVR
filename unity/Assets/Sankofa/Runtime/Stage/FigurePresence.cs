using System.Linq;
using UnityEngine;

namespace Sankofa.Runtime.Stage
{
    /// <summary>Small authored acting over the library motion. Owns eyes and finger joints only;
    /// it neither chooses an action nor changes the story. Uses the paused stage clock.</summary>
    [DefaultExecutionOrder(30)]
    public sealed class FigurePresence : MonoBehaviour
    {
        [SerializeField] SkinnedMeshRenderer eyes;
        [SerializeField] Transform heldAnchor;
        [SerializeField] Transform rightHand;
        [SerializeField] Transform leftHand;
        [Tooltip("Right then left; thumb, index, middle, ring, pinky; three joints each.")]
        [SerializeField] Transform[] fingers = new Transform[0];
        Quaternion[] rest;
        StageObject[] heldObjects;
        FigureMotion motion;
        float elapsed, nextBlink, blinking = -1;
        float rightGrip, leftGrip;
        MaterialPropertyBlock eyeProperties;
        static readonly int ClosedId = Shader.PropertyToID("_EyeClosed");

        void Awake()
        {
            motion = GetComponent<FigureMotion>();
            eyeProperties = new MaterialPropertyBlock();
            rest = fingers.Select(t => t.localRotation).ToArray();
            heldObjects = heldAnchor.GetComponentsInChildren<StageObject>(true);
            // Different initial times keep companions from blinking together. No random state or network.
            nextBlink = 2.7f + transform.position.x * 0.23f;
        }

        void Update()
        {
            var dt = StageClock.DeltaTime;
            elapsed += dt;
            if (blinking < 0 && elapsed >= nextBlink) blinking = 0;
            if (blinking >= 0)
            {
                blinking += dt;
                var amount = Mathf.Sin(Mathf.Clamp01(blinking / 0.20f) * Mathf.PI);
                eyes.SetBlendShapeWeight(0, amount * 100);
                if (blinking >= 0.20f)
                {
                    blinking = -1;
                    nextBlink = elapsed + 4.2f + Mathf.Sin(elapsed * 0.7f) * 0.8f;
                }
            }
            eyes.GetPropertyBlock(eyeProperties);
            eyeProperties.SetFloat(ClosedId, eyes.GetBlendShapeWeight(0) / 100);
            eyes.SetPropertyBlock(eyeProperties);
        }

        void LateUpdate()
        {
            var held = System.Array.Find(heldObjects, o => o != null && o.Visible && o.InHolderHands && o.transform.IsChildOf(heldAnchor));
            var dt = StageClock.DeltaTime;
            rightGrip = Mathf.MoveTowards(rightGrip, held != null ? 1 : 0, dt * 3);
            leftGrip = Mathf.MoveTowards(leftGrip, held != null ? motion.SharingWeight : 0, dt * 3);
            for (var i = 0; i < fingers.Length; i++)
            {
                var finger = fingers[i];
                finger.localRotation = rest[i]; // An owned pose, not an accumulating offset over previous frames.
                var hand = i < 15 ? rightHand : leftHand;
                var amount = i < 15 ? rightGrip : leftGrip;
                var joint = i % 3;
                var thumb = i % 15 < 3;
                var angle = (joint == 0 ? 27 : joint == 1 ? 48 : 30) * amount * (thumb ? 0.55f : 1);
                if (angle <= 0.001f) continue;
                var direction = finger.childCount > 0 ? finger.GetChild(0).position - finger.position : finger.up;
                // The held object's neck (or centre for a bundle) directs the curl, instead of assuming the
                // artist's joint axes. This also works after the entire stage has rotated.
                var target = held != null ? held.PourPosition ?? held.transform.position : hand.position - hand.up * 0.05f;
                var axis = Vector3.Cross(direction, target - finger.position);
                if (axis.sqrMagnitude < 1e-8f) axis = hand.right;
                finger.rotation = Quaternion.AngleAxis(angle, axis.normalized) * finger.rotation;
            }
        }
    }
}
