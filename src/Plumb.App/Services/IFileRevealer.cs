namespace Plumb.App.Services;

/// <summary>
/// Shows a file or folder in the system file manager.
/// </summary>
public interface IFileRevealer
{
    /// <summary>Button text for this platform, e.g. "Show in Finder".</summary>
    string ActionLabel { get; }

    void Reveal(string path);
}
