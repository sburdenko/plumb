using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Plumb.App.Logging;

internal static class AppLogging
{
    private const string LogFileName = "plumb.log";

    /// <summary>
    /// Logs next to the app; falls back to the temp folder when that is not writable
    /// (read-only install, second instance), and to no logging as a last resort.
    /// </summary>
    public static ILoggerFactory CreateFactory()
    {
        var candidates = new[] { AppContext.BaseDirectory, Path.GetTempPath() };
        foreach (var directory in candidates)
        {
            if (TryCreateProvider(Path.Combine(directory, LogFileName)) is { } provider)
            {
                return LoggerFactory.Create(builder => builder
                    .SetMinimumLevel(LogLevel.Information)
                    .AddProvider(provider));
            }
        }

        return NullLoggerFactory.Instance;
    }

    private static FileLoggerProvider? TryCreateProvider(string path)
    {
        try
        {
            return new FileLoggerProvider(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
