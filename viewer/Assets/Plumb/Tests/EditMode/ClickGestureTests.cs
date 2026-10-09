using NUnit.Framework;
using UnityEngine;

namespace Plumb.Viewer.Tests
{
    public sealed class ClickGestureTests
    {
        [Test]
        public void ReleaseInPlaceIsAClick()
        {
            var gesture = new ClickGesture();
            gesture.Press(new Vector2(100, 100));

            Assert.That(gesture.ReleaseIsClick(new Vector2(102, 101)), Is.True);
        }

        [Test]
        public void ReleaseAfterDraggingIsNotAClick()
        {
            var gesture = new ClickGesture();
            gesture.Press(new Vector2(100, 100));

            Assert.That(gesture.ReleaseIsClick(new Vector2(140, 100)), Is.False);
        }

        [Test]
        public void ReleaseWithoutPressIsNotAClick()
        {
            Assert.That(new ClickGesture().ReleaseIsClick(Vector2.zero), Is.False);
        }

        [Test]
        public void EachPressCountsOnce()
        {
            var gesture = new ClickGesture();
            gesture.Press(Vector2.zero);
            gesture.ReleaseIsClick(Vector2.zero);

            Assert.That(gesture.ReleaseIsClick(Vector2.zero), Is.False);
        }
    }
}
