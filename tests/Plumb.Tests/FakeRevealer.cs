using System.Diagnostics.CodeAnalysis;
using Plumb.App.Platform;

namespace Plumb.Tests;

internal sealed class FakeRevealer : IFileRevealer
{
    public string ActionLabel => "Show in Test";

    public List<string> Revealed { get; } = [];

    public string? Failure { get; set; }

    public bool TryReveal(string path, [NotNullWhen(false)] out string? error)
    {
        Revealed.Add(path);
        error = Failure;
        return error == null;
    }
}
