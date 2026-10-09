using CommunityToolkit.Mvvm.Input;

namespace Plumb.App.ViewModels;

public sealed class EmptyStateViewModel(IAsyncRelayCommand browseCommand, string? errorMessage) : ViewModelBase
{
    public IAsyncRelayCommand BrowseCommand { get; } = browseCommand;

    public string? ErrorMessage { get; } = errorMessage;

    public bool HasError => ErrorMessage != null;
}
