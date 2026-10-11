using System.Numerics;
using Sankofa.Interaction;
using NUnit.Framework;

namespace Sankofa.Tests
{
    public class PalmReceiveGestureTests
    {
        /// <summary>
        /// A hand with fingers pointing forward (+Z) and the wrist at <paramref name="wrist"/>. Palm down puts the
        /// thumb side toward −X for a right hand and +X for a left hand; turning the palm up mirrors that.
        /// </summary>
        static HandSample Hand(bool left, bool palmUp, Vector3 wrist, bool open = true)
        {
            var thumbSide = (left ? 1f : -1f) * (palmUp ? -1f : 1f);
            var curl = open ? 1f : 0.45f;
            return new HandSample
            {
                Tracked = true,
                IsLeft = left,
                Wrist = wrist,
                IndexProximal = wrist + new Vector3(0.025f * thumbSide, 0, 0.09f),
                MiddleProximal = wrist + new Vector3(0, 0, 0.092f),
                LittleProximal = wrist + new Vector3(-0.035f * thumbSide, -0.005f, 0.075f),
                IndexTip = wrist + new Vector3(0.03f * thumbSide, 0, 0.18f * curl),
                MiddleTip = wrist + new Vector3(0, 0, 0.19f * curl),
                RingTip = wrist + new Vector3(-0.02f * thumbSide, 0, 0.18f * curl),
            };
        }

        [Test]
        public void PalmNormalPointsOutOfThePalmForBothHands()
        {
            foreach (var left in new[] { false, true })
            {
                Assert.That(HandGeometry.PalmNormal(Hand(left, palmUp: true, Vector3.Zero)).Y, Is.GreaterThan(0.95f), left ? "left up" : "right up");
                Assert.That(HandGeometry.PalmNormal(Hand(left, palmUp: false, Vector3.Zero)).Y, Is.LessThan(-0.95f), left ? "left down" : "right down");
            }
        }

        [Test]
        public void OpennessSeparatesAnOpenHandFromAFist()
        {
            Assert.That(HandGeometry.Openness(Hand(false, true, Vector3.Zero)), Is.GreaterThan(1.8f));
            Assert.That(HandGeometry.Openness(Hand(false, true, Vector3.Zero, open: false)), Is.LessThan(1.1f));
        }

        static readonly Vector3 Ember = new Vector3(0, 1.0f, 0.45f);
        // Palm centre (wrist + middle knuckle) / 2 sits about 10 cm below the ember.
        static readonly Vector3 UnderEmber = new Vector3(0, 0.9f, 0.45f - 0.046f);

        [Test]
        public void AnOpenPalmHeldUnderTheEmberReceivesItOnce()
        {
            var g = new PalmReceiveGesture();
            var hand = Hand(false, true, UnderEmber);
            Assert.That(g.Update(hand, Ember, 0.2), Is.False, "the dwell is not complete");
            Assert.That(g.Progress, Is.GreaterThan(0.5f));
            Assert.That(g.Update(hand, Ember, 0.2), Is.True);
            Assert.That(g.Update(hand, Ember, 0.2), Is.False, "received once");
            Assert.That(g.Received, Is.True);
        }

        [Test]
        public void PassingAFistOrAPalmDownNeverTakesTheEmber()
        {
            foreach (var hand in new[] { Hand(false, false, UnderEmber), Hand(true, true, UnderEmber, open: false) })
            {
                var g = new PalmReceiveGesture();
                for (var i = 0; i < 20; i++) Assert.That(g.Update(hand, Ember, 0.1), Is.False);
                Assert.That(g.Proximity, Is.EqualTo(0), "no inviting feedback for a hand that is not offering");
            }
        }

        [Test]
        public void TheDwellRestartsWhenThePalmLeavesOrTrackingDrops()
        {
            var g = new PalmReceiveGesture();
            var under = Hand(true, true, UnderEmber);
            var aside = Hand(true, true, UnderEmber + new Vector3(0.3f, 0, 0));
            g.Update(under, Ember, 0.3);
            Assert.That(g.Update(aside, Ember, 0.3), Is.False);
            Assert.That(g.InPlace, Is.False);
            Assert.That(g.Proximity, Is.GreaterThan(0), "still close enough for local feedback");
            g.Update(under, Ember, 0.3);
            Assert.That(g.Update(new HandSample(), Ember, 0.3), Is.False, "untracked");
            Assert.That(g.Update(under, Ember, 0.3), Is.False, "dwell restarted after tracking loss");
            Assert.That(g.Update(under, Ember, 0.1), Is.True);
        }

        [Test]
        public void APalmAboveTheEmberDoesNotCount()
        {
            var g = new PalmReceiveGesture();
            var above = Hand(false, true, UnderEmber + new Vector3(0, 0.2f, 0));
            for (var i = 0; i < 10; i++) Assert.That(g.Update(above, Ember, 0.1), Is.False);
        }
    }

