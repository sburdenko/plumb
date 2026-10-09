using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Plumb.App.Services;

/// <summary>
/// Finds the viewer build and starts it with <c>--package</c>. The location is <c>viewer-build/</c> next to
/// the app unless the <c>PLUMB_VIEWER</c> environment variable points somewhere else.
/// </summary>
public sealed class ViewerLauncher(ILogger<ViewerLauncher> logger) : IViewerLauncher
{
    public const string LocationVariable = "PLUMB_VIEWER";
    private const string NotFound = "The 3D viewer was not found. Build it with tools/build-viewer.sh, or set PLUMB_VIEWER to its location.";

    public static string DefaultLocation => Path.Combine(AppContext.BaseDirectory, "viewer-build");

    public bool TryOpen(string packagePath, [NotNullWhen(false)] out string? error)
    {
        if (!Directory.Exists(packagePath))
        {
            error = $"{Path.GetFileName(Path.TrimEndingDirectorySeparator(packagePath))} no longer exists.";
            return false;
        }

        var location = Environment.GetEnvironmentVariable(LocationVariable) is { Length: > 0 } configured ? configured : DefaultLocation;
        var executable = FindExecutable(location);
        if (executable == null)
        {
            logger.LogWarning("No viewer executable under {Location}", location);
            error = NotFound;
            return false;
        }

        try
        {
            var startInfo = new ProcessStartInfo(executable) { UseShellExecute = false, ArgumentList = { "--package", packagePath } };
            using var process = Process.Start(startInfo);
            logger.LogInformation("Viewer started for {Package}", packagePath);
            error = null;
            return true;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            logger.LogError(ex, "Cannot start {Viewer}", executable);
            error = $"The 3D viewer could not be started: {ex.Message}";
            return false;
        }
    }

    /// <param name="location">A viewer folder, a macOS .app bundle, or the executable itself.</param>
    internal static string? FindExecutable(string location)
    {
        if (File.Exists(location))
        {
            return location;
        }

        if (!Directory.Exists(location))
        {
            return null;
        }

        if (OperatingSystem.IsMacOS())
        {
            var bundle = location.EndsWith(".app", StringComparison.OrdinalIgnoreCase)
                ? location
                : Directory.EnumerateDirectories(location, "*.app").FirstOrDefault();
            var binaries = bundle == null ? null : Path.Combine(bundle, "Contents", "MacOS");
            return binaries != null && Directory.Exists(binaries) ? Directory.EnumerateFiles(binaries).FirstOrDefault() : null;
        }

        // Unity names the player PlumbViewer.exe on Windows and PlumbViewer.x86_64 on Linux.
        var playerExtension = OperatingSystem.IsWindows() ? ".exe" : ".x86_64";
        return Directory.EnumerateFiles(location, "PlumbViewer*", SearchOption.AllDirectories)
            .FirstOrDefault(path => path.EndsWith(playerExtension, StringComparison.OrdinalIgnoreCase));
    }
}
