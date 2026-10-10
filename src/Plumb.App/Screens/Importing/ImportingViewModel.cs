using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plumb.App.Shell;
using Plumb.Core.Import;

namespace Plumb.App.Screens.Importing;

/// <summary>
/// Shows the pipeline while an IFC file is imported or a package is opened: each step turns from pending
/// to running to done as progress reports arrive.
/// </summary>
public sealed partial class ImportingViewModel : ScreenViewModel
{
    private static readonly (ImportStep Step, string Name)[] ImportSteps =
    [
        (ImportStep.Validating, "Validate"),
        (ImportStep.Snapshot, "Snapshot"),
        (ImportStep.ReadingModel, "Read"),
        (ImportStep.ConvertingGeometry, "Geometry"),
        (ImportStep.WritingPackage, "Write"),
        (ImportStep.Finalizing, "Publish"),
    ];

    private static readonly (ImportStep Step, string Name)[] OpenSteps = [(ImportStep.OpeningPackage, "Open package")];

    private readonly CancellationTokenSource _cancellation;
    private readonly ImportStep[] _order;
    private readonly Stopwatch _stepClock = Stopwatch.StartNew();
    private int _current = -1;

    public ImportingViewModel(string fileName, bool opensPackage, CancellationTokenSource cancellation)
    {
        FileName = fileName;
        _cancellation = cancellation;
        Kicker = opensPackage ? "OPENING" : "IMPORTING";
        var steps = opensPackage ? OpenSteps : ImportSteps;
        _order = steps.Select(s => s.Step).ToArray();
        Steps = steps.Select((s, i) => new PipelineStepViewModel(i + 1, s.Name)).ToList();
    }

    public string Kicker { get; }

    public string FileName { get; }

    public IReadOnlyList<PipelineStepViewModel> Steps { get; }

    [ObservableProperty]
    public partial int Percent { get; private set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    public partial bool IsCancelling { get; private set; }

    public void Report(ImportProgress progress)
    {
        if (IsCancelling)
        {
            return;
        }

        Percent = progress.Percent;
        var index = Array.IndexOf(_order, progress.Step);
        if (index > _current)
        {
            MoveTo(index);
        }

        if (progress.Percent >= 100 && _current == _order.Length - 1)
        {
            Steps[_current].Finish(_stepClock.Elapsed);
        }
    }

    private void MoveTo(int index)
    {
        if (_current >= 0)
        {
            Steps[_current].Finish(_stepClock.Elapsed);
        }

        for (var skipped = _current + 1; skipped < index; skipped++)
        {
            Steps[skipped].Finish(TimeSpan.Zero);
        }

        _current = index;
        _stepClock.Restart();
        Steps[index].Start();
    }

    private bool CanCancel() => !IsCancelling;

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel()
    {
        IsCancelling = true;
        _cancellation.Cancel();
    }
}
