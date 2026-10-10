using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using Plumb.App.Shell;

namespace Plumb.App.Screens.Model.Viewport;

/// <summary>What the 3D area shows: the geometry is ready, was not built, or there is no package.</summary>
public abstract class ViewportViewModel : ViewModelBase
{
    /// <summary>Right side of the viewport toolbar, e.g. "model.glb · 215 nodes".</summary>
    public abstract string GeometryInfo { get; }
}

public sealed class ViewportReadyViewModel(int? nodeCount, IRelayCommand openIn3D) : ViewportViewModel
{
    public override string GeometryInfo => nodeCount is { } count
        ? string.Format(CultureInfo.InvariantCulture, "model.glb · {0:N0} nodes", count)
        : "model.glb";

    public string Detail => nodeCount is { } count
        ? string.Format(CultureInfo.InvariantCulture, "model.glb is ready · {0:N0} nodes", count)
        : "model.glb is ready";

    public IRelayCommand OpenIn3D { get; } = openIn3D;
}

/// <param name="reason">The user-facing sentence from <see cref="GeometryWarning"/>.</param>
/// <param name="detail">Technical detail shown under Details; may be empty.</param>
/// <param name="sourcePath">The IFC file to import again; null when it is not known.</param>
public sealed class ViewportGeometryMissingViewModel(string reason, string detail, IAsyncRelayCommand<string?> openPath, string? sourcePath)
    : ViewportViewModel
{
    public override string GeometryInfo => "no geometry";

    public string Reason { get; } = reason;

    public string Detail { get; } = detail;

    public bool HasDetail => Detail.Length > 0;

    public bool CanReimport => SourcePath != null;

    public IAsyncRelayCommand<string?> OpenPath { get; } = openPath;

    public string? SourcePath { get; } = sourcePath;
}

public sealed class ViewportNotSavedViewModel(string reason) : ViewportViewModel
{
    public override string GeometryInfo => "no package";

    public string Reason { get; } = reason;
}
