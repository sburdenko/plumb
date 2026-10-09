namespace Plumb.Tests;

/// <summary>
/// Unlike <see cref="Progress{T}"/>, reports on the calling thread so tests observe every value in order.
/// </summary>
internal sealed class SyncProgress<T>(Action<T> onReport) : IProgress<T>
{
    public void Report(T value) => onReport(value);
}
