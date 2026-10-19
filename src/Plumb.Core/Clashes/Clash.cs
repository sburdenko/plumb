using System.Numerics;

namespace Plumb.Core.Clashes
{
    /// <summary>Two elements whose surfaces pass through each other.</summary>
    /// <param name="FirstId">The element from the first group.</param>
    /// <param name="SecondId">The element from the second group.</param>
    /// <param name="Penetration">
    /// How far, in metres, the surfaces cross each other where they cross the most: the shallower side of the
    /// deepest pair of crossing triangles. An overlap of a few millimetres is a modelling join; a duct through a
    /// beam crosses by the size of the duct.
    /// </param>
    /// <param name="Location">A point where the two meet, for framing the clash in 3D.</param>
    public sealed record Clash(string FirstId, string SecondId, float Penetration, Vector3 Location);
}
