using System.Numerics;
using Plumb.Core.Clashes;

namespace Plumb.Tests;

/// <summary>Axis-aligned boxes as closed triangle meshes, the way IfcConvert writes a wall or a duct segment.</summary>
internal static class Cuboid
{
    public static ElementShape Shape(string globalId, Vector3 min, Vector3 max) => new(globalId, Triangles(min, max));

    public static ElementShape Shape(string globalId, (float X, float Y, float Z) min, (float X, float Y, float Z) max) =>
        Shape(globalId, new Vector3(min.X, min.Y, min.Z), new Vector3(max.X, max.Y, max.Z));

    public static IReadOnlyList<Triangle> Triangles(Vector3 min, Vector3 max)
    {
        Vector3 Corner(int i) => new((i & 1) == 0 ? min.X : max.X, (i & 2) == 0 ? min.Y : max.Y, (i & 4) == 0 ? min.Z : max.Z);

        int[][] faces = [[0, 2, 3, 1], [4, 5, 7, 6], [0, 1, 5, 4], [2, 6, 7, 3], [0, 4, 6, 2], [1, 3, 7, 5]];
        return faces
            .SelectMany(f => new[] { new Triangle(Corner(f[0]), Corner(f[1]), Corner(f[2])), new Triangle(Corner(f[0]), Corner(f[2]), Corner(f[3])) })
            .ToList();
    }
}
