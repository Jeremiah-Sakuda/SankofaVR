using System;
using System.Collections.Generic;
using System.Numerics;

namespace Sankofa.Interaction
{
    /// <summary>
    /// Where and how a hand sweeps to turn the story stage. Positions are in the stage's own space in metres:
    /// x toward the visitor's right, y up, z away from the visitor, with the origin at the centre of the turntable.
    /// </summary>
    public sealed class TurnSettings
    {
        /// <summary>The sweep is read between the visitor and the stage, within seated reach.</summary>
        public float HalfWidth = 0.50f, Near = -0.55f, Far = 0.15f;
        /// <summary>Above the caption card's top edge, so a hand moving between its buttons is not read as a sweep.</summary>
        public float Low = 0.06f, High = 0.60f;
        /// <summary>How far sideways the hand must travel.</summary>
        public float Travel = 0.20f;
        /// <summary>The travel must happen within this long: a hand that drifts does not turn the stage.</summary>
        public double Within = 0.7;
        /// <summary>Forward and vertical movement must each stay under this share of the sideways travel.</summary>
        public float Straightness = 0.6f;
        /// <summary>A sweep survives tracking dropping out for this long.</summary>
        public double LostGrace = 0.12;
        /// <summary>After a turn, nothing is read for this long, while the stage turns.</summary>
        public double Rest = 1.5;
    }

    /// <summary>
    /// A hand swept sideways in front of the stage turns it, the way a hand turns a table that spins. Reaching
    /// forward, lifting a hand, holding it still or moving it slowly does nothing. Reports how far through a sweep
    /// the hand is, so the stage can lean with it before it gives way.
    /// </summary>
    public sealed class TurnGesture
    {
        readonly TurnSettings settings;
        readonly List<KeyValuePair<double, Vector3>> trail = new List<KeyValuePair<double, Vector3>>();
        double clock, lost, rest;

        public TurnGesture(TurnSettings settings = null) => this.settings = settings ?? new TurnSettings();

        /// <summary>From -1 to 1: the share of a full sweep made so far, positive toward the visitor's right.</summary>
        public float Lean { get; private set; }

        public void Reset()
        {
            trail.Clear();
            Lean = 0; lost = 0; rest = 0;
        }

        /// <summary>
        /// Returns 1 for a completed sweep toward the visitor's right and -1 toward their left, once; otherwise 0.
        /// </summary>
        public int Update(bool tracked, Vector3 palm, double deltaSeconds)
        {
            var dt = Math.Max(0, deltaSeconds);
            if (rest > 0)
            {
                rest -= dt;
                trail.Clear(); Lean = 0;
                return 0;
            }
            if (!tracked || !Inside(palm))
            {
                lost += dt;
                if (lost > settings.LostGrace) { trail.Clear(); Lean = 0; }
                return 0;
            }
            lost = 0;
            clock += dt;
            trail.Add(new KeyValuePair<double, Vector3>(clock, palm));
            trail.RemoveAll(sample => sample.Key < clock - settings.Within);

            // The longest mostly-sideways movement that ends where the hand is now.
            var best = 0f;
            foreach (var sample in trail)
            {
                var from = sample.Value;
                var sideways = palm.X - from.X;
                var limit = settings.Straightness * Math.Abs(sideways);
                if (Math.Abs(sideways) <= Math.Abs(best) || Math.Abs(palm.Z - from.Z) > limit || Math.Abs(palm.Y - from.Y) > limit) continue;
                best = sideways;
            }
            Lean = Math.Max(-1f, Math.Min(1f, best / settings.Travel));
            if (Math.Abs(best) < settings.Travel) return 0;
            trail.Clear(); Lean = 0; rest = settings.Rest;
            return best > 0 ? 1 : -1;
        }

        bool Inside(Vector3 p) =>
            Math.Abs(p.X) <= settings.HalfWidth && p.Z >= settings.Near && p.Z <= settings.Far && p.Y >= settings.Low && p.Y <= settings.High;
    }
}
