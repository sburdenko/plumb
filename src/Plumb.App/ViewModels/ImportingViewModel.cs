using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plumb.Core.Import;

namespace Plumb.App.ViewModels;

public sealed partial class ImportingViewModel : ViewModelBase
{
    private const string CancellingText = "Cancelling…";

    private readonly CancellationTokenSource _cancellation;

    public ImportingViewModel(string fileName, CancellationTokenSource cancellation)
    {
        FileName = fileName;
        _cancellation = cancellation;
        StepText = DescribeStep(ImportStep.Validating);
    }

    public string FileName { get; }

    [ObservableProperty]
    public partial string StepText { get; private set; }

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

        StepText = DescribeStep(progress.Step);
        Percent = progress.Percent;
    }

    private bool CanCancel() => !IsCancelling;

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel()
    {
        IsCancelling = true;
        StepText = CancellingText;
        _cancellation.Cancel();
    }

    private static string DescribeStep(ImportStep step) => step switch
    {
        ImportStep.Validating => "Checking file…",
        ImportStep.ReadingModel => "Reading IFC…",
        _ => throw new ArgumentOutOfRangeException(nameof(step), step, "Unknown import step."),
    };
}
