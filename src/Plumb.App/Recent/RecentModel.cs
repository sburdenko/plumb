using Plumb.App.ViewModels;
using Plumb.Core.Import;

namespace Plumb.App.Recent;

/// <summary>A model the user opened: where its files are, what it holds and when it was last opened.</summary>
/// <param name="SourcePath">The IFC file, when it is known.</param>
/// <param name="PackagePath">The .plumb package, when one was saved.</param>
public sealed record RecentModel(
    string? SourcePath,
    string? PackagePath,
    string Name,
    string Schema,
    int ElementCount,
    DateTimeOffset LastOpened,
    bool IsPinned)
{
    // macOS and Windows file systems ignore case by default, so Duplex.ifc and duplex.IFC are one file there.
    private static readonly StringComparer PathComparer = OperatingSystem.IsLinux() ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;

    public static RecentModel From(LoadedModel loaded, DateTimeOffset openedAt)
    {
        var source = Normalize(loaded.FindSourcePath());
        var package = Normalize((loaded.Result.Package as PackageState.Saved)?.Path);
        var named = source ?? package ?? loaded.OpenedPath;
        var model = loaded.Result.Model;
        return new RecentModel(
            source,
            package,
            Path.GetFileNameWithoutExtension(Path.TrimEndingDirectorySeparator(named)),
            model.IfcSchema,
            model.Elements.Count,
            openedAt,
            IsPinned: false);
    }

    /// <summary>The folder the model lives in.</summary>
    public string Folder => Path.GetDirectoryName(PackagePath ?? SourcePath) ?? string.Empty;

    /// <summary>True when both describe the same IFC file or the same package.</summary>
    public bool SharesLocationWith(RecentModel other) => Paths.Any(path => other.Paths.Contains(path, PathComparer));

    private IEnumerable<string> Paths => new[] { SourcePath, PackagePath }.OfType<string>();

    private static string? Normalize(string? path) =>
        path == null ? null : Path.GetFullPath(Path.TrimEndingDirectorySeparator(path));
}
