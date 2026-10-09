using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Plumb.App.ViewModels;

public enum StepState
{
    Pending,
    Running,
    Done,
}

/// <summary>One row of the import pipeline list: number, name, and its duration once done.</summary>
public sealed partial class PipelineStepViewModel(int number, string name) : ViewModelBase
{
    public string Number { get; } = number.ToString("00", CultureInfo.InvariantCulture);

    public string Name { get; } = name;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPending), nameof(IsRunning), nameof(IsDone), nameof(StatusText))]
    public partial StepState State { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    public partial TimeSpan Duration { get; private set; }

    public bool IsPending => State == StepState.Pending;

    public bool IsRunning => State == StepState.Running;

    public bool IsDone => State == StepState.Done;

    public string StatusText => State switch
    {
        StepState.Done => Durations.Format(Duration),
        StepState.Running => "running",
        _ => "—",
    };

    public void Start() => State = StepState.Running;

    public void Finish(TimeSpan duration)
    {
        Duration = duration;
        State = StepState.Done;
    }
}
