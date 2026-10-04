using System;
using System.Collections.Generic;
using System.Numerics;

namespace Sankofa.Interaction
{
    /// <summary>Something the visitor might look toward: an id, its centre and an approximate radius in metres.</summary>
    public readonly struct AttentionCandidate
    {
        public AttentionCandidate(string id, Vector3 position, float radius)
        {
            Id = id;
            Position = position;
            Radius = radius;
        }

        public string Id { get; }
        public Vector3 Position { get; }
        public float Radius { get; }
    }

    public sealed class AttentionSettings
    {
        /// <summary>Degrees beyond an object's own angular radius that still count as facing it.</summary>
        public float MarginDegrees = 7f;
        /// <summary>Seconds of facing an object before it counts as noticed.</summary>
        public double Dwell = 1.4;
        /// <summary>Facing away for this long ends a look; the object must be looked at afresh to be noticed again.</summary>
        public double Release = 0.7;
    }

    /// <summary>
    /// Notices which object the visitor's head has rested toward. Head direction is not eye tracking: it only
    /// identifies a candidate, which the application may use to surface an optional question. It never confirms
    /// a choice or advances the story.
    /// </summary>
    public sealed class AttentionTracker
    {
        readonly AttentionSettings settings;
        string current;
        double held;
        double away;
        bool reported;

        public AttentionTracker(AttentionSettings settings = null) => this.settings = settings ?? new AttentionSettings();

        /// <summary>The object currently faced, if any.</summary>
        public string Current => current;
        public float Progress => current == null ? 0f : (float)Math.Min(1.0, held / settings.Dwell);

        public void Reset()
        {
            current = null;
            held = 0;
            away = 0;
            reported = false;
        }

        /// <summary>Returns an object's id once, on the update in which a look at it completes the dwell; otherwise null.</summary>
        public string Update(Vector3 head, Vector3 forward, IReadOnlyList<AttentionCandidate> candidates, double deltaSeconds)
        {
            var dt = Math.Max(0, deltaSeconds);
            var faced = Faced(head, forward, candidates);
            if (faced != null && faced == current)
            {
                held += dt;
                away = 0;
            }
            else if (faced == null)
            {
                if (current != null && (away += dt) >= settings.Release) Reset();
            }
            else
            {
                current = faced;
                held = dt;
                away = 0;
                reported = false;
            }
            if (current != null && !reported && held >= settings.Dwell)
            {
                reported = true;
                return current;
            }
            return null;
        }

        string Faced(Vector3 head, Vector3 forward, IReadOnlyList<AttentionCandidate> candidates)
        {
            if (forward.LengthSquared() < 1e-8f) return null;
            var direction = Vector3.Normalize(forward);
            string best = null;
            var bestAngle = double.MaxValue;
            foreach (var c in candidates)
            {
                var to = c.Position - head;
                var distance = to.Length();
                if (distance < 1e-3f) continue;
                var angle = Math.Acos(Math.Max(-1.0, Math.Min(1.0, Vector3.Dot(direction, to / distance)))) * 180.0 / Math.PI;
                var allowed = Math.Atan2(c.Radius, distance) * 180.0 / Math.PI + settings.MarginDegrees;
                if (angle <= allowed && angle < bestAngle)
                {
                    best = c.Id;
                    bestAngle = angle;
                }
            }
            return best;
        }
    }
}
