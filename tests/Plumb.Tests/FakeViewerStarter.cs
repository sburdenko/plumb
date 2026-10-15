using System.Diagnostics.CodeAnalysis;
using Plumb.App.Platform.Viewer;

namespace Plumb.Tests;

internal sealed class FakeViewerStarter : IViewerStarter
{
    public List<(string PackagePath, LinkInvitation? Invitation)> Started { get; } = [];

    public List<FakeViewerProcess> Processes { get; } = [];

    public string? Failure { get; set; }

    public LinkInvitation Invitation => Started[^1].Invitation ?? throw new InvalidOperationException("Started without a link.");

    public bool TryStart(
        string packagePath,
        LinkInvitation? invitation,
        [NotNullWhen(true)] out IViewerProcess? process,
        [NotNullWhen(false)] out string? error)
    {
        if (Failure != null)
        {
            process = null;
            error = Failure;
            return false;
        }

        Started.Add((packagePath, invitation));
        var started = new FakeViewerProcess();
        Processes.Add(started);
        process = started;
        error = null;
        return true;
    }
}

internal sealed class FakeViewerProcess : IViewerProcess
{
    public bool HasExited { get; set; }

    public int TimesBroughtToFront { get; private set; }

    public void BringToFront() => TimesBroughtToFront++;

    public void Dispose()
    {
    }
}
