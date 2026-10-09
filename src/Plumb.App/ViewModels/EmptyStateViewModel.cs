namespace Plumb.App.ViewModels;

public sealed class EmptyStateViewModel(OpenCommands open, string? errorMessage) : ViewModelBase
{
    public OpenCommands Open { get; } = open;

    public string? ErrorMessage { get; } = errorMessage;

    public bool HasError => ErrorMessage != null;
}
