using System;
using System.Numerics;
using NUnit.Framework;
using Sankofa.Interaction;

namespace Sankofa.Tests
{
    public sealed class RestGestureTests
    {
        const double Frame = 1.0 / 72;
        static readonly Vector3 Eye = new Vector3(0.3f, 1.2f, -0.4f);
        static readonly Vector3 Ahead = Vector3.Normalize(new Vector3(0.6f, 0, 0.8f));
        static readonly Vector3 Right = new Vector3(Ahead.Z, 0, -Ahead.X);

        /// <summary>A point given the way a seated visitor would describe it: so far ahead, so far to the right, so far below the eyes.</summary>
        static Vector3 At(float ahead, float right = 0, float below = 0.45f) => Eye + Ahead * ahead + Right * right - Vector3.UnitY * below;

        static bool Hold(RestGesture gesture, Func<int, Vector3> palm, double seconds, bool tracked = true)
        {
            var rested = false;
            for (var i = 0; i * Frame < seconds; i++) rested |= gesture.Update(tracked, palm(i), Eye, Ahead, Frame);
            return rested;
        }

        [Test]
        public void AHandLaidStillOnATableShowsWhereItIs()
        {
            var gesture = new RestGesture();
            var table = At(0.45f, 0.12f, 0.47f);
            Assert.That(Hold(gesture, _ => table, 0.6), Is.False, "a touch in passing is not a rest");
            Assert.That(gesture.Progress, Is.EqualTo(0.6f).Within(0.05f), "the hold reports how far it has come");
            Assert.That(Hold(gesture, _ => table, 0.6), Is.True);
            Assert.That(Vector3.Distance(gesture.Place, table), Is.LessThan(0.001f));
            Assert.That(Hold(gesture, _ => table, 2.0), Is.False, "one rest is reported once");
            gesture.Reset();
            Assert.That(gesture.Progress, Is.EqualTo(0));
            Assert.That(Hold(gesture, _ => table, 1.2), Is.True, "after a reset another rest can be read");
        }

        [Test]
        public void TrackingJitterIsAveragedAndFlickerIsForgiven()
        {
            var gesture = new RestGesture();
            var table = At(0.5f);
            // A tracked hand on a surface trembles by a few millimetres.
            Vector3 Trembling(int i) => table + new Vector3((i % 3 - 1) * 0.004f, (i % 2 == 0 ? 1 : -1) * 0.003f, (i % 5 - 2) * 0.002f);
            Assert.That(Hold(gesture, Trembling, 0.5), Is.False);
            Assert.That(Hold(gesture, _ => default, 0.15, tracked: false), Is.False);
            Assert.That(Hold(gesture, Trembling, 0.7), Is.True, "a flicker shorter than the grace keeps the hold");
            Assert.That(Vector3.Distance(gesture.Place, table), Is.LessThan(0.004f), "the place is the mean of where the palm lay");

            var lost = new RestGesture();
            Hold(lost, _ => table, 0.8);
            Hold(lost, _ => default, 0.5, tracked: false);
            Assert.That(lost.Progress, Is.EqualTo(0), "a hand that is really lost starts again");
            Assert.That(Hold(lost, _ => table, 0.5), Is.False);
        }

        [Test]
        public void AMovingHandALapAndTheAirShowNothing()
        {
            var cases = new (string Name, Func<int, Vector3> Palm)[]
            {
                ("a hand sliding across the table", i => At(0.45f, -0.2f + i * 0.004f)),
                ("a hand lowering slowly onto the table", i => At(0.45f, 0, 0.30f + i * 0.0015f)),
                ("a hand resting in the lap", _ => At(0.22f, 0.1f, 0.55f)),
                ("a hand held at chest height close to the body", _ => At(0.25f, 0, 0.30f)),
                ("a hand raised near the face", _ => At(0.40f, 0, 0.05f)),
                ("a hand hanging beside the chair", _ => At(0.10f, 0.35f, 0.80f)),
                ("a hand far out to one side", _ => At(0.45f, 0.60f)),
                ("a hand stretched beyond a table's reach", _ => At(0.95f)),
                ("a hand on the floor", _ => At(0.5f, 0, 1.15f)),
            };
            foreach (var (name, palm) in cases)
                Assert.That(Hold(new RestGesture(), palm, 3.0), Is.False, name);
            var gesture = new RestGesture();
            Hold(gesture, _ => At(0.45f), 0.7);
            Assert.That(Hold(gesture, _ => At(0.45f, 0.10f), 0.7), Is.False, "moving to another spot starts the hold again there");
            Assert.That(Hold(gesture, _ => At(0.45f, 0.10f), 0.4), Is.True);
            Assert.That(Vector3.Distance(gesture.Place, At(0.45f, 0.10f)), Is.LessThan(0.001f));
        }

        [Test]
        public void HeightsFromACounterToALowTableAreAccepted()
        {
            foreach (var below in new[] { 0.20f, 0.45f, 0.70f, 0.93f })
                Assert.That(Hold(new RestGesture(), _ => At(0.5f, 0, below), 1.2), Is.True, below + " m below the eyes");
            // Facing is taken level: looking down at the table does not move where "ahead" is.
            var gesture = new RestGesture();
            var rested = false;
            var lookingDown = Vector3.Normalize(Ahead - Vector3.UnitY * 0.6f);
            for (var i = 0; i * Frame < 1.2; i++) rested |= gesture.Update(true, At(0.5f), Eye, lookingDown, Frame);
            Assert.That(rested, Is.True);
            var straightDown = new RestGesture();
            rested = false;
            for (var i = 0; i * Frame < 1.2; i++) rested |= straightDown.Update(true, At(0.5f), Eye, -Vector3.UnitY, Frame);
            Assert.That(rested, Is.False, "with no level facing there is no ahead");
        }
    }
}
