using System;
using System.Numerics;

namespace Sankofa.Interaction
{
    /// <summary>Joint positions of one tracked hand in world space (metres, +Y up).</summary>
    public struct HandSample
    {
        public bool Tracked;
        public bool IsLeft;
        public Vector3 Wrist;
        public Vector3 IndexProximal;
        public Vector3 MiddleProximal;
        public Vector3 LittleProximal;
        public Vector3 IndexTip;
        public Vector3 MiddleTip;
        public Vector3 RingTip;
    }

    /// <summary>
    /// Palm geometry from joint positions alone, so it does not depend on a runtime's joint axis conventions.
    /// </summary>
    public static class HandGeometry
    {
        public static Vector3 PalmCenter(in HandSample h) => (h.Wrist + h.MiddleProximal) * 0.5f;

        /// <summary>
        /// Unit vector out of the palm. The knuckle triangle's winding is mirrored between hands: for a right hand
        /// held palm down with fingers forward, (index − wrist) × (little − wrist) points up, out of the back.
        /// </summary>
        public static Vector3 PalmNormal(in HandSample h)
        {
            var n = Vector3.Cross(h.IndexProximal - h.Wrist, h.LittleProximal - h.Wrist);
            if (n.LengthSquared() < 1e-12f) return Vector3.Zero;
            n = Vector3.Normalize(n);
            return h.IsLeft ? n : -n;
        }

        /// <summary>
        /// Fingertip reach relative to knuckle distance: about 2 with the fingers extended, near 1 in a fist.
        /// Scale-free, so it works for small and large hands.
        /// </summary>
        public static float Openness(in HandSample h)
        {
            var knuckle = Vector3.Distance(h.Wrist, h.MiddleProximal);
            if (knuckle < 1e-4f) return 0;
            var reach = (Vector3.Distance(h.Wrist, h.IndexTip) + Vector3.Distance(h.Wrist, h.MiddleTip) + Vector3.Distance(h.Wrist, h.RingTip)) / 3f;
            return reach / knuckle;
        }
    }

    public sealed class PalmReceiveSettings
    {
        /// <summary>Largest horizontal distance between palm centre and the offered object.</summary>
        public float MaxHorizontal = 0.16f;
        /// <summary>How far below the object the palm may be; a cupped palm sits under what it receives.</summary>
        public float MaxBelow = 0.24f;
        public float MaxAbove = 0.06f;
        /// <summary>Palm normal · up. 0.6 accepts palms tilted up to about 53 degrees from facing the sky.</summary>
        public float MinUpward = 0.6f;
        public float MinOpenness = 1.45f;
        /// <summary>Seconds the palm must stay in place, so a hand passing by does not take the object.</summary>
        public double Dwell = 0.35;
        /// <summary>Distance at which local feedback begins.</summary>
        public float FeedbackRange = 0.45f;
    }

    /// <summary>
    /// Detects one open palm held, facing up, beneath an offered object for a short dwell. A deliberate action:
    /// glancing at or passing near the object never completes it. Reports progress for immediate local feedback.
    /// </summary>
    public sealed class PalmReceiveGesture
    {
        readonly PalmReceiveSettings settings;
        double held;
        bool done;

        public PalmReceiveGesture(PalmReceiveSettings settings = null) => this.settings = settings ?? new PalmReceiveSettings();

        /// <summary>0 when the hand is far or untracked, rising to 1 as an open palm approaches the object.</summary>
        public float Proximity { get; private set; }
        /// <summary>Fraction of the dwell completed while the palm is in place.</summary>
        public float Progress => (float)Math.Min(1.0, held / settings.Dwell);
        public bool InPlace { get; private set; }
        public bool Received => done;

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

            var palm = HandGeometry.PalmCenter(hand);
            var offset = target - palm;
            var horizontal = new Vector2(offset.X, offset.Z).Length();
            var open = HandGeometry.Openness(hand) >= settings.MinOpenness;
            var upward = Vector3.Dot(HandGeometry.PalmNormal(hand), Vector3.UnitY) >= settings.MinUpward;

            var distance = offset.Length();
            Proximity = open && upward ? Math.Max(0f, 1f - distance / settings.FeedbackRange) : 0f;

            InPlace = open && upward && horizontal <= settings.MaxHorizontal && offset.Y <= settings.MaxBelow && offset.Y >= -settings.MaxAbove;
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
