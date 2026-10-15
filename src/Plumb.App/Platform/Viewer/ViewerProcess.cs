using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Plumb.App.Platform.Viewer;

/// <summary>A started viewer. Only macOS lets Plumb raise it; elsewhere it stays where the person left it.</summary>
internal sealed class ViewerProcess(Process process, ILogger logger) : IViewerProcess
{
    public bool HasExited => process.HasExited;

    public void BringToFront()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        try
        {
            MacActivation.Activate(process.Id);
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or InvalidOperationException)
        {
            logger.LogWarning(ex, "Could not bring the viewer forward");
        }
    }

    public void Dispose() => process.Dispose();
}
