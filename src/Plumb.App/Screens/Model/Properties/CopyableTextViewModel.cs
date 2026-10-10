using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plumb.App.Platform;
using Plumb.App.Shell;

namespace Plumb.App.Screens.Model.Properties;

/// <summary>A value that copies itself to the clipboard on click and says for a moment whether that worked.</summary>
public sealed partial class CopyableTextViewModel(string text, ITextClipboard clipboard, TimeProvider clock) : ViewModelBase
{
    public static readonly TimeSpan FeedbackDuration = TimeSpan.FromSeconds(1.5);

    public string Text { get; } = text;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Feedback))]
    [NotifyPropertyChangedFor(nameof(HasFeedback))]
    public partial CopyState State { get; private set; }

    public string Feedback => State switch
    {
        CopyState.Copied => "Copied",
        CopyState.Failed => "Could not copy",
        _ => string.Empty,
    };

    public bool HasFeedback => State != CopyState.Idle;

    [RelayCommand]
    private async Task CopyAsync()
    {
        State = await clipboard.TrySetTextAsync(Text) ? CopyState.Copied : CopyState.Failed;
        await Task.Delay(FeedbackDuration, clock);
        State = CopyState.Idle;
    }
}
