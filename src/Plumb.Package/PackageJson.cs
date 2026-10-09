using System.Text.Json;
using Plumb.Core.Model;
using Plumb.Core.Package;

namespace Plumb.Package;

/// <summary>
/// The JSON files of a package: <c>manifest.json</c> and <c>elements.json</c>.
/// </summary>
internal static class PackageJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    public static void WriteManifest(string path, PackageManifest manifest) =>
        File.WriteAllText(path, JsonSerializer.Serialize(manifest, Options));

    /// <exception cref="JsonException">The file is not a valid manifest.</exception>
    public static PackageManifest ReadManifest(string path) =>
        JsonSerializer.Deserialize<PackageManifest>(File.ReadAllText(path), Options)
            ?? throw new JsonException("The manifest is empty.");

    public static void WriteElementIndex(string path, IReadOnlyList<ElementRecord> elements)
    {
        var storeyNames = elements
            .Where(e => e.GlobalId == e.StoreyGlobalId)
            .ToDictionary(e => e.GlobalId, e => e.Name);

        var entries = elements.Select(e => new ElementIndexEntry(
            e.GlobalId,
            e.IfcType,
            e.Name,
            e.StoreyGlobalId == null ? null : storeyNames.GetValueOrDefault(e.StoreyGlobalId)));

        File.WriteAllText(path, JsonSerializer.Serialize(entries, Options));
    }
}
