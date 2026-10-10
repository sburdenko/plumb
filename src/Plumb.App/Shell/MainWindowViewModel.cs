using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Plumb.App.Platform;
using Plumb.App.Screens.Failed;
using Plumb.App.Screens.Importing;
using Plumb.App.Screens.Model;
using Plumb.App.Screens.Start;
using Plumb.App.Screens.Start.Recent;
using Plumb.Core.Import;
using Plumb.Core.Package;

namespace Plumb.App.Shell;

/// <summary>
/// Owns the window state: empty, importing, failed or loaded, and remembers every model that opens.
/// </summary>
public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly IImportService _importService;
    private readonly PlatformServices _platform;
    private readonly TimeProvider _clock;
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly OpenCommands _open;
    private readonly RecentModelsViewModel _recent;

    public MainWindowViewModel(
        IImportService importService,
        PlatformServices platform,
        IRecentModelStore recentModels,
        TimeProvider clock,
        ILogger<MainWindowViewModel> logger)
    {
        _importService = importService;
        _platform = platform;
        _clock = clock;
        _logger = logger;
        _open = new OpenCommands(OpenIfcCommand, OpenPackageCommand, OpenPathCommand);
        _recent = new RecentModelsViewModel(recentModels, OpenPathCommand, platform.Revealer, clock);
        CurrentState = Empty(errorMessage: null);
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenIfcCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenPackageCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenPathCommand))]
    public partial ScreenViewModel CurrentState { get; private set; }

    private bool CanStartLoading() => CurrentState is not ImportingViewModel;

    private bool CanOpenPath(string? path) => CanStartLoading() && !string.IsNullOrWhiteSpace(path);

    [RelayCommand(CanExecute = nameof(CanStartLoading))]
    private Task OpenIfcAsync() => PickAndOpenAsync(_platform.FilePicker.PickIfcFileAsync);

    [RelayCommand(CanExecute = nameof(CanStartLoading))]
    private Task OpenPackageAsync() => PickAndOpenAsync(_platform.FilePicker.PickPackageAsync);

    /// <summary>
    /// Opens a <c>.plumb</c> package as is, or imports anything else as an IFC file. A package in an earlier format
    /// is imported again from the IFC file next to it, which replaces the package.
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
            if (result is ImportResult.PackageOutdated { SourcePath: { } source })
            {
                _logger.LogInformation("{Package} has an earlier format; importing {Source} again", path, source);
                CurrentState = previous;
                await OpenPathAsync(source);
                return;
            }

            CurrentState = NextState(result, previous, path, opensPackage, clock.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while opening {Path}", path);
            CurrentState = Empty($"Unexpected error: {ex.Message}");
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
            CurrentState = Empty($"Cannot open the file picker: {ex.Message}");
            return;
        }

        // A file may have been dropped while the picker was open.
        if (path != null && CanOpenPath(path))
        {
            await OpenPathAsync(path);
        }
    }

    private ScreenViewModel NextState(ImportResult result, ScreenViewModel previous, string path, bool openedPackage, TimeSpan loadTime) =>
        result switch
        {
            ImportResult.Success success => Loaded(new LoadedModel(success, path, openedPackage, loadTime)),
            ImportResult.Failure { Error: ImportError.Cancelled } => previous is LoadedViewModel
                ? previous
                : Empty(errorMessage: null),
            ImportResult.Failure failure => new FailedViewModel(path, failure.Message, _open),
            ImportResult.PackageOutdated outdated => new FailedViewModel(path, OutdatedMessage(path, outdated.SourceFile), _open),
            _ => throw new UnreachableException(),
        };

    private static string OutdatedMessage(string packagePath, string sourceFile) =>
        $"{Path.GetFileName(Path.TrimEndingDirectorySeparator(packagePath))} was saved by an earlier version of Plumb. "
        + $"Open {sourceFile} to build it again; it is not next to the package.";

    private LoadedViewModel Loaded(LoadedModel model)
    {
        _recent.Record(model);
        return new LoadedViewModel(model, _open, _platform, _clock);
    }

    private EmptyStateViewModel Empty(string? errorMessage)
    {
        _recent.Refresh();
        return new EmptyStateViewModel(_open, errorMessage, _recent);
    }
}
