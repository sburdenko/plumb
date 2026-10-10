using Plumb.Core.Package;

namespace Plumb.Package;

/// <summary>
/// The package was written by an earlier version of Plumb in a format this version no longer reads.
/// </summary>
/// <param name="sourceFile">The IFC file name the package was made from, as its manifest records it.</param>
public sealed class PackageOutdatedException(int formatVersion, string sourceFile)
    : Exception($"Package format {formatVersion} is older than {PackageLayout.FormatVersion}.")
{
    public int FormatVersion { get; } = formatVersion;

    public string SourceFile { get; } = sourceFile;
}
