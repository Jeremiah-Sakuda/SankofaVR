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
    }
}
