using System.Text.Json;
using Microsoft.Data.Sqlite;
using Plumb.Core.Model;
using Plumb.Core.Package;

namespace Plumb.Package;

public static class PackageReader
{
    /// <exception cref="DirectoryNotFoundException">The folder does not exist.</exception>
    /// <exception cref="PackageFormatException">The folder is not a readable package.</exception>
    public static PackageContents Read(string directory, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"Package not found: {directory}");
        }

        var manifest = ReadManifest(Path.Combine(directory, PackageLayout.ManifestFile));
        var (elements, properties) = ReadDatabase(Path.Combine(directory, PackageLayout.DatabaseFile), cancellationToken);

        var model = new IfcModelData(manifest.SourceFile, manifest.IfcSchema, elements, properties);
        return new PackageContents(model, manifest);
    }

    private static PackageManifest ReadManifest(string path)
    {
        RequireFile(path);

        PackageManifest manifest;
        try
        {
            manifest = PackageJson.ReadManifest(path);
        }
        catch (JsonException ex)
        {
            throw new PackageFormatException($"{PackageLayout.ManifestFile} is not valid: {ex.Message}", ex);
        }

        return manifest.FormatVersion == PackageLayout.FormatVersion
            ? manifest
            : throw new PackageFormatException(
                $"Package format {manifest.FormatVersion} is not supported; expected {PackageLayout.FormatVersion}.");
    }

    private static (IReadOnlyList<ElementRecord>, IReadOnlyList<PropertyRecord>) ReadDatabase(
        string path,
        CancellationToken cancellationToken)
    {
        RequireFile(path);

        try
        {
            return PackageDatabase.Read(path, cancellationToken);
        }
        catch (SqliteException ex)
        {
            throw new PackageFormatException($"{PackageLayout.DatabaseFile} cannot be read: {ex.Message}", ex);
        }
    }

    private static void RequireFile(string path)
    {
        if (!File.Exists(path))
        {
            throw new PackageFormatException($"{Path.GetFileName(path)} is missing.");
        }
    }
}
