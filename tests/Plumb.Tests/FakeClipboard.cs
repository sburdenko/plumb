using Plumb.App.Platform;

namespace Plumb.Tests;

internal sealed class FakeClipboard : ITextClipboard
{
    public bool Works { get; set; } = true;

    public string? Text { get; private set; }

    public Task<bool> TrySetTextAsync(string text)
    {
        Text = Works ? text : null;
        return Task.FromResult(Works);
    }
}
