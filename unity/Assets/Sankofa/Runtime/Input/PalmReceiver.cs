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
            if (invitationId == null || target == null) return;
            var p = target.position;
            var position = new System.Numerics.Vector3(p.x, p.y, p.z);
            var feedback = 0f;
            for (var i = 0; i < hands.Length; i++)
            {
                var g = gestures[i];
                if (g.Update(hands[i].Sample, position, Time.deltaTime))
                {
                    var id = invitationId;
                    Received?.Invoke(id, hands[i]);
                    return;
                }
                feedback = Mathf.Max(feedback, g.Proximity * 0.6f + g.Progress * 0.4f);
            }
            Feedback = feedback;
        }
    }
}
