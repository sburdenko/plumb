using NUnit.Framework;
using UnityEngine;

namespace Plumb.Viewer.Tests
{
    public sealed class ClipPlanesTests
    {
        private static readonly Bounds Building = new Bounds(Vector3.zero, new Vector3(9f, 8f, 27f));

        [Test]
        public void FarPlaneCoversTheWholeModelAfterFramingASmallElementAndZoomingOut()
        {
            var smallElementPivot = new Vector3(4f, 1f, 12f);

            var (_, far) = ClipPlanes.For(distance: 200f, smallElementPivot, Building);

            Assert.That(far, Is.GreaterThan(200f + Vector3.Distance(smallElementPivot, Building.center) + Building.extents.magnitude));
        }

        [Test]
        public void NearPlaneShrinksWhenCloseSoNearbyGeometryStaysVisible()
        {
            var (near, _) = ClipPlanes.For(distance: 0.5f, Vector3.zero, Building);

            Assert.That(near, Is.LessThan(0.05f));
        }

        [Test]
        public void FarIsAlwaysBeyondNear()
        {
            var (near, far) = ClipPlanes.For(distance: 0.01f, Vector3.zero, new Bounds());

            Assert.That(far, Is.GreaterThan(near));
        }
    }
}
