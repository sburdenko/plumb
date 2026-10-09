using Microsoft.Extensions.Logging;

namespace Plumb.Import;

/// <summary>
/// A hidden folder next to the target package where a new package is written.
/// <see cref="Publish"/> swaps it in; <see cref="Dispose"/> removes whatever is left.
/// </summary>
internal sealed class PackageDraft : IDisposable
{
    private readonly string _target;
    private readonly string _backup;
    private readonly ILogger _logger;

    public PackageDraft(string target, ILogger logger)
    {
        var directory = Path.GetDirectoryName(target) ?? ".";
        var name = Path.GetFileName(target);
        var suffix = Guid.NewGuid().ToString("N");

        // Same folder as the target, so publishing is a rename rather than a copy.
        Location = Path.Combine(directory, $".{name}.{suffix}.draft");
        _backup = Path.Combine(directory, $".{name}.{suffix}.old");
        _target = target;
        _logger = logger;
    }

    public string Location { get; }

    /// <summary>
    /// Moves the draft into place. An existing package is removed only after the move succeeds,
    /// and is restored if it fails.
    /// </summary>
    public void Publish()
    {
        if (!Directory.Exists(_target))
        {
            Directory.Move(Location, _target);
            return;
        }

        Directory.Move(_target, _backup);
        try
        {
            Directory.Move(Location, _target);
        }
        catch
        {
            Directory.Move(_backup, _target);
            throw;
        }
    }

    public void Dispose()
    {
        DeleteIfExists(Location);
        DeleteIfExists(_backup);
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
