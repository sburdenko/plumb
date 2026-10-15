using Plumb.Core.Import;

namespace Plumb.Tests;

/// <summary>
/// Returns <see cref="Next"/> immediately when set; otherwise blocks until <see cref="Release"/> or cancellation.
/// </summary>
internal sealed class FakeImportService : IImportService
{
    private readonly List<TaskCompletionSource<ImportResult>> _pending = [];
    private readonly Queue<ImportResult> _queued = [];

    /// <summary>Queues the result of the next call; calls with nothing queued wait for <see cref="Release"/>.</summary>
    public ImportResult Next
    {
        set => _queued.Enqueue(value);
    }

    public List<(string Method, string Path, string? OutputDirectory)> Calls { get; } = [];

    public Task<ImportResult> RunAsync(
        string ifcPath,
        string outputDirectory,
        IProgress<ImportProgress> progress,
        CancellationToken cancellationToken)
    {
        Calls.Add(("import", ifcPath, outputDirectory));
        return Load(cancellationToken);
    }

    public Task<ImportResult> OpenPackageAsync(string packagePath, IProgress<ImportProgress> progress, CancellationToken cancellationToken)
    {
        Calls.Add(("open", packagePath, null));
        return Load(cancellationToken);
    }

    public void Release(ImportResult result) => _pending.ForEach(pending => pending.TrySetResult(result));

    private Task<ImportResult> Load(CancellationToken cancellationToken)
    {
        if (_queued.TryDequeue(out var next))
        {
            return Task.FromResult(next);
        }

        var pending = new TaskCompletionSource<ImportResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        cancellationToken.Register(() =>
            pending.TrySetResult(new ImportResult.Failure(ImportError.Cancelled, "cancelled")));
        _pending.Add(pending);
        return pending.Task;
    }
}
