using Plumb.App.Screens.Start.Recent;
using Plumb.App.Shell;

namespace Plumb.App.Screens.Start;

/// <summary>The start screen: open buttons, the last error if any, and the recent models.</summary>
public sealed class EmptyStateViewModel(OpenCommands open, string? errorMessage, RecentModelsViewModel recent) : ScreenViewModel
{
    public OpenCommands Open { get; } = open;

    public string? ErrorMessage { get; } = errorMessage;

    public bool HasError => ErrorMessage != null;

    public RecentModelsViewModel Recent { get; } = recent;
}
