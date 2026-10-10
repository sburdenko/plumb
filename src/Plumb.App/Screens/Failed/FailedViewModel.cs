using CommunityToolkit.Mvvm.Input;
using Plumb.App.Shell;

namespace Plumb.App.Screens.Failed;

/// <summary>An import or package open that failed: the file, the reason, and a way to try again.</summary>
public sealed partial class FailedViewModel(string path, string message, OpenCommands open) : ScreenViewModel
{
    public string FileName { get; } = Path.GetFileName(Path.TrimEndingDirectorySeparator(path));

    public string Message { get; } = message;

    public OpenCommands Open { get; } = open;

    [RelayCommand]
    private Task TryAgainAsync() => Open.OpenPath.ExecuteAsync(path);
}