    public class PokeGestureTests
    {
        [Test]
        public void PressesOnceAfterWithdrawingAndReachingTheFace()
        {
            var g = new PokeGesture();
            Assert.That(g.Update(0.05, true, 0.0), Is.False);
            Assert.That(g.Update(0.01, true, 0.1), Is.False);
            Assert.That(g.Update(-0.002, true, 0.2), Is.True);
            Assert.That(g.Update(-0.01, true, 0.3), Is.False, "resting on the face does not repeat");
            Assert.That(g.Update(0.01, true, 0.9), Is.False, "a shallow lift is not enough to re-arm");
            Assert.That(g.Update(-0.001, true, 1.0), Is.False);
            g.Update(0.04, true, 1.1);
            Assert.That(g.Update(0.0, true, 1.2), Is.True);
        }

        [Test]
        public void SlidingInSidewaysOrPressingOutsideDoesNothing()
        {
            var g = new PokeGesture();
            g.Update(0.05, false, 0);
            Assert.That(g.Update(-0.005, false, 0.1), Is.False);
            Assert.That(g.Update(-0.005, true, 0.2), Is.False, "slid in behind the face");
            Assert.That(g.Armed, Is.False);
        }

        [Test]
        public void LostFingertipMustWithdrawAgainAndRapidRepeatsAreLimited()
        {
            var g = new PokeGesture(minIntervalSeconds: 0.3);
            g.Update(0.05, true, 0);
            g.Lost();
            Assert.That(g.Update(-0.001, true, 0.1), Is.False);
            g.Update(0.05, true, 0.2);
            Assert.That(g.Update(-0.001, true, 0.25), Is.True);
            g.Update(0.05, true, 0.3);
            Assert.That(g.Update(-0.001, true, 0.4), Is.False, "within the minimum interval");
        }
    }

    public class TouchGestureTests
    {
        static HandSample PointingAt(Vector3 fingertip) => new HandSample
        {
            Tracked = true,
            Wrist = fingertip - new Vector3(0, 0, 0.18f),
            IndexProximal = fingertip - new Vector3(-0.02f, 0, 0.09f),
            MiddleProximal = fingertip - new Vector3(0, 0, 0.088f),
            LittleProximal = fingertip - new Vector3(0.035f, 0, 0.1f),
            IndexTip = fingertip,
            MiddleTip = fingertip - new Vector3(0, 0.01f, 0.01f),
            RingTip = fingertip - new Vector3(0.02f, 0.01f, 0.02f),
        };

        static readonly Vector3 Rim = new Vector3(-0.2f, 0.62f, 0.5f);

        [Test]
        public void AFingertipRestingOnTheTargetTouchesItOnce()
        {
            var g = new TouchGesture();
            var hand = PointingAt(Rim + new Vector3(0.03f, 0.02f, 0));
            Assert.That(g.Update(hand, Rim, 0.2), Is.False);
            Assert.That(g.InPlace, Is.True);
            Assert.That(g.Update(hand, Rim, 0.2), Is.True);
            Assert.That(g.Update(hand, Rim, 0.2), Is.False, "touched once");
            Assert.That(g.Touched, Is.True);
        }

        [Test]
        public void AHandPassingThroughOrFarAwayDoesNotTouch()
        {
            var g = new TouchGesture();
            var near = PointingAt(Rim);
            var away = PointingAt(Rim + new Vector3(0, 0, -0.3f));
            Assert.That(g.Update(near, Rim, 0.1), Is.False);
            Assert.That(g.Update(away, Rim, 0.1), Is.False, "left before the dwell completed");
            Assert.That(g.InPlace, Is.False);
            Assert.That(g.Proximity, Is.GreaterThan(0).And.LessThan(0.5f), "close enough for feedback, not to touch");
            Assert.That(g.Update(near, Rim, 0.25), Is.False, "the dwell restarted");
            Assert.That(g.Update(new HandSample(), Rim, 1), Is.False);
            Assert.That(g.Proximity, Is.EqualTo(0));
        }
    }

    public class AttentionTrackerTests
    {
        static readonly Vector3 Head = new Vector3(0, 1.15f, 0);
        static readonly AttentionCandidate[] Objects =
        {
            new AttentionCandidate("bowl", new Vector3(-0.25f, 0.55f, 0.55f), 0.12f),
            new AttentionCandidate("door", new Vector3(0.1f, 1.1f, 4.6f), 0.7f),
        };

        static Vector3 Toward(string id) => Vector3.Normalize(System.Array.Find(Objects, c => c.Id == id).Position - Head);

