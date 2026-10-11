using UnityEngine;

namespace Sankofa.Runtime.Stage
{
    /// <summary>Resolves both actors after their base animation and before fingers. One giver owns an exchange;
    /// this never schedules narration or advances a visit.</summary>
    [DefaultExecutionOrder(20)]
    [RequireComponent(typeof(FigureMotion))]
    public sealed class FigureExchange : MonoBehaviour
    {
        [SerializeField] Transform chest;
        FigureMotion motion;
        void Awake() => motion = GetComponent<FigureMotion>();

        void LateUpdate()
        {
            var partner = motion.ExchangePartner;
            var time = motion.ExchangeTime;
            if (partner == null || time < 0 || time > 9) { motion.DirectSharing(); return; }
            partner.FacePartner(motion.transform.position);
            var companion = partner.GetComponent<FigureExchange>();
            var weight = motion.SharingWeight;
            // Look at the companion first; then both attend to the vessel. Finally look up and acknowledge.
            var attend = Mathf.SmoothStep(0, 1, (time - 0.6f) / 1.2f) * (1 - Mathf.SmoothStep(0, 1, (time - 6.4f) / 1.2f));
            var between = (motion.transform.position + partner.transform.position) * 0.5f + Vector3.up * 0.95f;
            var otherHead = partner.Head != null ? partner.Head.position : between + Vector3.up * 0.4f;
            var ownHead = motion.Head != null ? motion.Head.position : between + Vector3.up * 0.4f;
            var nod = Mathf.Sin(Mathf.Clamp01((time - 7.1f) / 0.9f) * Mathf.PI) * 0.045f;
            // Small whole-torso preparation, rather than isolated arms reaching out of a rigid body.
            Lean(chest, motion.transform, weight * 4f);
            if (companion != null) Lean(companion.chest, partner.transform, weight * 2.5f);
            motion.DirectLook(Vector3.Lerp(otherHead - Vector3.up * nod, between, attend), weight);
            partner.DirectLook(Vector3.Lerp(ownHead - Vector3.up * nod, between, attend), weight);
            motion.DirectSharing();
        }

        static void Lean(Transform bone, Transform actor, float degrees)
        {
            if (bone != null) bone.rotation = Quaternion.AngleAxis(degrees, actor.right) * bone.rotation;
        }
    }
}
