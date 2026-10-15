using System.Diagnostics.CodeAnalysis;
using Plumb.App.Platform.Viewer;

namespace Plumb.Tests;

internal sealed class FakeViewerLink : IViewerLink
{
    public event EventHandler<ViewerSelection>? SelectionChanged;

    public List<string> Opened { get; } = [];

    public List<(string PackagePath, string? SelectedId)> Opens { get; } = [];

    public List<(string PackagePath, string? GlobalId)> Shown { get; } = [];

    public string? Failure { get; set; }

    public bool TryOpen(string packagePath, string? selectedId, [NotNullWhen(false)] out string? error)
    {
        Opened.Add(packagePath);
        Opens.Add((packagePath, selectedId));
        error = Failure;
        return error == null;
    }

    public void ShowSelection(string packagePath, string? globalId) => Shown.Add((packagePath, globalId));

    /// <summary>Acts as if the person selected <paramref name="globalId"/> in the viewer.</summary>
    public void SelectInViewer(string packagePath, string? globalId) =>
        SelectionChanged?.Invoke(this, new ViewerSelection(packagePath, globalId));
}