        [Test]
        public void ARestingLookIsNoticedOnceAfterTheDwell()
        {
            var t = new AttentionTracker();
            string noticed = null;
            for (var i = 0; i < 13; i++) noticed ??= t.Update(Head, Toward("bowl"), Objects, 0.1);
            Assert.That(noticed, Is.Null, "1.3 s is a glance, not a look");
            Assert.That(t.Current, Is.EqualTo("bowl"));
            Assert.That(t.Update(Head, Toward("bowl"), Objects, 0.1), Is.EqualTo("bowl"));
            for (var i = 0; i < 30; i++) Assert.That(t.Update(Head, Toward("bowl"), Objects, 0.1), Is.Null, "noticed once per look");
        }

        [Test]
        public void AGlanceAwayKeepsTheLookButLookingElsewhereStartsAgain()
        {
            var t = new AttentionTracker();
            var up = new Vector3(0, 1, 0);
            for (var i = 0; i < 10; i++) t.Update(Head, Toward("bowl"), Objects, 0.1);
            for (var i = 0; i < 5; i++) t.Update(Head, up, Objects, 0.1);
            Assert.That(t.Current, Is.EqualTo("bowl"), "half a second away is still the same look");
            string noticed = null;
            for (var i = 0; i < 5 && noticed == null; i++) noticed = t.Update(Head, Toward("bowl"), Objects, 0.1);
            Assert.That(noticed, Is.EqualTo("bowl"));

            for (var i = 0; i < 10; i++) Assert.That(t.Update(Head, Toward("door"), Objects, 0.1), Is.Null);
            Assert.That(t.Current, Is.EqualTo("door"));
            for (var i = 0; i < 10; i++) t.Update(Head, up, Objects, 0.1);
            Assert.That(t.Current, Is.Null, "looking away for long ends the look");
        }

        [Test]
        public void NothingIsNoticedWhenFacingNothing()
        {
            var t = new AttentionTracker();
            for (var i = 0; i < 50; i++) Assert.That(t.Update(Head, new Vector3(1, 0, 0), Objects, 0.1), Is.Null);
            Assert.That(t.Update(Head, Vector3.Zero, Objects, 0.1), Is.Null);
            Assert.That(t.Progress, Is.EqualTo(0));
        }
    }

    public class PointerSelectTests
    {
        [Test]
        public void AReacquiredHeldPinchNeedsNeutralBeforeAnotherPress()
        {
            var s = new PointerSelect();
            s.Update(true, 1, "keep");
            s.Update(false, 1, "keep");
            Assert.That(s.Update(true, 1, "keep"), Is.Null);
            Assert.That(s.Update(true, 0, "keep"), Is.Null);
            s.Update(true, 1, "keep");
            Assert.That(s.Update(true, 0, "keep"), Is.EqualTo("keep"));
        }

        [Test]
        public void ASwitchFromHandToControllerCannotFinishAPinch()
        {
            var s = new PointerSelect();
            s.Update(true, 1, "keep", 1);
            Assert.That(s.Update(true, 1, "keep", 2), Is.Null);
            Assert.That(s.Update(true, 0, "keep", 2), Is.Null);
            s.Update(true, 1, "keep", 2);
            Assert.That(s.Update(true, 0, "keep", 2), Is.EqualTo("keep"));
        }

        [Test]
        public void APinchBegunAndReleasedOverOneButtonPressesItOnRelease()
        {
            var s = new PointerSelect();
            Assert.That(s.Update(true, 0.2, "begin"), Is.Null);
            Assert.That(s.Update(true, 0.9, "begin"), Is.Null, "nothing happens on the pinch itself");
            Assert.That(s.Held, Is.True);
            Assert.That(s.Update(true, 0.7, "begin"), Is.Null, "still held between the thresholds");
            Assert.That(s.Update(true, 0.3, "begin"), Is.EqualTo("begin"));
            Assert.That(s.Update(true, 0.3, "begin"), Is.Null, "one press per pinch");
        }

        [Test]
        public void SweepingAcrossButtonsOrLettingGoElsewherePressesNothing()
        {
            var s = new PointerSelect();
            s.Update(true, 1, "begin");
            s.Update(true, 1, "leave");
            Assert.That(s.Update(true, 0, "leave"), Is.Null);

            s.Update(true, 1, "begin");
            s.Update(true, 1, null);
            s.Update(true, 1, "begin");
            Assert.That(s.Update(true, 0, "begin"), Is.Null, "moving off cancels even if the pointer comes back");

            s.Update(true, 1, null);
            Assert.That(s.Update(true, 0, "begin"), Is.Null, "a pinch begun over nothing cannot land on a button");
        }

        [Test]
        public void LosingThePointerCancelsAHeldSelect()
        {
            var s = new PointerSelect();
            s.Update(true, 1, "begin");
            Assert.That(s.Update(false, 1, "begin"), Is.Null);
            Assert.That(s.Held, Is.False);
            Assert.That(s.Update(true, 0, "begin"), Is.Null, "a reappearing hand cannot finish a press it lost");
        }

        [Test]
        public void ThePressStrengthMustExceedTheReleaseStrength() =>
            Assert.That(() => new PointerSelect(0.4, 0.6), Throws.ArgumentException);
    }
}
