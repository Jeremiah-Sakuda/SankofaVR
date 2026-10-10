using System;
using Sankofa.Interaction;
using Sankofa.Runtime.Stage;
using UnityEngine;

namespace Sankofa.Runtime.Input
{
    /// <summary>
    /// While a touch invitation is open, watches both hands for a fingertip or palm resting on the object's touch
    /// point. Gives immediate local feedback; the flow decides whether the action is accepted.
    /// </summary>
    [DefaultExecutionOrder(150)]
    public sealed class TouchReceiver : MonoBehaviour
    {
        [SerializeField] HandSource[] hands;

        TouchGesture[] gestures;
        string invitationId;
        StageObject target;

        public event Action<string, HandSource> Touched;
        /// <summary>0..1: how close a hand is to completing the touch.</summary>
        public float Feedback { get; private set; }
        public bool Open => invitationId != null;

        public void Listen(string invitation, StageObject touched)
        {
            invitationId = invitation;
            target = touched;
            gestures = new TouchGesture[hands.Length];
            for (var i = 0; i < hands.Length; i++) gestures[i] = new TouchGesture(new TouchSettings { Radius = touched.TouchRadius });
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
            // While paused nothing is touched: a gesture finished now would be refused and its hand spent.
            if (invitationId == null || target == null || StageClock.Paused)
            {
                // A touch begun before a pause starts again after it, rather than finishing on the first frame back.
                if (Feedback > 0) foreach (var g in gestures) g.Reset();
                Feedback = 0;
                return;
            }
            var p = target.TouchPosition;
            var position = new System.Numerics.Vector3(p.x, p.y, p.z);
            var feedback = 0f;
            for (var i = 0; i < hands.Length; i++)
            {
                var g = gestures[i];
                if (g.Update(hands[i].Sample, position, Time.deltaTime))
                {
                    var id = invitationId;
                    Touched?.Invoke(id, hands[i]);
                    return;
                }
                feedback = Mathf.Max(feedback, g.Proximity * 0.6f + g.Progress * 0.4f);
            }
            Feedback = feedback;
        }
    }
}
