using System.Diagnostics.CodeAnalysis;

namespace Plumb.App.Platform.Viewer;

/// <summary>
/// The 3D viewer Plumb started, and the selection the two share while it runs. The viewer is a separate program:
/// when it is closed, or never links back, everything else keeps working.
/// </summary>
public interface IViewerLink
{
    /// <summary>Raised on the window's thread when the person changes the selection in the viewer.</summary>
    event EventHandler<ViewerSelection>? SelectionChanged;

    /// <summary>
    /// Shows <paramref name="packagePath"/> in the viewer: brings forward the one already showing it, or starts one.
    /// </summary>
    /// <param name="selectedId">The element selected in Plumb, which the viewer selects once it has loaded.</param>
    /// <param name="error">A message for the person when the viewer is missing or could not start.</param>
    bool TryOpen(string packagePath, string? selectedId, [NotNullWhen(false)] out string? error);

    /// <summary>Tells the viewer showing <paramref name="packagePath"/>, if any, that Plumb selected this element.</summary>
    /// <param name="globalId">The element, or null when nothing is selected.</param>
    void ShowSelection(string packagePath, string? globalId);
}
