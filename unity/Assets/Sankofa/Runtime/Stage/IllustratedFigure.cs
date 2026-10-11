using Sankofa.Core.Story;
using UnityEngine;

namespace Sankofa.Runtime.Stage
{
    /// <summary>A small vocabulary of illustrated poses. Accepted story actions own all changes.</summary>
    [RequireComponent(typeof(StageObject))]
    public sealed class IllustratedFigure : MonoBehaviour
    {
        [SerializeField] Renderer surface;
        [SerializeField] Vector4[] frames;
        [SerializeField] Transform giftOrigin;
        [SerializeField] Vector3 waitingHand;
        [SerializeField] Vector3 offeringHand;
        [SerializeField] float dissolveSeconds = 0.65f;

        MaterialPropertyBlock block;
        int previousPose, pose;
        float blend = 1;
        public int Pose => pose;
        public float PoseBlend => blend;
        public Vector3 GiftOrigin => giftOrigin.position;
        public Renderer Surface => surface;

        public void PaintMemory(Renderer memory, MaterialPropertyBlock properties, float visibility)
        {
            properties.SetVector("_FrameA", frames[0]);
            properties.SetVector("_FrameB", frames[0]);
            properties.SetFloat("_PoseBlend", 1);
            properties.SetFloat("_Visibility", visibility);
            properties.SetFloat("_Unfold", 1);
            memory.SetPropertyBlock(properties);
        }

        void Awake() { block = new MaterialPropertyBlock(); ResetState(); }

        public void ResetState()
        {
            previousPose = pose = 0;
            blend = 1;
            if (giftOrigin != null) giftOrigin.localPosition = waitingHand;
            Apply();
        }

        public void SetPose(int value)
        {
            if (frames == null || frames.Length == 0) return;
            value = Mathf.Clamp(value, 0, frames.Length - 1);
            if (value == pose) return;
            previousPose = blend < 0.5f ? previousPose : pose;
            pose = value;
            blend = 0;
            Apply();
        }

        /// <summary>The gourd starts at a painted palm and crosses into the visitor's reachable space.</summary>
        public void Offer(StageObject item, StoryStage stage)
        {
            SetPose(1);
            giftOrigin.localPosition = offeringHand;
            item.ShowAt(giftOrigin.position);
            item.MoveTo(stage.OfferPosition(), 1.4f);
        }

        public void Perform(StoryAction action, StageObject item)
        {
            switch (action.Kind)
            {
                case ActionKind.Reveal: item.Reveal(); break;
                case ActionKind.Illuminate: item.Illuminate(); break;
                case ActionKind.Move: SetPose(action.At == null ? 0 : 1); break;
                case ActionKind.Withdraw: item.Withdraw(item.HomePosition, 1.2f); break;
            }
        }

        void Update()
        {
            blend = Mathf.MoveTowards(blend, 1, StageClock.DeltaTime / Mathf.Max(0.05f, dissolveSeconds));
            Apply();
        }

        void Apply()
        {
            if (surface == null || frames == null || frames.Length == 0) return;
            if (block == null) block = new MaterialPropertyBlock();
            surface.GetPropertyBlock(block); // Keep StageObject's visibility and attention feedback.
            block.SetVector("_FrameA", frames[previousPose]);
            block.SetVector("_FrameB", frames[pose]);
            block.SetFloat("_PoseBlend", Mathf.SmoothStep(0, 1, blend));
            surface.SetPropertyBlock(block);
        }
    }
}
