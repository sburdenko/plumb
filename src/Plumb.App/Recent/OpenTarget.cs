using Plumb.Core.Package;

namespace Plumb.App.Recent;

/// <summary>What opening a recent model does: open its package, import its IFC file, or nothing when both are gone.</summary>
public abstract record OpenTarget
{
    private OpenTarget()
    {
    }

    /// <summary>
    /// Prefers the package, which opens in milliseconds, unless the IFC file was saved after the package was written.
    /// </summary>
    public static OpenTarget For(RecentModel model)
    {
        var manifest = model.PackagePath == null ? null : System.IO.Path.Combine(model.PackagePath, PackageLayout.ManifestFile);
        var hasPackage = manifest != null && File.Exists(manifest);
        var hasSource = model.SourcePath != null && File.Exists(model.SourcePath);

        if (hasPackage && hasSource && File.GetLastWriteTimeUtc(model.SourcePath!) > File.GetLastWriteTimeUtc(manifest!))
        {
            return new Source(model.SourcePath!);
        }

        if (hasPackage)
        {
            return new Package(model.PackagePath!);
        }

        return hasSource ? new Source(model.SourcePath!) : new Missing();
    }

    public sealed record Package(string Path) : OpenTarget;

    public sealed record Source(string Path) : OpenTarget;

    public sealed record Missing : OpenTarget;
}
