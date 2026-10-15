using System.Runtime.Versioning;
using static Plumb.App.Platform.MacRuntime;

namespace Plumb.App.Platform.Viewer;

[SupportedOSPlatform("macos")]
internal static class MacActivation
{
    private const nuint ActivateAllWindows = 1;

    /// <summary>
    /// Activates the application with process id <paramref name="pid"/>. macOS honours it because Plumb is the
    /// active application when the person asks for the viewer.
    /// </summary>
    /// <exception cref="InvalidOperationException">No application runs with that id.</exception>
    public static void Activate(int pid)
    {
        var application = Send(Class("NSRunningApplication"), Selector("runningApplicationWithProcessIdentifier:"), pid);
        if (application == IntPtr.Zero)
        {
            throw new InvalidOperationException($"No application runs with process id {pid}.");
        }

        Send(application, Selector("activateWithOptions:"), ActivateAllWindows);
    }
}
