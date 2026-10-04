using System;
using System.Numerics;

namespace Sankofa.Interaction
{
    public sealed class TouchSettings
    {
        /// <summary>A fingertip or palm within this distance of the touch point is touching it.</summary>
        public float Radius = 0.08f;
        /// <summary>Seconds the hand must stay, so a hand passing through does not count.</summary>
        public double Dwell = 0.3;
        /// <summary>Distance at which local feedback begins.</summary>
        public float FeedbackRange = 0.35f;
    }

    /// <summary>
    /// Detects a hand resting on an object that the visitor has been invited to touch: the index fingertip or the
    /// palm centre stays within reach of its touch point for a short dwell. Reports proximity for local feedback.
    /// </summary>
    public sealed class TouchGesture
    {
        readonly TouchSettings settings;
        double held;
        bool done;

        public TouchGesture(TouchSettings settings = null) => this.settings = settings ?? new TouchSettings();

        /// <summary>0 when the hand is far or untracked, rising to 1 as it reaches the touch point.</summary>
        public float Proximity { get; private set; }
        public float Progress => (float)Math.Min(1.0, held / settings.Dwell);
        public bool InPlace { get; private set; }
        public bool Touched => done;

        public void Reset()
        {
            held = 0;
            done = false;
            InPlace = false;
            Proximity = 0;
        }

        /// <summary>Returns true once, on the update that completes the dwell.</summary>
        public bool Update(in HandSample hand, Vector3 target, double deltaSeconds)
        {
            if (done) return false;
            if (!hand.Tracked)
            {
                held = 0;
                InPlace = false;
                Proximity = 0;
                return false;
            }
            var distance = Math.Min(Vector3.Distance(hand.IndexTip, target), Vector3.Distance(HandGeometry.PalmCenter(hand), target));
            Proximity = Math.Max(0f, 1f - distance / settings.FeedbackRange);
            InPlace = distance <= settings.Radius;
            held = InPlace ? held + Math.Max(0, deltaSeconds) : 0;
            if (InPlace && held >= settings.Dwell)
            {
                done = true;
                return true;
            }
            return false;
        }
    }
}
