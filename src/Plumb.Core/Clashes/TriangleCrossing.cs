using System;
using System.Numerics;

namespace Plumb.Core.Clashes
{
    /// <summary>
    /// How far two triangles pass through each other. Each triangle is measured against the other's plane: a
    /// triangle that reaches across it by d on one side and e on the other crosses it by min(d, e), so a wall
    /// standing on a slab crosses it by nothing. Two triangles cross when each crosses the other's plane and the
    /// two crossings meet on the line where the planes cut; the depth is the shallower of the two.
    /// </summary>
    public static class TriangleCrossing
    {
        private const float DegenerateArea = 1e-12f;

        /// <returns>The depth in metres, or 0 when the triangles do not pass through each other.</returns>
        public static float Depth(Triangle a, Triangle b)
        {
            if (!TryNormal(a, out var normalA) || !TryNormal(b, out var normalB))
            {
                return 0;
            }

            var distancesA = DistancesTo(a, b.A, normalB);
            var depthA = Crossing(distancesA);
            if (depthA <= 0)
            {
                return 0;
            }

            var distancesB = DistancesTo(b, a.A, normalA);
            var depthB = Crossing(distancesB);
            if (depthB <= 0)
            {
                return 0;
            }

            var line = Vector3.Cross(normalA, normalB);
            var (minA, maxA) = Span(a, distancesA, line);
            var (minB, maxB) = Span(b, distancesB, line);
            return maxA >= minB && maxB >= minA ? Math.Min(depthA, depthB) : 0;
        }

        private static bool TryNormal(Triangle triangle, out Vector3 normal)
        {
            var cross = Vector3.Cross(triangle.B - triangle.A, triangle.C - triangle.A);
            var length = cross.Length();
            normal = length > DegenerateArea ? cross / length : Vector3.Zero;
            return length > DegenerateArea;
        }

        private static Vector3 DistancesTo(Triangle triangle, Vector3 pointOnPlane, Vector3 normal) => new Vector3(
            Vector3.Dot(triangle.A - pointOnPlane, normal),
            Vector3.Dot(triangle.B - pointOnPlane, normal),
            Vector3.Dot(triangle.C - pointOnPlane, normal));

        /// <summary>How far the triangle reaches to the nearer side of the plane its distances are measured to.</summary>
        private static float Crossing(Vector3 distances)
        {
            var above = Math.Max(0, Math.Max(distances.X, Math.Max(distances.Y, distances.Z)));
            var below = Math.Max(0, -Math.Min(distances.X, Math.Min(distances.Y, distances.Z)));
            return Math.Min(above, below);
        }

        /// <summary>Where along <paramref name="line"/> the triangle crosses the other plane.</summary>
        private static (float Min, float Max) Span(Triangle triangle, Vector3 distances, Vector3 line)
        {
            var min = float.MaxValue;
            var max = float.MinValue;

            void Add(Vector3 point)
            {
                var along = Vector3.Dot(point, line);
                min = Math.Min(min, along);
                max = Math.Max(max, along);
            }

            void Edge(Vector3 from, float fromDistance, Vector3 to, float toDistance)
            {
                if (fromDistance == 0)
                {
                    Add(from);
                }
                else if (fromDistance * toDistance < 0)
                {
                    Add(from + (to - from) * (fromDistance / (fromDistance - toDistance)));
                }
            }

            Edge(triangle.A, distances.X, triangle.B, distances.Y);
            Edge(triangle.B, distances.Y, triangle.C, distances.Z);
            Edge(triangle.C, distances.Z, triangle.A, distances.X);
            return (min, max);
        }
    }
}
