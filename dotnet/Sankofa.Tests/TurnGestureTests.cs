using System;
using System.Numerics;
using NUnit.Framework;
using Sankofa.Interaction;

namespace Sankofa.Tests
{
    public sealed class TurnGestureTests
    {
        const double Frame = 1.0 / 72;

        /// <summary>Moves a hand in a straight line at a steady speed and reports what the gesture said on the way.</summary>
        static (int Turned, float PeakLean, int Frames) Move(TurnGesture gesture, Vector3 from, Vector3 to, double seconds, bool tracked = true)
        {
            var frames = Math.Max(1, (int)Math.Round(seconds / Frame));
            var turned = 0; var peak = 0f; var used = 0;
            for (var i = 0; i <= frames; i++)
            {
                var result = gesture.Update(tracked, Vector3.Lerp(from, to, (float)i / frames), Frame);
                if (Math.Abs(gesture.Lean) > Math.Abs(peak)) peak = gesture.Lean;
                if (result != 0 && turned == 0) { turned = result; used = i; }
            }
            return (turned, peak, used);
        }
        static Vector3 At(float x, float y = 0.30f, float z = -0.25f) => new Vector3(x, y, z);

        [Test]
        public void ASidewaysSweepTurnsTheStageOnceTowardTheHand()
        {
            var right = Move(new TurnGesture(), At(-0.15f), At(0.15f), 0.45);
            Assert.That(right.Turned, Is.EqualTo(1));
            var left = Move(new TurnGesture(), At(0.15f), At(-0.15f), 0.45);
            Assert.That(left.Turned, Is.EqualTo(-1));

            var gesture = new TurnGesture();
            var first = Move(gesture, At(-0.2f), At(0.2f), 0.5);
            Assert.That(first.Turned, Is.EqualTo(1));
            Assert.That(gesture.Lean, Is.EqualTo(0), "the stage has given way");
            Assert.That(Move(gesture, At(0.2f), At(-0.2f), 0.5).Turned, Is.EqualTo(0), "one sweep is one turn: the hand coming back does not turn it again");
            Move(gesture, At(-0.2f), At(-0.2f), 1.2);
            Assert.That(Move(gesture, At(-0.2f), At(0.2f), 0.5).Turned, Is.EqualTo(1), "after the stage has turned it can be turned again");
        }

        [Test]
        public void TheStageLeansWithTheHandBeforeItGivesWay()
        {
            var gesture = new TurnGesture();
            var part = Move(gesture, At(0f), At(0.10f), 0.25);
            Assert.That(part.Turned, Is.EqualTo(0));
            Assert.That(gesture.Lean, Is.EqualTo(0.5f).Within(0.03f), "half the travel is half the lean, toward the hand");
            Move(gesture, At(0.10f), At(0.10f), 0.9);
            Assert.That(gesture.Lean, Is.EqualTo(0).Within(0.01f), "a hand that stops lets the stage settle back");
            var back = new TurnGesture();
            Move(back, At(0f), At(-0.10f), 0.25);
            Assert.That(back.Lean, Is.EqualTo(-0.5f).Within(0.03f));
        }

        [Test]
        public void ReachingLiftingDriftingAndHoldingStillDoNothing()
        {
            var cases = new (string Name, Vector3 From, Vector3 To, double Seconds)[]
            {
                ("a slow drift", At(-0.2f), At(0.2f), 2.5),
                ("holding still", At(0.1f), At(0.1f), 2.0),
                ("reaching forward to the stage", At(-0.05f, 0.30f, -0.45f), At(0.10f, 0.30f, -0.05f), 0.5),
                ("reaching diagonally toward the fire", At(0.05f, 0.25f, -0.45f), At(-0.25f, 0.25f, -0.10f), 0.5),
                ("lifting a hand from the lap", At(-0.10f, 0.17f), At(0.05f, 0.55f), 0.4),
                ("a sweep too short to mean it", At(-0.08f), At(0.08f), 0.3),
                ("moving low across the caption card to another button", At(-0.25f, 0.03f, -0.35f), At(0.25f, 0.03f, -0.35f), 0.4),
                ("a sweep beyond the stage", At(-0.2f, 0.30f, 0.40f), At(0.2f, 0.30f, 0.40f), 0.4),
                ("a sweep far to one side", At(0.55f), At(0.95f), 0.4),
            };
            foreach (var (name, from, to, seconds) in cases)
            {
                var gesture = new TurnGesture();
                Assert.That(Move(gesture, from, to, seconds).Turned, Is.EqualTo(0), name);
            }
            var wobble = new TurnGesture();
            for (var i = 0; i < 6; i++)
                Assert.That(Move(wobble, At(i % 2 == 0 ? -0.07f : 0.07f), At(i % 2 == 0 ? 0.07f : -0.07f), 0.2).Turned, Is.EqualTo(0), "a hand wobbling in place");
        }

