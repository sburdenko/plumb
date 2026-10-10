using System.Diagnostics.CodeAnalysis;

namespace Plumb.App.Platform;

/// <summary>
/// Starts the 3D viewer on a <c>.plumb</c> package. The viewer is a separate program; nothing is sent to it
/// after it starts.
/// </summary>
public interface IViewerLauncher
{
    /// <param name="error">A message for the user when the viewer is missing or could not start.</param>
    bool TryOpen(string packagePath, [NotNullWhen(false)] out string? error);
}
