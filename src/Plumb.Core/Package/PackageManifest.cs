using System;

namespace Plumb.Core.Package
{
    /// <summary>
    /// Contents of <c>manifest.json</c>: where a package came from and what it holds.
    /// </summary>
    /// <param name="SourceSha256">Lowercase hex SHA-256 of the source IFC file.</param>
    /// <param name="CreatedUtc">When the package was written, in UTC.</param>
    public sealed record PackageManifest(
        int FormatVersion,
        string SourceFile,
        string SourceSha256,
        string IfcSchema,
        DateTime CreatedUtc,
        int ElementCount,
        long ImportDurationMs);
}