        [Test]
        public void TrackingMayFlickerButALostHandStartsAgain()
        {
            var gesture = new TurnGesture();
            Move(gesture, At(-0.15f), At(-0.03f), 0.18);
            Move(gesture, At(-0.02f), At(0.02f), 0.06, tracked: false);
            Assert.That(Move(gesture, At(0.03f), At(0.15f), 0.18).Turned, Is.EqualTo(1), "a flicker shorter than the grace keeps the sweep");

            var lost = new TurnGesture();
            Move(lost, At(-0.15f), At(0.02f), 0.25);
            Move(lost, At(0.02f), At(0.02f), 0.4, tracked: false);
            Assert.That(lost.Lean, Is.EqualTo(0));
            Assert.That(Move(lost, At(0.03f), At(0.12f), 0.15).Turned, Is.EqualTo(0), "what came before a real loss does not count");

            var reset = new TurnGesture();
            Move(reset, At(-0.15f), At(0.02f), 0.25);
            reset.Reset();
            Assert.That(reset.Lean, Is.EqualTo(0));
            Assert.That(Move(reset, At(0.03f), At(0.12f), 0.15).Turned, Is.EqualTo(0));
        }

        [Test]
        public void AHandAtTheStageIsKnownBeforeItMoves()
        {
            // The fire holds its own turn while a hand is where a sweep would be read.
            var gesture = new TurnGesture();
            Assert.That(gesture.Present, Is.False);
            gesture.Update(true, At(0.1f), Frame);
            Assert.That(gesture.Present, Is.True, "a still hand in front of the stage");
            gesture.Update(true, At(0.1f, 0.02f), Frame);
            Assert.That(gesture.Present, Is.False, "a hand down at the page's buttons is not at the stage");
            gesture.Update(false, At(0.1f), Frame);
            Assert.That(gesture.Present, Is.False, "an untracked hand");
            gesture.Update(true, At(0.1f), Frame);
            gesture.Reset();
            Assert.That(gesture.Present, Is.False);
            // It is still known while the stage rests after a turn.
            Move(gesture, At(-0.15f), At(0.15f), 0.45);
            gesture.Update(true, At(0.15f), Frame);
            Assert.That(gesture.Present, Is.True);
        }

        [Test]
        public void AGentleDeliberateSweepIsEnoughAndDirectionFollowsTheHand()
        {
            // Twenty centimetres in just under the time allowed: an unhurried sweep still turns the stage.
            Assert.That(Move(new TurnGesture(), At(-0.11f), At(0.11f), 0.62).Turned, Is.EqualTo(1));
            // A sweep low across the front of the stage, at the height of the painted figures, counts.
            Assert.That(Move(new TurnGesture(), At(-0.15f, 0.10f, -0.30f), At(0.15f, 0.10f, -0.30f), 0.45).Turned, Is.EqualTo(1));
            // A little forward or upward drift within a sideways sweep is forgiven.
            Assert.That(Move(new TurnGesture(), At(-0.14f, 0.28f, -0.28f), At(0.14f, 0.34f, -0.20f), 0.45).Turned, Is.EqualTo(1));
            var custom = new TurnGesture(new TurnSettings { Travel = 0.30f });
            Assert.That(Move(custom, At(-0.12f), At(0.12f), 0.4).Turned, Is.EqualTo(0));
            Assert.That(Move(new TurnGesture(new TurnSettings { Travel = 0.30f }), At(-0.17f), At(0.17f), 0.5).Turned, Is.EqualTo(1));
        }
    }
}
