using Microsoft.Extensions.Logging;

namespace Plumb.Import;

/// <summary>
/// A dot-prefixed folder next to the target package where a new package is written.
/// <see cref="Publish"/> swaps it in; <see cref="Dispose"/> removes the draft, and the previous
/// package only once the new one is in place.
/// </summary>
internal sealed class PackageDraft : IDisposable
{
    private readonly string _target;
    private readonly string _backup;
    private readonly ILogger _logger;
    private readonly Action<string, string> _move;
    private DraftState _state = DraftState.Writing;

    public PackageDraft(string target, ILogger logger)
        : this(target, logger, Directory.Move)
    {
    }

    internal PackageDraft(string target, ILogger logger, Action<string, string> move)
    {
        var directory = Path.GetDirectoryName(target) ?? ".";
        var name = Path.GetFileName(target);
        var suffix = Guid.NewGuid().ToString("N");

        // Same folder as the target, so publishing is a rename rather than a copy.
        Location = Path.Combine(directory, $".{name}.{suffix}.draft");
        _backup = Path.Combine(directory, $".{name}.{suffix}.old");
        _target = target;
        _logger = logger;
        _move = move;
    }

    private enum DraftState
    {
        Writing,
        Published,
        RestoreFailed,
    }

    public string Location { get; }

    /// <summary>
    /// Moves the draft into place. An existing package is set aside first and restored if the move fails.
    /// If even the restore fails, the previous package stays in its backup folder and is never deleted.
    /// </summary>
    public void Publish()
    {
        if (!Directory.Exists(_target))
        {
            _move(Location, _target);
            _state = DraftState.Published;
            return;
        }

        _move(_target, _backup);
        try
        {
            _move(Location, _target);
            _state = DraftState.Published;
        }
        catch
        {
            Restore();
            throw;
        }
    }

    public void Dispose()
    {
        DeleteIfExists(Location);
        if (_state == DraftState.Published)
        {
            DeleteIfExists(_backup);
        }
    }

    private void Restore()
    {
        try
        {
            _move(_backup, _target);
        }
        catch (Exception ex)
        {
            _state = DraftState.RestoreFailed;
            _logger.LogError(ex, "Could not restore the previous package; it is kept at {Backup}", _backup);
        }
    }

    private void DeleteIfExists(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return;
        }

        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Could not remove {Directory}", directory);
        }
    }
}
