using Plumb.Core.Import;

namespace Plumb.App.Screens.Model;

/// <summary>A successfully loaded model and how it got here.</summary>
/// <param name="OpenedPath">The .ifc file or .plumb folder the user opened.</param>
/// <param name="OpenedPackage">True when an existing package was opened rather than an IFC file imported.</param>
/// <param name="LoadTime">How long opening took, as the user waited for it.</param>
public sealed record LoadedModel(ImportResult.Success Result, string OpenedPath, bool OpenedPackage, TimeSpan LoadTime)
{
    /// <summary>
    /// The IFC file this model came from: the opened file, or for a package the source file next to it, if it still exists.
    /// </summary>
    public string? FindSourcePath()
    {
        if (!OpenedPackage)
        {
            return OpenedPath;
        }

        var folder = Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(OpenedPath));
        var candidate = folder == null ? null : Path.Combine(folder, Result.Model.SourceFile);
        return candidate != null && File.Exists(candidate) ? candidate : null;
    }
}
