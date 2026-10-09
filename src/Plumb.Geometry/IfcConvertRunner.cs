using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Plumb.Core.Geometry;

namespace Plumb.Geometry;

/// <summary>
/// Converts IFC geometry to binary glTF by running IfcOpenShell's IfcConvert as a separate process.
/// </summary>
public sealed partial class IfcConvertRunner(string executablePath, TimeSpan timeout, ILogger<IfcConvertRunner> logger) : IGeometryConverter
{
    private static readonly TimeSpan KillGracePeriod = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan OutputGracePeriod = TimeSpan.FromSeconds(5);

    /// <summary>The copy bundled with the app in <c>tools/ifcconvert/</c>.</summary>
    public static string DefaultExecutablePath =>
        Path.Combine(AppContext.BaseDirectory, "tools", "ifcconvert", OperatingSystem.IsWindows() ? "IfcConvert.exe" : "IfcConvert");

    /// <summary>Lets tests check that this run's own process was stopped.</summary>
    internal Action<int>? ProcessStarted { get; init; }

    public async Task<GeometryState> ConvertAsync(string ifcPath, string glbPath, CancellationToken cancellationToken)
    {
        if (!File.Exists(executablePath))
        {
            return new GeometryState.NotBuilt(GeometryError.ConverterMissing, $"not found at {executablePath}");
        }

        using var process = new Process { StartInfo = StartInfo(ifcPath, glbPath) };
        try
        {
            process.Start();
        }
        catch (Win32Exception ex)
        {
            logger.LogError(ex, "Cannot start {Converter}", executablePath);
            return new GeometryState.NotBuilt(GeometryError.ConverterFailed, $"could not start: {ex.Message}");
        }

        ProcessStarted?.Invoke(process.Id);

        // Both streams are drained while the process runs; a full pipe buffer would otherwise block it.
        var output = ReadAllAsync(process.StandardOutput.BaseStream);
        var errors = ReadAllAsync(process.StandardError.BaseStream);

        var outcome = await WaitAsync(process, cancellationToken).ConfigureAwait(false);
        var log = await CollectOutputAsync(output, errors).ConfigureAwait(false);

        if (outcome == Outcome.Cancelled)
        {
            DeletePartialFile(glbPath);
            cancellationToken.ThrowIfCancellationRequested();
        }

        if (outcome == Outcome.TimedOut)
        {
            DeletePartialFile(glbPath);
            return new GeometryState.NotBuilt(GeometryError.Timeout, $"stopped after {timeout.TotalSeconds:0.#} s");
        }

        if (process.ExitCode != 0 || !File.Exists(glbPath))
        {
            logger.LogWarning("IfcConvert failed with exit code {ExitCode}: {Log}", process.ExitCode, log);
            DeletePartialFile(glbPath);
            return new GeometryState.NotBuilt(GeometryError.ConverterFailed, $"exit code {process.ExitCode}: {LastError(log)}".TrimEnd(' ', ':'));
        }

        return new GeometryState.Built();
    }

    private ProcessStartInfo StartInfo(string ifcPath, string glbPath)
    {
        var startInfo = new ProcessStartInfo(executablePath)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        // Flags checked against "IfcConvert --help" for 0.9.0. --use-element-guids also names glTF nodes.
        foreach (var argument in new[] { "-y", "-q", "--no-progress", "--use-element-guids", ifcPath, glbPath })
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }

    private async Task<Outcome> WaitAsync(Process process, CancellationToken cancellationToken)
    {
        using var timeoutSource = new CancellationTokenSource(timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);
        try
        {
            await process.WaitForExitAsync(linked.Token).ConfigureAwait(false);
            return Outcome.Exited;
        }
        catch (OperationCanceledException)
        {
            // Finishing just as the time limit fired is a result, not a timeout; a user cancel always wins.
            if (!cancellationToken.IsCancellationRequested && process.HasExited)
            {
                return Outcome.Exited;
            }

            await StopAsync(process).ConfigureAwait(false);
            return cancellationToken.IsCancellationRequested ? Outcome.Cancelled : Outcome.TimedOut;
        }
    }

    /// <summary>
    /// Waits briefly for the output once the process is gone. A process that escaped the killed tree
    /// can keep the pipes open forever, so the log is best effort and never blocks the result.
    /// </summary>
    private async Task<string> CollectOutputAsync(Task<string> output, Task<string> errors)
    {
        try
        {
            await Task.WhenAll(output, errors).WaitAsync(OutputGracePeriod).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            logger.LogWarning("IfcConvert output was still open {Seconds} s after it stopped", OutputGracePeriod.TotalSeconds);
        }

        return string.Join('\n', new[] { output, errors }.Where(t => t.IsCompletedSuccessfully).Select(t => t.Result));
    }

    private static async Task<string> ReadAllAsync(Stream stream)
    {
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer).ConfigureAwait(false);
        return ConsoleText.Decode(buffer.ToArray());
    }

    private async Task StopAsync(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
            using var grace = new CancellationTokenSource(KillGracePeriod);
            await process.WaitForExitAsync(grace.Token).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not stop IfcConvert (process {Id})", process.Id);
        }
    }

    private void DeletePartialFile(string glbPath)
    {
        try
        {
            File.Delete(glbPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Could not remove partial {Path}", glbPath);
        }
    }

    // IfcConvert logs details first and its summary ("Unable to parse input file") last.
    private static string LastError(string log) =>
        log.Split('\n')
            .Select(line => ErrorLine().Match(line.Trim()))
            .LastOrDefault(match => match.Success)
            ?.Groups["message"].Value ?? string.Empty;

    // "[error] [SYN001] [2026-10-09 13:24:31] message": strips the leading tags, keeps brackets in the message.
    [GeneratedRegex(@"^\[error\](\s*\[[^\]]*\])*\s*(?<message>.*)$", RegexOptions.IgnoreCase)]
    private static partial Regex ErrorLine();

    private enum Outcome
    {
        Exited,
        TimedOut,
        Cancelled,
    }
}
