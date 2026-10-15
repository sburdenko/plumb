namespace Plumb.App.Platform.Viewer;

/// <summary>The viewer Plumb last started, if it still matters.</summary>
internal abstract record ViewerSession
{
    private ViewerSession()
    {
    }

    /// <summary>No viewer, or one Plumb has let go of.</summary>
    public sealed record None : ViewerSession;

    /// <param name="Selection">
    /// The selection both sides agree on, or the one Plumb will send once the viewer links. A selection equal to it
    /// is not sent again, so what the viewer reports never echoes back to it.
    /// </param>
    public abstract record Running(string PackagePath, IViewerProcess Process, string? Selection) : ViewerSession;

    /// <summary>Started, but has not said hello yet.</summary>
    public sealed record Starting(string PackagePath, IViewerProcess Process, string? Selection, string Token)
        : Running(PackagePath, Process, Selection);

    public sealed record Linked(string PackagePath, IViewerProcess Process, string? Selection, LinkConnection Connection)
        : Running(PackagePath, Process, Selection);
}
