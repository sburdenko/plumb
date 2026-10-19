using System.Collections.Generic;
using System.Linq;

namespace Plumb.Core.Clashes
{
    /// <summary>The surface of one element: every triangle of its geometry, placed where it stands in the model.</summary>
    public sealed record ElementShape
    {
        public ElementShape(string globalId, IReadOnlyList<Triangle> triangles)
        {
            GlobalId = globalId;
            Triangles = triangles;
            Bounds = triangles.Aggregate(Box.Empty, (box, triangle) => box.Union(triangle.Bounds));
        }

        public string GlobalId { get; }

        public IReadOnlyList<Triangle> Triangles { get; }

        public Box Bounds { get; }
    }
}
