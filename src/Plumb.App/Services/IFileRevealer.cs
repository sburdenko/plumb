using System.Diagnostics.CodeAnalysis;

namespace Plumb.App.Services;

/// <summary>
/// Shows a file or folder in the system file manager.
/// </summary>
public interface IFileRevealer
{
    /// <summary>Button text for this platform, e.g. "Show in Finder".</summary>
    string ActionLabel { get; }

    /// <param name="error">A message for the user when the file manager could not be opened.</param>
    bool TryReveal(string path, [NotNullWhen(false)] out string? error);
}
