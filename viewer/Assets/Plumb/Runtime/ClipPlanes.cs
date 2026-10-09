using UnityEngine;

namespace Plumb.Viewer
{
    /// <summary>
    /// Near and far planes that keep the whole model visible from wherever the camera is,
    /// including after framing one small element and zooming far out.
    /// </summary>
    public static class ClipPlanes
    {
        private const float Margin = 1.2f;
        private const float NearShareOfDistance = 0.01f;
        private const float MinNear = 0.01f;

        public static (float Near, float Far) For(float distance, Vector3 pivot, Bounds scene)
        {
            var farthest = distance + Vector3.Distance(pivot, scene.center) + scene.extents.magnitude;
            var near = Mathf.Max(distance * NearShareOfDistance, MinNear);
            return (near, Mathf.Max(farthest * Margin, near * 10f));
        }
    }
}
