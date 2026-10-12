using Sankofa.Core.Story;
using UnityEngine;

namespace Sankofa.Runtime.Stage
{
    /// <summary>A small vocabulary of illustrated poses. Accepted story actions own all changes.</summary>
    [RequireComponent(typeof(StageObject))]
    public sealed class IllustratedFigure : MonoBehaviour, IGlancer
    {
        [SerializeField] Renderer surface;
        [SerializeField] Vector4[] frames;
        [SerializeField] Transform giftOrigin;
        [SerializeField] Vector3 waitingHand;
        [SerializeField] Vector3 offeringHand;
        [SerializeField] float dissolveSeconds = 0.65f;
        [Tooltip("Degrees the painted figure turns, at most, toward what she glances at.")]
        [SerializeField] float glanceTurn = 15f;

        MaterialPropertyBlock block;
        Vector3 surfaceRest;
        bool surfaceRestKnown;
        Vector3 glanceAt;
        float glanceFor, glanceWeight;
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

        public bool CanGlance => surface != null;
        /// <summary>How far, 0 to 1, she is turned toward what she glances at.</summary>
        public float GlanceWeight => glanceWeight;

        /// <summary>
        /// Turns the painted figure a little toward a point for a while, and lifts her light: a woman of the story
        /// turning to the visitor who looked at her or acted, or to what the visitor looked at.
        /// </summary>
        public void Glance(Vector3 at, float seconds)
        {
            glanceAt = at;
            glanceFor = seconds;
            if (TryGetComponent<StageObject>(out var staged)) staged.Pulse();
        }

        public void ResetState()
        {
            glanceFor = glanceWeight = 0;
            Turn(0);
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
            glanceFor = Mathf.Max(0, glanceFor - StageClock.DeltaTime);
            glanceWeight = Mathf.MoveTowards(glanceWeight, glanceFor > 0 ? 1 : 0, StageClock.DeltaTime / 0.5f);
            Turn(Mathf.SmoothStep(0, 1, glanceWeight));
            Apply();
        }

        /// <summary>The card turns about the figure's own upright, toward the glance, by its weight.</summary>
        void Turn(float weight)
        {
            if (surface == null) return;
            var card = surface.transform;
            if (!surfaceRestKnown)
            {
                surfaceRest = card.localPosition;
                surfaceRestKnown = true;
            }
            var yaw = 0f;
            if (weight > 0.001f)
            {
                // The painting faces along the figure's -Z; turn that toward the point, level.
                var facing = Vector3.ProjectOnPlane(transform.rotation * Vector3.back, Vector3.up);
                var toward = Vector3.ProjectOnPlane(glanceAt - card.position, Vector3.up);
                if (facing.sqrMagnitude > 1e-6f && toward.sqrMagnitude > 1e-6f)
                    yaw = Mathf.Clamp(Vector3.SignedAngle(facing, toward, Vector3.up), -glanceTurn, glanceTurn) * weight;
            }
            var turn = Quaternion.Euler(0, yaw, 0);
            card.localRotation = turn;
            card.localPosition = turn * surfaceRest;
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
