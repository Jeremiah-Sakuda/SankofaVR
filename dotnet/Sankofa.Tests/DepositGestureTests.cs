using NUnit.Framework;
using Sankofa.Interaction;
namespace Sankofa.Tests
{
    public class DepositGestureTests
    {
        [Test] public void OnlyAnOpenDwellThenReleaseDepositsOnce()
        {
            var g=new DepositGesture();Assert.That(g.Update(true,true,false,1),Is.False);
            Assert.That(g.Update(true,true,true,.3),Is.False);Assert.That(g.Update(true,true,false,.1),Is.False);
            g.Update(true,true,true,.5);Assert.That(g.Armed,Is.True);Assert.That(g.Update(true,true,false,.01),Is.True);
            Assert.That(g.Update(true,true,false,1),Is.False);
        }
        [TestCase(false,true)] [TestCase(true,false)]
        public void TrackingLossOrLeavingTheRecessCannotBecomeARelease(bool tracked,bool near)
        {
            var g=new DepositGesture();g.Update(true,true,true,.5);
            Assert.That(g.Update(tracked,near,false,.1),Is.False);Assert.That(g.Update(true,true,false,1),Is.False);
            Assert.That(g.Armed,Is.False);
        }
        [Test] public void PauseResetRequiresANewDeliberateDeposit()
        {
            var g=new DepositGesture();g.Update(true,true,true,.5);g.Reset();
            Assert.That(g.Update(true,true,false,1),Is.False);
        }
    }
}
