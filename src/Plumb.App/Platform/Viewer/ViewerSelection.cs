namespace Plumb.App.Platform.Viewer;

/// <summary>What the person selected in the viewer showing <paramref name="PackagePath"/>; null when nothing.</summary>
public sealed record ViewerSelection(string PackagePath, string? GlobalId);
