using UnityEngine;

namespace Plumb.Viewer
{
    /// <summary>
    /// Tells a click from a drag: the left button both selects and orbits, so a press only counts as a click
    /// when the pointer barely moved before release.
    /// </summary>
    public sealed class ClickGesture
    {
        private const float MaxClickTravel = 4f;

        private Vector2 _pressedAt;
        private bool _pressed;

        public void Press(Vector2 position)
        {
            _pressedAt = position;
            _pressed = true;
        }

        public bool ReleaseIsClick(Vector2 position)
        {
            var wasPressed = _pressed;
            _pressed = false;
            return wasPressed && (position - _pressedAt).sqrMagnitude <= MaxClickTravel * MaxClickTravel;
        }
    }
}
