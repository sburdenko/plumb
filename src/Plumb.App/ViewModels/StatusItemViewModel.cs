using CommunityToolkit.Mvvm.Input;

namespace Plumb.App.ViewModels;

/// <summary>
/// A status bar entry such as "Package saved". A problem shows its reason on click; a fine entry may run an action.
/// </summary>
public sealed record StatusItemViewModel(string Text, bool IsProblem, string? Reason, IRelayCommand? Action)
{
    public bool IsAction => !IsProblem && Action != null;

    public bool IsPlain => !IsProblem && Action == null;
}
