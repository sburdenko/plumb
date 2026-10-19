using System.Numerics;

namespace Plumb.Core.Clashes
{
    /// <summary>One triangle of an element's surface, in metres in the model's coordinates.</summary>
    public readonly record struct Triangle(Vector3 A, Vector3 B, Vector3 C)
    {
        public Box Bounds => new Box(Vector3.Min(Vector3.Min(A, B), C), Vector3.Max(Vector3.Max(A, B), C));
    }
}
