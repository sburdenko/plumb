using NUnit.Framework;
using UnityEngine;

namespace Plumb.Viewer.Tests
{
    public sealed class ElementPanelTests
    {
        private const float ScreenHeight = 800f;

        [Test]
        public void PointerOverThePanelIsInside()
        {
            var area = ElementPanel.Area(ScreenHeight);
            var pointerFromBottom = new Vector2(area.center.x, ScreenHeight - area.center.y);

            Assert.That(ElementPanel.Contains(pointerFromBottom, ScreenHeight), Is.True);
        }

        [Test]
        public void PointerAtTheTopOfTheScreenIsOutside()
        {
            Assert.That(ElementPanel.Contains(new Vector2(100f, ScreenHeight - 50f), ScreenHeight), Is.False);
        }
    }
}
