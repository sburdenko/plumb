namespace Plumb.App.ViewModels;

/// <summary>The start screen: open buttons, the last error if any, and the recent models.</summary>
public sealed class EmptyStateViewModel(OpenCommands open, string? errorMessage, RecentModelsViewModel recent) : ViewModelBase
{
    public OpenCommands Open { get; } = open;

    public string? ErrorMessage { get; } = errorMessage;

    public bool HasError => ErrorMessage != null;

    public RecentModelsViewModel Recent { get; } = recent;
}
