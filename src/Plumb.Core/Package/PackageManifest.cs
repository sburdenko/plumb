using System;
using Plumb.Core.Geometry;

namespace Plumb.Core.Package
{
    /// <summary>
    /// Contents of <c>manifest.json</c>: where a package came from and what it holds.
    /// </summary>
    /// <param name="SourceSha256">Lowercase hex SHA-256 of the source IFC file.</param>
    /// <param name="CreatedUtc">When the package was written, in UTC.</param>
    /// <param name="GeometryError">Why <c>model.glb</c> is missing; null when geometry was built.</param>
    /// <param name="GeometryMessage">The reason shown to the user; null when geometry was built.</param>
    public sealed record PackageManifest(
        int FormatVersion,
        string SourceFile,
        string SourceSha256,
        string IfcSchema,
        DateTime CreatedUtc,
        int ElementCount,
        long ImportDurationMs,
        GeometryError? GeometryError = null,
        string? GeometryMessage = null);
}
