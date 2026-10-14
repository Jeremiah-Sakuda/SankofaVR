using Sankofa.Core.Story;
using UnityEngine;

namespace Sankofa.Runtime.Stage
{
    /// <summary>Authored acting and resolved invitations share a small pose vocabulary; neither creates visitor choices.</summary>
    [RequireComponent(typeof(StageObject))]
    public sealed class IllustratedFigure : MonoBehaviour, IGlancer
    {
        [SerializeField] Renderer surface;
        [SerializeField] Vector4[] frames;
        [SerializeField] Texture2D expressionAtlas;
        [SerializeField] Vector4[] expressionFrames;
        Texture originalAtlas;
        [SerializeField] Transform giftOrigin;
        [SerializeField] Vector3 waitingHand;
        [SerializeField] Vector3 offeringHand;
        [SerializeField] float dissolveSeconds = 0.65f;
        [Tooltip("Degrees the painted figure turns, at most, toward what she glances at.")]
        [SerializeField] float glanceTurn = 15f;
        [SerializeField] StoryTransition transition;
        [Tooltip("Seconds a gift rests in the painted palms, once they show, before it crosses to the visitor.")]
        [SerializeField] float restInPalm = 0.5f;

        MaterialPropertyBlock block;
        Vector3 surfaceRest;
        bool surfaceRestKnown;
        Vector3 glanceAt;
        Matrix4x4 glanceFrame;
        float glanceFor, glanceWeight;
        int previousPose, pose;
        float blend = 1;
        StageObject offering;
        StoryStage offeringStage;
        bool offeringShown;
        float offeringRest;
        int ShownPose => blend < 0.5f ? previousPose : pose;
        /// <summary>The gift waiting to be given, shown in the palms once they are painted, then sent across.</summary>
        public StageObject Offering => offering;
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

        void Awake() { block = new MaterialPropertyBlock(); originalAtlas = surface.sharedMaterial.mainTexture; ResetState(); }

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
            glanceFrame = transform.parent != null ? transform.parent.localToWorldMatrix : Matrix4x4.identity;
            glanceFor = seconds;
            if (TryGetComponent<StageObject>(out var staged)) staged.Pulse();
        }

        public void ResetState()
        {
            glanceFor = glanceWeight = 0;
            offering = null;
            previousPose = pose = 0;
            blend = 1;
            Turn(0);
            Apply();
        }

        public void SetPose(int value)
        {
            if (frames == null || frames.Length == 0) return;
            value = Mathf.Clamp(value, 0, frames.Length + (expressionAtlas != null && expressionFrames != null ? expressionFrames.Length : 0) - 1);
            if (value == pose) return;
            // The light dips, the pose swaps at the bottom, the light returns. Mid-change, keep the brightness
            // continuous: still dimming, carry on into the new pose; already rising, dim the shown pose from there.
            if (blend >= 0.5f)
            {
                previousPose = pose;
                blend = 1 - blend;
            }
            pose = value;
            if (transition != null && transition.ReducedMotion)
            {
                previousPose = pose;
                blend = 1;
            }
            Apply();
        }

        /// <summary>
        /// The gift appears in the painted palms only once the offering pose shows (never at the hip of the pose being
        /// left), rests there a moment so it is seen as hers, then crosses into the visitor's reachable space.
        /// </summary>
        public void Offer(StageObject item, StoryStage stage)
        {
            SetPose(1);
            offering = item;
            offeringStage = stage;
            offeringShown = false;
            offeringRest = restInPalm;
            Turn(Mathf.SmoothStep(0, 1, glanceWeight));
            Give(0);
        }

        /// <summary>Stops giving this gift: it was received before it crossed.</summary>
        public void CancelOffer(StageObject item)
        {
            if (offering == item) offering = null;
        }

        void Give(float dt)
        {
            if (offering == null || ShownPose != 1) return;
            if (!offeringShown)
            {
                offering.ShowAt(giftOrigin.position);
                offering.Pulse();
                offeringShown = true;
            }
            else offering.transform.position = giftOrigin.position;
            offeringRest -= dt;
            if (offeringRest > 0) return;
            offering.MoveTo(offeringStage.OfferPosition(), 1.4f);
            offering = null;
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
            var frame = transform.parent != null ? transform.parent.localToWorldMatrix : Matrix4x4.identity;
            if ((glanceFor > 0 || glanceWeight > 0) && frame != glanceFrame)
            {
                glanceAt = (frame * glanceFrame.inverse).MultiplyPoint3x4(glanceAt);
                glanceFrame = frame;
            }
            blend = Mathf.MoveTowards(blend, 1, StageClock.DeltaTime / Mathf.Max(0.05f, dissolveSeconds));
            if (transition != null && transition.ReducedMotion) { previousPose = pose; blend = 1; }
            glanceFor = Mathf.Max(0, glanceFor - StageClock.DeltaTime);
            glanceWeight = Mathf.MoveTowards(glanceWeight, glanceFor > 0 ? 1 : 0, StageClock.DeltaTime / 0.5f);
            Turn(Mathf.SmoothStep(0, 1, glanceWeight));
            Give(StageClock.DeltaTime);
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
            if (weight > 0.001f && (transition == null || !transition.ReducedMotion))
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
            // The painted palm and its physical gift must turn together until the gift leaves the hand.
            if (giftOrigin != null)
            {
                var shownPose = blend < 0.5f ? previousPose : pose;
                giftOrigin.localPosition = turn * (shownPose == 1 ? offeringHand : waitingHand);
                giftOrigin.localRotation = turn;
            }
        }

        void Apply()
        {
            if (surface == null || frames == null || frames.Length == 0) return;
            if (block == null) block = new MaterialPropertyBlock();
            surface.GetPropertyBlock(block); // Keep StageObject's visibility and attention feedback.
            // A clean swap at the dimmest point allows a sibling atlas without cross-atlas blending.
            var shown = ShownPose;
            var expression = shown >= frames.Length && expressionAtlas != null;
            var atlas = expression ? expressionAtlas : originalAtlas != null ? originalAtlas : surface.sharedMaterial.mainTexture;
            block.SetTexture("_MainTex", atlas);
            block.SetVector("_MainTex_TexelSize", new Vector4(1f/atlas.width, 1f/atlas.height, atlas.width, atlas.height));
            var frame = expression ? expressionFrames[shown-frames.Length] : frames[shown];
            block.SetVector("_FrameA", frame);
            block.SetVector("_FrameB", frame);
            block.SetFloat("_PoseBlend", blend);
            surface.SetPropertyBlock(block);
        }
    }
}
