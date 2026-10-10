using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Plumb.App.Platform;

public sealed class SystemFileRevealer(ILogger<SystemFileRevealer> logger) : IFileRevealer
{
    public string ActionLabel { get; } =
        OperatingSystem.IsMacOS() ? "Show in Finder"
        : OperatingSystem.IsWindows() ? "Show in Explorer"
        : "Show in Folder";

    public bool TryReveal(string path, [NotNullWhen(false)] out string? error)
    {
        if (!Directory.Exists(path) && !File.Exists(path))
        {
            error = $"{Path.GetFileName(Path.TrimEndingDirectorySeparator(path))} no longer exists.";
            return false;
        }

        try
        {
            using var process = Process.Start(StartInfoFor(path));
            error = null;
            return true;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            logger.LogError(ex, "Cannot reveal {Path}", path);
            error = $"Could not open the file manager: {ex.Message}";
            return false;
        }
    }

    private static ProcessStartInfo StartInfoFor(string path)
    {
        if (OperatingSystem.IsMacOS())
        {
            return new ProcessStartInfo("open") { ArgumentList = { "-R", path } };
        }

        if (OperatingSystem.IsWindows())
        {
            // Explorer parses its own command line and needs the quotes right after the comma.
            return new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"");
        }

        return new ProcessStartInfo("xdg-open") { ArgumentList = { Path.GetDirectoryName(path) ?? path } };
    }
}
