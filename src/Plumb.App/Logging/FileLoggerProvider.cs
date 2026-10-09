using Microsoft.Extensions.Logging;

namespace Plumb.App.Logging;

/// <summary>
/// Appends log lines to a single file. Microsoft.Extensions.Logging ships no file provider.
/// </summary>
internal sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly Lock _gate = new();
    private readonly StreamWriter _writer;
    private bool _disposed;

    public FileLoggerProvider(string path)
    {
        var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read);
        _writer = new StreamWriter(stream) { AutoFlush = true };
    }

    public ILogger CreateLogger(string categoryName) => new FileLogger(categoryName, this);

    public void Dispose()
    {
        lock (_gate)
        {
            _disposed = true;
            _writer.Dispose();
        }
    }

    private void Write(string line)
    {
        lock (_gate)
        {
            if (!_disposed)
            {
                _writer.WriteLine(line);
            }
        }
    }

    private sealed class FileLogger(string category, FileLoggerProvider provider) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var line = $"{DateTimeOffset.Now:O} [{logLevel}] {category}: {formatter(state, exception)}";
            provider.Write(exception == null ? line : line + Environment.NewLine + exception);
        }
    }
}
