using System;

namespace Sankofa.Flow
{
    public enum RiverThresholdPhase { Opening, Choosing, Crossing, Riverside, Table }

    /// <summary>The prototype's presentation clock. It never changes story content or a tracked viewpoint.</summary>
    public sealed class RiverThresholdSequence
    {
        public const double OpeningSeconds = 32;
        public const double CrossingSeconds = 6;
        public RiverThresholdPhase Phase { get; private set; } = RiverThresholdPhase.Opening;
        public double Seconds { get; private set; }
        public bool ReducedMotion { get; set; }
        public bool HoldingStory => Phase == RiverThresholdPhase.Opening || Phase == RiverThresholdPhase.Crossing;

        public void Tick(double elapsed, bool paused)
        {
            if (paused || double.IsNaN(elapsed) || double.IsInfinity(elapsed) || elapsed <= 0) return;
            Seconds += Math.Min(elapsed, 0.1); // A resumed or stalled frame cannot skip an unread title or reveal.
            if (Phase == RiverThresholdPhase.Opening && Seconds >= OpeningSeconds) Choose();
            else if (Phase == RiverThresholdPhase.Crossing && Seconds >= CrossingSeconds) Enter(RiverThresholdPhase.Riverside);
        }

        public void Choose() { if (Phase == RiverThresholdPhase.Opening) Enter(RiverThresholdPhase.Choosing); }
        public bool RequestWater()
        {
            if (Phase != RiverThresholdPhase.Choosing && Phase != RiverThresholdPhase.Table) return false;
            Enter(RiverThresholdPhase.Crossing);
            return true;
        }
        /// <summary>Also cancels a pending crossing: ticking afterwards cannot start its story.</summary>
        public void StayAtTable() => Enter(RiverThresholdPhase.Table);
        public void ReturnToChoices() => Enter(RiverThresholdPhase.Choosing);
        public void ReplayOpening() { if (!HoldingStory) Enter(RiverThresholdPhase.Opening); }
        void Enter(RiverThresholdPhase phase) { Phase = phase; Seconds = 0; }
    }
}
