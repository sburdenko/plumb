using System.Numerics;

namespace Plumb.Core.Clashes
{
    /// <summary>An axis-aligned bounding box, in metres in the model's coordinates.</summary>
    public readonly record struct Box(Vector3 Min, Vector3 Max)
    {
        /// <summary>A box that contains nothing; including any point in it gives a box around that point.</summary>
        public static Box Empty { get; } = new Box(new Vector3(float.MaxValue), new Vector3(float.MinValue));

        public Vector3 Center => (Min + Max) * 0.5f;

        public Vector3 Size => Max - Min;

        public Box Include(Vector3 point) => new Box(Vector3.Min(Min, point), Vector3.Max(Max, point));

        public Box Union(Box other) => new Box(Vector3.Min(Min, other.Min), Vector3.Max(Max, other.Max));

        /// <summary>The part both boxes cover; only meaningful when they overlap.</summary>
        public Box Intersect(Box other) => new Box(Vector3.Max(Min, other.Min), Vector3.Min(Max, other.Max));

        /// <summary>True when the boxes share any point, a face or an edge included.</summary>
        public bool Overlaps(Box other) =>
            Min.X <= other.Max.X && other.Min.X <= Max.X
            && Min.Y <= other.Max.Y && other.Min.Y <= Max.Y
            && Min.Z <= other.Max.Z && other.Min.Z <= Max.Z;
    }
}
