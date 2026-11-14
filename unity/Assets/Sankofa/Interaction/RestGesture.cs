using System;
using System.Numerics;

namespace Sankofa.Interaction
{
    /// <summary>Where a hand may rest to show a table, relative to the visitor's eyes, and for how long.</summary>
    public sealed class RestSettings
    {
        /// <summary>Metres ahead of the eyes, measured level. Nearer than this is a hand in the lap, not on a table.</summary>
        public float NearestAhead = 0.30f, FarthestAhead = 0.85f;
        /// <summary>Metres to either side of straight ahead.</summary>
        public float Aside = 0.45f;
        /// <summary>Metres below the eyes: from a high counter down to a low table.</summary>
        public float LeastBelow = 0.18f, MostBelow = 0.95f;
        /// <summary>A resting palm may wander this far from where it settled.</summary>
        public float Still = 0.025f;
        /// <summary>Seconds the palm must stay.</summary>
        public double Hold = 1.0;
        /// <summary>A rest survives tracking dropping out for this long.</summary>
        public double LostGrace = 0.25;
    }

    /// <summary>
    /// A hand laid on a real table and held still shows where that table is. Reads only the palm's position against
    /// the eyes: a moving hand, a hand in the lap or a hand held up in the air shows nothing.
    /// </summary>
    public sealed class RestGesture
    {
        readonly RestSettings settings;
        Vector3 settled, sum;
        int samples;
        double held, lost;

        public RestGesture(RestSettings settings = null) => this.settings = settings ?? new RestSettings();

        /// <summary>0 to 1 through the hold, for local feedback.</summary>
        public float Progress => (float)Math.Min(1.0, held / settings.Hold);
        /// <summary>Where the palm rested: the mean of its positions through the hold.</summary>
        public Vector3 Place => samples > 0 ? sum / samples : settled;

        public void Reset()
        {
            held = 0; lost = 0; samples = 0; sum = Vector3.Zero;
        }

        /// <summary>
        /// <paramref name="ahead"/> is the level direction the visitor faces. Returns true once, when the hold completes;
        /// call <see cref="Reset"/> to read another.
        /// </summary>
        public bool Update(bool tracked, Vector3 palm, Vector3 eye, Vector3 ahead, double deltaSeconds)
        {
            var dt = Math.Max(0, deltaSeconds);
            if (held >= settings.Hold) return false;
            if (!tracked)
            {
                lost += dt;
                if (lost > settings.LostGrace) Reset();
                return false;
            }
            lost = 0;
            if (!Within(palm, eye, ahead)) { Reset(); return false; }
            if (samples == 0 || Vector3.Distance(palm, settled) > settings.Still)
            {
                // The hand has only just arrived, or moved: the hold begins again from here.
                settled = palm; sum = palm; samples = 1; held = 0;
                return false;
            }
            sum += palm; samples++;
            held += dt;
            return held >= settings.Hold;
        }

        bool Within(Vector3 palm, Vector3 eye, Vector3 ahead)
        {
            var level = new Vector3(ahead.X, 0, ahead.Z);
            if (level.LengthSquared() < 1e-6f) return false;
            level = Vector3.Normalize(level);
            var offset = palm - eye;
            var forward = offset.X * level.X + offset.Z * level.Z;
            var aside = Math.Abs(offset.X * level.Z - offset.Z * level.X);
            var below = -offset.Y;
            return forward >= settings.NearestAhead && forward <= settings.FarthestAhead && aside <= settings.Aside
                && below >= settings.LeastBelow && below <= settings.MostBelow;
        }
    }
}
