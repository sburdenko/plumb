namespace Plumb.App.Platform;

/// <summary>Puts text on the system clipboard.</summary>
public interface ITextClipboard
{
    /// <returns>False when the clipboard could not be reached; the reason is logged.</returns>
    Task<bool> TrySetTextAsync(string text);
}
