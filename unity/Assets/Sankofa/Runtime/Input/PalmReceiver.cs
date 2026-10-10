using System;
using Sankofa.Interaction;
using UnityEngine;

namespace Sankofa.Runtime.Input
{
    /// <summary>
    /// While a receive invitation is open, watches both hands for an open palm held under the offered object.
    /// Gives immediate local feedback; the flow decides whether the action is accepted.
    /// </summary>
    [DefaultExecutionOrder(150)]
    public sealed class PalmReceiver : MonoBehaviour
    {
        [SerializeField] HandSource[] hands;
        [Tooltip("A ring under the offered object showing where to hold an open hand, filling as the palm holds.")]
        [SerializeField] Renderer guide;
        [SerializeField] float guideBelow = 0.1f;

        static readonly int FillId = Shader.PropertyToID("_Fill");
        static readonly int AlphaId = Shader.PropertyToID("_Alpha");
        MaterialPropertyBlock block;
        float guideAlpha;
        float guideFill;
        PalmReceiveGesture[] gestures;
        string invitationId;
        Transform target;

        public event Action<string, HandSource> Received;
        /// <summary>0..1: how close an offering palm is to completing the receive.</summary>
        public float Feedback { get; private set; }
        public bool Open => invitationId != null;

        void Awake()
        {
            gestures = new PalmReceiveGesture[hands.Length];
            for (var i = 0; i < hands.Length; i++) gestures[i] = new PalmReceiveGesture();
            block = new MaterialPropertyBlock();
            if (guide != null) guide.enabled = false;
        }

        public void Listen(string invitation, Transform offered)
        {
            invitationId = invitation;
            target = offered;
            foreach (var g in gestures) g.Reset();
            Feedback = 0;
        }

        public void Close()
        {
            invitationId = null;
            target = null;
            Feedback = 0;
        }

        void Update()
        {
            ShowGuide();
            // While paused nothing is taken: a gesture finished now would be refused and its hand spent.
            if (invitationId == null || target == null || Sankofa.Runtime.Stage.StageClock.Paused)
            {
                // A hold begun before a pause starts again after it, rather than finishing on the first frame back.
                if (Feedback > 0) foreach (var g in gestures) g.Reset();
                Feedback = 0;
                return;
            }
            var p = target.position;
            var position = new System.Numerics.Vector3(p.x, p.y, p.z);
            var feedback = 0f;
            for (var i = 0; i < hands.Length; i++)
            {
                var g = gestures[i];
                if (g.Update(hands[i].Sample, position, Time.deltaTime))
                {
                    var id = invitationId;
                    guideFill = 1f;
                    Received?.Invoke(id, hands[i]);
                    return;
                }
                feedback = Mathf.Max(feedback, g.Proximity * 0.6f + g.Progress * 0.4f);
            }
            Feedback = feedback;
        }

        /// <summary>The ring fades in under what is offered and fills with the palm's hold; it fades out once taken.</summary>
        void ShowGuide()
        {
            if (guide == null) return;
            var open = invitationId != null && target != null && !Sankofa.Runtime.Stage.StageClock.Paused;
            guideAlpha = Mathf.MoveTowards(guideAlpha, open ? 1f : 0f, Time.unscaledDeltaTime * 2f);
            guide.enabled = guideAlpha > 0.01f;
            if (!guide.enabled) return;
            if (open) guide.transform.SetPositionAndRotation(target.position + Vector3.down * guideBelow, Quaternion.Euler(90f, 0, 0));
            guide.GetPropertyBlock(block);
            // Closing, the ring fades at the fill it had: only a palm that took the gift ever fills it.
            if (open) guideFill = Feedback;
            block.SetFloat(FillId, guideFill);
            block.SetFloat(AlphaId, guideAlpha * (0.8f + 0.2f * Mathf.Sin(Time.unscaledTime * 2.4f)));
            guide.SetPropertyBlock(block);
        }
    }
}
