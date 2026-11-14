using NUnit.Framework;
using Sankofa.Runtime.Personal;
using UnityEngine;

namespace Sankofa.Tests
{
    public sealed class StoryTablePlacementTests
    {
        [TestCase(0f)]
        [TestCase(45f)]
        [TestCase(135f)]
        public void ObliqueApproachKeepsEveryDeckCornerInsideANonSquareTable(float yaw)
        {
            var surface = Quaternion.Euler(0, yaw, 0) * Quaternion.Euler(-90, 0, 0);
            var rotation = StoryTablePlacement.RotationAlongSurface(surface, Vector3.forward);
            const float width = 1f, depth = .6f;
            var scale = Mathf.Min((width - .06f) / 1.25f, (depth - .06f) / .72f);
            foreach (var x in new[] { -1f, 1f })
                foreach (var z in new[] { -1f, 1f })
                {
                    var corner = rotation * new Vector3(x * 1.25f * scale / 2, 0, z * .72f * scale / 2);
                    Assert.That(Mathf.Abs(Vector3.Dot(corner, surface * Vector3.right)), Is.LessThanOrEqualTo(width / 2));
                    Assert.That(Mathf.Abs(Vector3.Dot(corner, surface * Vector3.up)), Is.LessThanOrEqualTo(depth / 2));
                }
            Assert.That(Vector3.Dot(rotation * Vector3.forward, Vector3.forward), Is.GreaterThanOrEqualTo(-.001f));
        }

        /// <summary>A scanned tabletop lying level: its own forward axis points up, its first axis along world X.</summary>
        static readonly Quaternion Level = Quaternion.Euler(-90, 0, 0);

        [Test]
        public void TheStoryFacesAwayFromWhicheverEdgeTheVisitorSitsAt()
        {
            // A table 1.6 m along world X and 0.8 m along world Z, its top 0.74 m up and its middle 0.9 m ahead.
            var top = new Vector3(0, 0.74f, 0.9f);
            var seats = new (string Name, Vector3 Eye, Vector3 Facing, bool Sideways)[]
            {
                ("at the long near edge, off to one side", new Vector3(0.3f, 1.2f, 0.2f), Vector3.forward, false),
                ("at the long far edge", new Vector3(-0.5f, 1.2f, 1.6f), Vector3.back, false),
                ("at the left end", new Vector3(-1.1f, 1.2f, 0.9f), Vector3.right, true),
                ("at the right end", new Vector3(1.1f, 1.2f, 1.0f), Vector3.left, true),
            };
            foreach (var seat in seats)
                foreach (var glance in new[] { -50f, 0f, 50f })
                {
                    // Where the visitor sits decides, however far their head is turned at that moment.
                    var rotation = StoryTablePlacement.FacingAcrossTable(Level, top, 1.6f, 0.8f, seat.Eye, Quaternion.Euler(0, glance, 0) * seat.Facing, out var sideways);
                    Assert.That(Vector3.Angle(rotation * Vector3.forward, seat.Facing), Is.LessThan(0.1f), seat.Name);
                    Assert.That(sideways, Is.EqualTo(seat.Sideways), seat.Name);
                }
            // At a corner, beyond two edges at once, the side the visitor is looking across is theirs.
            var corner = new Vector3(-1.0f, 1.2f, 0.3f);
            Assert.That(Vector3.Angle(StoryTablePlacement.FacingAcrossTable(Level, top, 1.6f, 0.8f, corner, new Vector3(0.3f, -0.4f, 1), out var alongCorner) * Vector3.forward, Vector3.forward), Is.LessThan(0.1f));
            Assert.That(alongCorner, Is.False);
            Assert.That(Vector3.Angle(StoryTablePlacement.FacingAcrossTable(Level, top, 1.6f, 0.8f, corner, new Vector3(1, -0.4f, 0.2f), out var acrossCorner) * Vector3.forward, Vector3.right), Is.LessThan(0.1f));
            Assert.That(acrossCorner, Is.True);
        }

        [TestCase(0f)]
        [TestCase(30f)]
        [TestCase(90f)]
        [TestCase(200f)]
        public void TheStorySitsInFrontOfTheVisitorAndWhollyOnTheTable(float yaw)
        {
            const float sizeAcross = 1.8f, sizeAlong = 0.9f;
            var turn = Quaternion.Euler(0, yaw, 0);
            var surface = turn * Level;
            var top = new Vector3(0.4f, 0.74f, 1.1f);
            // Seats given in the table's own frame, then turned with it: along a long edge and at an end.
            var seats = new[] { new Vector3(-0.55f, 0.45f, -0.75f), new Vector3(0.7f, 0.45f, -0.7f), new Vector3(0.1f, 0.45f, 0.72f), new Vector3(-1.2f, 0.45f, 0.1f), new Vector3(1.25f, 0.45f, -0.2f) };
            foreach (var seat in seats)
            {
                var eye = top + turn * seat;
                StoryTablePlacement.SeatOnTable(surface, top, sizeAcross, sizeAlong, eye, top - eye, out var centre, out var rotation, out var width, out var depth);
                var scale = StoryTableView.SurfaceScale(width, depth);
                Assert.That(centre.y, Is.EqualTo(top.y).Within(0.0001f), "the story stands on the surface");
                foreach (var x in new[] { -1f, 1f })
                    foreach (var z in new[] { -1f, 1f })
                    {
                        var corner = centre + rotation * new Vector3(x * StoryTableView.DeckWidth * scale / 2, 0, z * StoryTableView.DeckDepth * scale / 2) - top;
                        Assert.That(Mathf.Abs(Vector3.Dot(corner, surface * Vector3.right)), Is.LessThanOrEqualTo(sizeAcross / 2 + 0.0001f), seat + " at " + yaw);
                        Assert.That(Mathf.Abs(Vector3.Dot(corner, surface * Vector3.up)), Is.LessThanOrEqualTo(sizeAlong / 2 + 0.0001f), seat + " at " + yaw);
                    }
                // It faces away from the visitor, and its near side is the table's near edge: as close as the table allows.
                var toStory = Vector3.ProjectOnPlane(centre - eye, Vector3.up);
                Assert.That(Vector3.Dot(rotation * Vector3.forward, toStory.normalized), Is.GreaterThan(0.7f), seat + " at " + yaw);
                Assert.That(toStory.magnitude, Is.LessThan(Vector3.ProjectOnPlane(top - eye, Vector3.up).magnitude + 0.3f), "never pushed to the far side of a long table");
            }
        }

        [Test]
        public void ALongTableIsStillTheVisitorsWhenTheySitAtOneEndOfIt()
        {
            // A table 2.4 m long: its middle is more than a metre away, its nearest edge within reach.
            var top = new Vector3(0, 0.74f, 1.5f);
            var eye = new Vector3(0.2f, 1.2f, 0);
            var gap = StoryTablePlacement.GapToTable(Quaternion.Euler(0, 90, 0) * Level, top, 2.4f, 0.9f, eye);
            Assert.That(gap.magnitude, Is.EqualTo(0.3f).Within(0.001f));
            Assert.That(Vector3.Angle(gap, Vector3.forward), Is.LessThan(0.1f));
            Assert.That(StoryTablePlacement.GapToTable(Level, top, 2.4f, 0.9f, new Vector3(0.5f, 1.2f, 1.4f)).magnitude, Is.LessThan(0.0001f), "leaning over the table is no distance at all");
        }
    }
}
