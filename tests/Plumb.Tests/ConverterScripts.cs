using System.Runtime.Versioning;

namespace Plumb.Tests;

/// <summary>
/// Shell scripts that stand in for IfcConvert to reproduce awkward process behaviour.
/// </summary>
[UnsupportedOSPlatform("windows")]
internal static class ConverterScripts
{
    public const string OrphanMarker = "31.7";

    /// <summary>
    /// Starts a process in its own session, so killing the script's process tree does not reach it,
    /// and that process keeps the script's stdout open.
    /// </summary>
    public static string LeavesOrphanHoldingOutput(string directory) =>
        Write(
            directory,
            "orphan.sh",
            $"( perl -e 'use POSIX qw(setsid); setsid(); exec \"sleep\", \"{OrphanMarker}\"' & )\nsleep 30\n");

    public static string FailsWith(string directory, string stdout, string stderr) =>
        Write(directory, "fails.sh", $"printf '%s' '{stdout}'\nprintf '%s\\n' '{stderr}' >&2\nexit 1\n");

    public static void KillOrphans()
    {
        using var pkill = System.Diagnostics.Process.Start("pkill", ["-f", $"sleep {OrphanMarker}"]);
        pkill.WaitForExit();
    }

    private static string Write(string directory, string name, string body)
    {
        var path = Path.Combine(directory, name);
        File.WriteAllText(path, "#!/bin/sh\n" + body);
        File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        return path;
    }
}
