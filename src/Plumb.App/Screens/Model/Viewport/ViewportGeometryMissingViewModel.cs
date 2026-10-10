using CommunityToolkit.Mvvm.Input;

namespace Plumb.App.Screens.Model.Viewport;

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
