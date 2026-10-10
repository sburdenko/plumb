using Avalonia.Controls;
using Avalonia.Input.Platform;
using Microsoft.Extensions.Logging;

namespace Plumb.App.Platform;

/// <summary>The clipboard of the window's platform.</summary>
public sealed class WindowClipboard(TopLevel window, ILogger<WindowClipboard> logger) : ITextClipboard
{
    public async Task<bool> TrySetTextAsync(string text)
    {
        if (window.Clipboard is not { } clipboard)
        {
            logger.LogWarning("The window has no clipboard");
            return false;
        }

        try
        {
            await clipboard.SetTextAsync(text);
            return true;
        }
        // Each platform reports clipboard failures with its own exception types.
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not copy to the clipboard");
            return false;
        }
    }
}
