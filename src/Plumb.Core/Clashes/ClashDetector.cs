using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;

namespace Plumb.Core.Clashes
{
    /// <summary>
    /// Finds elements of one group whose surfaces pass through elements of another. A tree over the second group's
    /// bounding boxes finds the pairs worth a look, and pairs whose boxes overlap by no more than the tolerance are
    /// dropped; a tree over each element's triangles finds the triangle pairs worth testing;
    /// <see cref="TriangleCrossing"/> says how deep they cross.
    /// </summary>
    public static class ClashDetector
    {
        /// <param name="tolerance">
        /// Crossings no deeper than this, in metres, are joins rather than clashes: a wall sunk a few millimetres
        /// into the slab it stands on.
        /// </param>
        /// <returns>Each clashing pair once, deepest first.</returns>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
        public static IReadOnlyList<Clash> Find(
            IReadOnlyList<ElementShape> first,
            IReadOnlyList<ElementShape> second,
            float tolerance,
            CancellationToken cancellationToken)
        {
            var secondTree = BoxTree.Build(second.Select(shape => shape.Bounds).ToList());
            var surfaces = new Dictionary<string, BoxTree>(StringComparer.Ordinal);
            var compared = new HashSet<(string, string)>();
            var clashes = new List<Clash>();
            var candidates = new List<int>();

            foreach (var a in first)
            {
                cancellationToken.ThrowIfCancellationRequested();
                candidates.Clear();
                secondTree.Query(a.Bounds, candidates);
                foreach (var b in candidates.Select(index => second[index]))
                {
                    if (a.GlobalId == b.GlobalId || !compared.Add(PairKey(a.GlobalId, b.GlobalId)))
                    {
                        continue;
                    }

                    var overlap = Thinnest(a.Bounds.Intersect(b.Bounds).Size);
                    if (overlap <= tolerance)
                    {
                        continue;
                    }

                    var (crossing, location) = Deepest(a, b, surfaces);
                    var penetration = Math.Min(crossing, overlap);
                    if (penetration > tolerance)
                    {
                        clashes.Add(new Clash(a.GlobalId, b.GlobalId, penetration, location));
                    }
                }
            }

            return clashes.OrderByDescending(clash => clash.Penetration).ToList();
        }

        /// <summary>The deepest crossing of any triangle of one element with any triangle of the other.</summary>
        private static (float Penetration, Vector3 Location) Deepest(ElementShape a, ElementShape b, Dictionary<string, BoxTree> surfaces)
        {
            var (small, large) = a.Triangles.Count <= b.Triangles.Count ? (a, b) : (b, a);
            var surface = SurfaceOf(large, surfaces);
            var meeting = small.Bounds.Intersect(large.Bounds);
            var deepest = (Penetration: 0f, Location: meeting.Center);
            var hits = new List<int>();

            foreach (var triangle in small.Triangles)
            {
                var bounds = triangle.Bounds;
                if (!bounds.Overlaps(meeting))
                {
                    continue;
                }

                hits.Clear();
                surface.Query(bounds, hits);
                foreach (var other in hits.Select(index => large.Triangles[index]))
                {
                    var depth = TriangleCrossing.Depth(triangle, other);
                    if (depth > deepest.Penetration)
                    {
                        deepest = (depth, bounds.Intersect(other.Bounds).Center);
                    }
                }
            }

            return deepest;
        }

        private static BoxTree SurfaceOf(ElementShape shape, Dictionary<string, BoxTree> surfaces)
        {
            if (!surfaces.TryGetValue(shape.GlobalId, out var tree))
            {
                tree = BoxTree.Build(shape.Triangles.Select(triangle => triangle.Bounds).ToList());
                surfaces.Add(shape.GlobalId, tree);
            }

            return tree;
        }

        /// <summary>
        /// Moving one element this far along the thinnest side of the overlap of the two boxes parts them, so no
        /// clash between them is deeper.
        /// </summary>
        private static float Thinnest(Vector3 size) => Math.Min(size.X, Math.Min(size.Y, size.Z));

        private static (string, string) PairKey(string a, string b) => string.CompareOrdinal(a, b) <= 0 ? (a, b) : (b, a);
    }
}
