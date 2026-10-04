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
}
