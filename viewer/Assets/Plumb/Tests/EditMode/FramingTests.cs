using NUnit.Framework;
using UnityEngine;

namespace Plumb.Viewer.Tests
{
    public sealed class FramingTests
    {
        private const float VerticalFov = 60f;

        [Test]
        public void ACubeFitsWhenItsNearestCornersTouchTheFrameEdges()
        {
            var cube = new Bounds(Vector3.zero, new Vector3(2f, 2f, 2f));

            var distance = Framing.Distance(cube, Quaternion.identity, VerticalFov, aspect: 1f);

            // The corners nearest the camera are 1 unit closer than the centre and 1 unit off-axis.
            Assert.That(distance, Is.EqualTo(1f / Mathf.Tan(30f * Mathf.Deg2Rad) + 1f).Within(0.001f));
        }

        [Test]
        public void AWideFlatModelSeenFromTheFrontIsLimitedByItsWidth()
        {
            var wall = new Bounds(Vector3.zero, new Vector3(20f, 2f, 2f));

            var distance = Framing.Distance(wall, Quaternion.identity, VerticalFov, aspect: 2f);

            var horizontalHalfTan = 2f * Mathf.Tan(30f * Mathf.Deg2Rad);
            Assert.That(distance, Is.EqualTo(10f / horizontalHalfTan + 1f).Within(0.001f));
        }

        [Test]
        public void ALongModelSeenEndOnIsLimitedByItsHeightAndItsNearEnd()
        {
            var wall = new Bounds(Vector3.zero, new Vector3(20f, 2f, 2f));
            var endOn = Quaternion.Euler(0f, 90f, 0f);

            var distance = Framing.Distance(wall, endOn, VerticalFov, aspect: 2f);

            Assert.That(distance, Is.EqualTo(1f / Mathf.Tan(30f * Mathf.Deg2Rad) + 10f).Within(0.001f));
        }

        [Test]
        public void ALongModelNeedsLessDistanceThanItsBoundingSphereWould()
        {
            var building = new Bounds(Vector3.zero, new Vector3(200f, 12f, 40f));
            var sphere = building.extents.magnitude / Mathf.Sin(30f * Mathf.Deg2Rad);

            var distance = Framing.Distance(building, Quaternion.Euler(30f, 45f, 0f), VerticalFov, aspect: 1.6f);

            Assert.That(distance, Is.LessThan(sphere * 0.75f));
        }

        [Test]
        public void ABoundsWithNoSizeStillGetsADistanceToLookFrom()
        {
            var distance = Framing.Distance(new Bounds(), Quaternion.identity, VerticalFov, aspect: 1.6f);

            Assert.That(distance, Is.GreaterThan(0f));
        }
    }
}
