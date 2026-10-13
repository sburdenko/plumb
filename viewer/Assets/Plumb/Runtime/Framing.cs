using UnityEngine;

namespace Plumb.Viewer
{
    /// <summary>How far a camera must stand to see a box entirely.</summary>
    public static class Framing
    {
        private const float MinRadius = 0.01f;

        /// <param name="rotation">Where the camera looks: its forward axis points at the box's centre.</param>
        /// <param name="aspect">Width divided by height of the view.</param>
        /// <returns>Distance from the box's centre to the camera at which the box's corners just fit.</returns>
        public static float Distance(Bounds bounds, Quaternion rotation, float verticalFovDegrees, float aspect)
        {
            var halfFov = verticalFovDegrees * 0.5f * Mathf.Deg2Rad;
            var tanVertical = Mathf.Tan(halfFov);
            var tanHorizontal = tanVertical * aspect;
            var right = rotation * Vector3.right;
            var up = rotation * Vector3.up;
            var forward = rotation * Vector3.forward;

            var distance = MinRadius / Mathf.Sin(halfFov);
            foreach (var corner in Corners(bounds))
            {
                // A corner at depth (distance + along) is inside the view while its offsets stay within the frustum.
                var along = Vector3.Dot(corner, forward);
                var horizontal = Mathf.Abs(Vector3.Dot(corner, right)) / tanHorizontal - along;
                var vertical = Mathf.Abs(Vector3.Dot(corner, up)) / tanVertical - along;
                distance = Mathf.Max(distance, horizontal, vertical);
            }

            return distance;
        }

        private static Vector3[] Corners(Bounds bounds)
        {
            var e = bounds.extents;
            return new[]
            {
                new Vector3(-e.x, -e.y, -e.z), new Vector3(e.x, -e.y, -e.z),
                new Vector3(-e.x, e.y, -e.z), new Vector3(e.x, e.y, -e.z),
                new Vector3(-e.x, -e.y, e.z), new Vector3(e.x, -e.y, e.z),
                new Vector3(-e.x, e.y, e.z), new Vector3(e.x, e.y, e.z),
            };
        }
    }
}
