using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Plumb.App.Services;
using Plumb.Core.Import;

namespace Plumb.App.ViewModels;

/// <summary>
/// Owns the window state: empty, importing or loaded. A failed import returns to empty with the error.
/// </summary>
public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly IImportService _importService;
    private readonly IFilePickerService _filePicker;
    private readonly ILogger<MainWindowViewModel> _logger;

    public MainWindowViewModel(IImportService importService, IFilePickerService filePicker, ILogger<MainWindowViewModel> logger)
    {
        _importService = importService;
        _filePicker = filePicker;
        _logger = logger;
        CurrentState = new EmptyStateViewModel(BrowseCommand, errorMessage: null);
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BrowseCommand))]
    [NotifyCanExecuteChangedFor(nameof(ImportFileCommand))]
    public partial ViewModelBase CurrentState { get; private set; }

    private bool CanStartImport() => CurrentState is not ImportingViewModel;

    private bool CanImportFile(string? path) => CanStartImport() && !string.IsNullOrWhiteSpace(path);

    [RelayCommand(CanExecute = nameof(CanStartImport))]
    private async Task BrowseAsync()
    {
        string? path;
        try
        {
            path = await _filePicker.PickIfcFileAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File picker failed");
            CurrentState = new EmptyStateViewModel(BrowseCommand, $"Cannot open the file picker: {ex.Message}");
            return;
        }

        // A file may have been dropped while the picker was open.
        if (path != null && CanImportFile(path))
        {
            await ImportFileAsync(path);
        }
    }

    [RelayCommand(CanExecute = nameof(CanImportFile))]
    private async Task ImportFileAsync(string? path)
    {
        if (path == null)
        {
            return;
        }

        var previous = CurrentState;
        using var cancellation = new CancellationTokenSource();
        var importing = new ImportingViewModel(Path.GetFileName(path), cancellation);
        CurrentState = importing;

        try
        {
            var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(path)) ?? ".";
            var result = await _importService.RunAsync(path, outputDirectory, new Progress<ImportProgress>(importing.Report), cancellation.Token);
            CurrentState = NextState(result, previous);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while importing {Path}", path);
            CurrentState = new EmptyStateViewModel(BrowseCommand, $"Unexpected error: {ex.Message}");
        }
    }

    private ViewModelBase NextState(ImportResult result, ViewModelBase previous) => result switch
    {
        ImportResult.Success success => new LoadedViewModel(success.Model, success.ImportDuration, BrowseCommand),
        ImportResult.Failure { Error: ImportError.Cancelled } => previous is LoadedViewModel
            ? previous
            : new EmptyStateViewModel(BrowseCommand, errorMessage: null),
        ImportResult.Failure failure => new EmptyStateViewModel(BrowseCommand, failure.Message),
        _ => throw new UnreachableException(),
    };
}
