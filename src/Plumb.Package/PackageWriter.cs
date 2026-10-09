using Plumb.Core.Model;
using Plumb.Core.Package;

namespace Plumb.Package;

public static class PackageWriter
{
    /// <summary>
    /// Writes a complete package into <paramref name="directory"/>, creating it if needed.
    /// The manifest is written last, so a folder without one was never finished.
    /// </summary>
    public static void Write(string directory, IfcModelData model, PackageManifest manifest, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(directory);

        PackageDatabase.Write(Path.Combine(directory, PackageLayout.DatabaseFile), model, cancellationToken);
        PackageJson.WriteElementIndex(Path.Combine(directory, PackageLayout.ElementIndexFile), model.Elements);
        PackageJson.WriteManifest(Path.Combine(directory, PackageLayout.ManifestFile), manifest);
    }
}
