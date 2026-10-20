using Plumb.Core.Clashes;
using Plumb.Core.Search;

namespace Plumb.Tests;

/// <summary>Answers with <see cref="Next"/> when one is queued; otherwise waits for <see cref="Release"/> or cancellation.</summary>
internal sealed class FakeClashService : IClashService
{
    private readonly Queue<ClashResult> _queued = [];
    private TaskCompletionSource<ClashResult>? _pending;

    public ClashResult Next
    {
        set => _queued.Enqueue(value);
    }

    public List<(string PackagePath, int First, int Second)> Calls { get; } = [];

    public Task<ClashResult> FindAsync(
        string packagePath,
        ElementMatches first,
        ElementMatches second,
        IProgress<ClashStep> progress,
        CancellationToken cancellationToken)
    {
        Calls.Add((packagePath, first.Count, second.Count));
        progress.Report(ClashStep.ReadingGeometry);
        if (_queued.TryDequeue(out var next))
        {
            return Task.FromResult(next);
        }

        _pending = new TaskCompletionSource<ClashResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        cancellationToken.Register(() => _pending.TrySetResult(new ClashResult.Cancelled()));
        return _pending.Task;
    }

    public void Release(ClashResult result) => _pending?.TrySetResult(result);
}
