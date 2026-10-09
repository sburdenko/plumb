using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Plumb.App.Services;
using Plumb.Core.Import;
using Plumb.Core.Package;

namespace Plumb.App.ViewModels;

/// <summary>
/// Owns the window state: empty, importing or loaded. A failed import returns to empty with the error.
/// </summary>
public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly IImportService _importService;
    private readonly IFilePickerService _filePicker;
    private readonly IFileRevealer _revealer;
    private readonly IViewerLauncher _viewer;
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly OpenCommands _open;

    public MainWindowViewModel(
        IImportService importService,
        IFilePickerService filePicker,
        IFileRevealer revealer,
        IViewerLauncher viewer,
        ILogger<MainWindowViewModel> logger)
    {
        _importService = importService;
        _filePicker = filePicker;
        _revealer = revealer;
        _viewer = viewer;
        _logger = logger;
        _open = new OpenCommands(OpenIfcCommand, OpenPackageCommand, OpenPathCommand);
        CurrentState = new EmptyStateViewModel(_open, errorMessage: null);
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenIfcCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenPackageCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenPathCommand))]
    public partial ViewModelBase CurrentState { get; private set; }

    private bool CanStartLoading() => CurrentState is not ImportingViewModel;

    private bool CanOpenPath(string? path) => CanStartLoading() && !string.IsNullOrWhiteSpace(path);

    [RelayCommand(CanExecute = nameof(CanStartLoading))]
    private Task OpenIfcAsync() => PickAndOpenAsync(_filePicker.PickIfcFileAsync);

    [RelayCommand(CanExecute = nameof(CanStartLoading))]
    private Task OpenPackageAsync() => PickAndOpenAsync(_filePicker.PickPackageAsync);

    /// <summary>
    /// Opens a <c>.plumb</c> package as is, or imports anything else as an IFC file.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanOpenPath))]
    private async Task OpenPathAsync(string? path)
    {
        if (path == null)
        {
            return;
        }

        var previous = CurrentState;
        var opensPackage = PackageLayout.IsPackagePath(path);
        using var cancellation = new CancellationTokenSource();
        var importing = new ImportingViewModel(Path.GetFileName(Path.TrimEndingDirectorySeparator(path)), opensPackage, cancellation);
        CurrentState = importing;

        try
        {
            var clock = Stopwatch.StartNew();
            var result = await LoadAsync(path, new Progress<ImportProgress>(importing.Report), cancellation.Token);
            CurrentState = NextState(result, previous, path, opensPackage, clock.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while opening {Path}", path);
            CurrentState = new EmptyStateViewModel(_open, $"Unexpected error: {ex.Message}");
        }
    }

    private Task<ImportResult> LoadAsync(string path, IProgress<ImportProgress> progress, CancellationToken cancellationToken)
    {
        if (PackageLayout.IsPackagePath(path))
        {
            return _importService.OpenPackageAsync(path, progress, cancellationToken);
        }

        var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(path)) ?? ".";
        return _importService.RunAsync(path, outputDirectory, progress, cancellationToken);
    }

    private async Task PickAndOpenAsync(Func<Task<string?>> pick)
    {
        string? path;
        try
        {
            path = await pick();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File picker failed");
            CurrentState = new EmptyStateViewModel(_open, $"Cannot open the file picker: {ex.Message}");
            return;
        }

        // A file may have been dropped while the picker was open.
        if (path != null && CanOpenPath(path))
        {
            await OpenPathAsync(path);
        }
    }

    private ViewModelBase NextState(ImportResult result, ViewModelBase previous, string path, bool openedPackage, TimeSpan loadTime) =>
        result switch
        {
            ImportResult.Success success => new LoadedViewModel(new LoadedModel(success, path, openedPackage, loadTime), _open, _revealer, _viewer),
            ImportResult.Failure { Error: ImportError.Cancelled } => previous is LoadedViewModel
                ? previous
                : new EmptyStateViewModel(_open, errorMessage: null),
            ImportResult.Failure failure => new FailedViewModel(path, failure.Message, _open),
            _ => throw new UnreachableException(),
        };
}
