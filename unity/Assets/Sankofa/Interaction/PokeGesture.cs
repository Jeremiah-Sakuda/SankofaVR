using System;

namespace Sankofa.Interaction
{
    /// <summary>
    /// Press logic for one fingertip on one button. The fingertip must first be clearly in front
    /// of the face (withdrawn), then reach the face inside the button to press once. Reaching the
    /// face outside the button, or sliding in sideways, disarms without pressing.
    /// </summary>
    public sealed class PokeGesture
    {
        readonly double pressDepth;
        readonly double releaseDepth;
        readonly double minInterval;
        bool armed;
        double lastPress = double.NegativeInfinity;

        public PokeGesture(double pressDepth = 0.0, double releaseDepth = 0.025, double minIntervalSeconds = 0.3)
        {
            if (!(releaseDepth > pressDepth)) throw new ArgumentException("Release depth must lie in front of the press depth");
            this.pressDepth = pressDepth;
            this.releaseDepth = releaseDepth;
            minInterval = minIntervalSeconds;
        }

        public bool Armed => armed;

        /// <summary>
        /// Updates with the fingertip's depth in front of the face (positive toward the user) and
        /// whether it lies inside the button. Returns true on the frame the button is pressed.
        /// </summary>
        public bool Update(double depth, bool inside, double now)
        {
            if (double.IsNaN(depth))
            {
                armed = false;
                return false;
            }
            if (depth > releaseDepth)
            {
                armed = true;
                return false;
            }
            if (depth > pressDepth) return false;

            var pressed = armed && inside && now - lastPress >= minInterval;
            armed = false;
            if (pressed) lastPress = now;
            return pressed;
        }

        /// <summary>The fingertip disappeared: it must withdraw again before pressing.</summary>
        public void Lost() => armed = false;
    }
}
