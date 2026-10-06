using System.Collections.Generic;
using Sankofa.Interaction;
using UnityEngine;
using Num = System.Numerics;

namespace Sankofa.Runtime.Input
{
    /// <summary>
    /// One tracked hand from Meta's OpenXR hand skeleton, translated into engine-free samples. Low-confidence
    /// tracking counts as untracked so a reappearing hand cannot press or receive anything.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class HandSource : MonoBehaviour
    {
        /// <summary>Above the transition curtain (Overlay, 4000), so hands stay visible while the world changes.</summary>
        public const int HandRenderQueue = 4001;

        [SerializeField] bool left;
        [SerializeField] OVRHand hand;
        [SerializeField] OVRSkeleton skeleton;

        readonly Dictionary<Material, Material> raised = new Dictionary<Material, Material>();
        Transform wrist, indexProximal, middleProximal, littleProximal, indexTip, middleTip, ringTip;
        Renderer handRenderer;

        public bool IsLeft => left;
        /// <summary>Meta's hand, for its system aim pose and pinch strength.</summary>
        public OVRHand Hand => hand;
        public bool Tracked { get; private set; }
        public Vector3? Fingertip => Tracked ? indexTip.position : (Vector3?)null;
        public Vector3 PalmCenter => Tracked ? (wrist.position + middleProximal.position) * 0.5f : Vector3.zero;

        public HandSample Sample => !Tracked ? new HandSample { IsLeft = left } : new HandSample
        {
            Tracked = true,
            IsLeft = left,
            Wrist = N(wrist.position),
            IndexProximal = N(indexProximal.position),
            MiddleProximal = N(middleProximal.position),
            LittleProximal = N(littleProximal.position),
            IndexTip = N(indexTip.position),
            MiddleTip = N(middleTip.position),
            RingTip = N(ringTip.position),
        };

        static Num.Vector3 N(Vector3 v) => new Num.Vector3(v.x, v.y, v.z);

        void Update()
        {
            if (indexTip == null) FindBones();
            Tracked = indexTip != null && hand != null && hand.IsTracked && hand.IsDataHighConfidence && skeleton.IsDataValid;
        }

        void LateUpdate()
        {
            if (handRenderer == null && hand != null) handRenderer = hand.GetComponentInChildren<SkinnedMeshRenderer>();
            if (handRenderer == null) return;
            // Meta's renderer swaps between its own materials; give each a raised-queue copy once.
            var shared = handRenderer.sharedMaterial;
            if (shared == null || shared.renderQueue == HandRenderQueue) return;
            if (!raised.TryGetValue(shared, out var copy))
            {
                copy = new Material(shared) { renderQueue = HandRenderQueue };
                raised.Add(shared, copy);
            }
            handRenderer.sharedMaterial = copy;
        }

        void OnDestroy()
        {
            foreach (var copy in raised.Values) Destroy(copy);
        }

        void FindBones()
        {
            if (skeleton == null || !skeleton.IsInitialized || skeleton.Bones == null) return;
            foreach (var bone in skeleton.Bones)
            {
                switch (bone.Id)
                {
                    case OVRSkeleton.BoneId.XRHand_Wrist: wrist = bone.Transform; break;
                    case OVRSkeleton.BoneId.XRHand_IndexProximal: indexProximal = bone.Transform; break;
                    case OVRSkeleton.BoneId.XRHand_MiddleProximal: middleProximal = bone.Transform; break;
                    case OVRSkeleton.BoneId.XRHand_LittleProximal: littleProximal = bone.Transform; break;
                    case OVRSkeleton.BoneId.XRHand_IndexTip: indexTip = bone.Transform; break;
                    case OVRSkeleton.BoneId.XRHand_MiddleTip: middleTip = bone.Transform; break;
                    case OVRSkeleton.BoneId.XRHand_RingTip: ringTip = bone.Transform; break;
                }
            }
            if (wrist == null || indexProximal == null || middleProximal == null || littleProximal == null || middleTip == null || ringTip == null)
                indexTip = null;
        }
    }
}
