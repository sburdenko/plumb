using System.Diagnostics.CodeAnalysis;

namespace Plumb.App.Platform.Viewer;

/// <summary>Starts the viewer program on a package.</summary>
public interface IViewerStarter
{
    /// <param name="invitation">How the viewer links back to Plumb, or null to start it on its own.</param>
    /// <param name="error">A message for the person when the viewer is missing or could not start.</param>
    bool TryStart(
        string packagePath,
        LinkInvitation? invitation,
        [NotNullWhen(true)] out IViewerProcess? process,
        [NotNullWhen(false)] out string? error);
}

/// <summary>A viewer that was started.</summary>
public interface IViewerProcess : IDisposable
{
    bool HasExited { get; }

    /// <summary>Raises the viewer's window above Plumb, where the system allows it.</summary>
    void BringToFront();
}
