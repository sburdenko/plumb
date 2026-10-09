using Microsoft.Extensions.Logging;

namespace Plumb.Tests;

/// <summary>
/// Keeps every log message so a failing assertion can show what the component reported.
/// </summary>
internal sealed class CollectingLogger<T> : ILogger<T>
{
    private readonly List<string> _messages = [];

    public string Text => string.Join(Environment.NewLine, _messages);

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        _messages.Add($"[{logLevel}] {formatter(state, exception)}");
}
