using System.Diagnostics.CodeAnalysis;
using Plumb.App.Platform;

namespace Plumb.Tests;

internal sealed class FakeViewerLauncher : IViewerLauncher
{
    public List<string> Opened { get; } = [];

    public string? Failure { get; set; }

    public bool TryOpen(string packagePath, [NotNullWhen(false)] out string? error)
    {
        Opened.Add(packagePath);
        error = Failure;
        return error == null;
    }
}
