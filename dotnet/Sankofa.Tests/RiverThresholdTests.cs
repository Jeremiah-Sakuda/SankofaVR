using NUnit.Framework;
using Sankofa.Flow;

namespace Sankofa.Tests
{
    public sealed class RiverThresholdTests
    {
        static void Advance(RiverThresholdSequence sequence, double seconds, bool paused = false)
        { for (var i=0; i<seconds*100; i++) sequence.Tick(.01, paused); }

        [Test]
        public void AnOpeningCannotChooseOrStartAStoryOnTheVisitorsBehalf()
        {
            var sequence = new RiverThresholdSequence();
            Assert.That(sequence.RequestWater(), Is.False);
            Advance(sequence, 40);
            Assert.That(sequence.Phase, Is.EqualTo(RiverThresholdPhase.Choosing));
            Advance(sequence, 100);
            Assert.That(sequence.Phase, Is.EqualTo(RiverThresholdPhase.Choosing));
            Assert.That(sequence.HoldingStory, Is.False);
        }

        [Test]
        public void PauseAndAStalledFrameCannotConsumeTheReveal()
        {
            var sequence = new RiverThresholdSequence(); sequence.Choose(); sequence.RequestWater();
            Advance(sequence, 2);
            var before = sequence.Seconds;
            Advance(sequence, 60, true);
            Assert.That(sequence.Seconds, Is.EqualTo(before));
            sequence.Tick(500, false);
            Assert.That(sequence.Seconds, Is.EqualTo(before+.1).Within(.00001));
            Assert.That(sequence.HoldingStory, Is.True);
            Advance(sequence, 4);
            Assert.That(sequence.Phase, Is.EqualTo(RiverThresholdPhase.Riverside));
            Assert.That(sequence.RequestWater(), Is.False, "a held button cannot restart the completed crossing");
        }

        [Test]
        public void CancellingTheCrossingInvalidatesItsPendingCompletion()
        {
            var sequence = new RiverThresholdSequence(); sequence.Choose(); sequence.RequestWater();
            Advance(sequence, 3); sequence.ReturnToChoices(); Advance(sequence, 20);
            Assert.That(sequence.Phase, Is.EqualTo(RiverThresholdPhase.Choosing));
            Assert.That(sequence.HoldingStory, Is.False);
            Assert.That(sequence.RequestWater(), Is.True, "an explicit new choice may retry");
        }

        [Test]
        public void ReducedMotionPreservesTheReadingTimeAndChoice()
        {
            var sequence = new RiverThresholdSequence { ReducedMotion = true };
            Advance(sequence, 30);
            Assert.That(sequence.Phase, Is.EqualTo(RiverThresholdPhase.Opening));
            sequence.Choose(); sequence.RequestWater(); Advance(sequence, 6.1);
            Assert.That(sequence.Phase, Is.EqualTo(RiverThresholdPhase.Riverside));
            sequence.StayAtTable(); Advance(sequence, 10);
            Assert.That(sequence.Phase, Is.EqualTo(RiverThresholdPhase.Table));
        }
    }
}
