using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging.Abstractions;
using Plumb.App.Services;
using Plumb.App.ViewModels;
using Plumb.Core.Geometry;
using Plumb.Core.Import;
using Plumb.Core.Model;

namespace Plumb.Tests;

[TestFixture]
public sealed class MainWindowViewModelTests
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(5);

    private static readonly ImportResult.Success Loaded = new(
        new IfcModelData("a.ifc", "IFC4", [new ElementRecord("P", "IfcProject", "Project", null, null)], []),
        TimeSpan.FromSeconds(1),
        new PackageState.Saved("/models/a.plumb", new GeometryState.Built()));

    private FakeImportService _importService = null!;
    private FakeFilePicker _picker = null!;
    private FakeRevealer _revealer = null!;
    private MainWindowViewModel _viewModel = null!;

    [SetUp]
    public void CreateViewModel()
    {
        _importService = new FakeImportService();
        _picker = new FakeFilePicker();
        _revealer = new FakeRevealer();
        _viewModel = new MainWindowViewModel(_importService, _picker, _revealer, NullLogger<MainWindowViewModel>.Instance);
    }

    [Test]
    public void StartsEmptyWithoutError()
    {
        Assert.That(_viewModel.CurrentState, Is.TypeOf<EmptyStateViewModel>());
        Assert.That(((EmptyStateViewModel)_viewModel.CurrentState).HasError, Is.False);
    }

    [Test]
    public async Task SuccessfulImportShowsLoadedModel()
    {
        _importService.Next = Loaded;

        await _viewModel.OpenPathCommand.ExecuteAsync("a.ifc").WaitAsync(TestTimeout);

        Assert.That(_viewModel.CurrentState, Is.TypeOf<LoadedViewModel>());
        Assert.That(((LoadedViewModel)_viewModel.CurrentState).FileName, Is.EqualTo("a.ifc"));
    }

    [Test]
    public async Task IfcFileIsImportedIntoItsOwnFolder()
    {
        _importService.Next = Loaded;
        var ifc = Path.Combine(Path.GetTempPath(), "models", "a.ifc");

        await _viewModel.OpenPathCommand.ExecuteAsync(ifc).WaitAsync(TestTimeout);

        Assert.That(_importService.Calls, Is.EqualTo(new[] { ("import", ifc, Path.GetDirectoryName(ifc)) }));
    }

    [Test]
    public async Task PackageIsOpenedNotImported()
    {
        _importService.Next = Loaded;

        await _viewModel.OpenPathCommand.ExecuteAsync("/models/a.plumb").WaitAsync(TestTimeout);

        Assert.That(_importService.Calls, Is.EqualTo(new[] { ("open", "/models/a.plumb", (string?)null) }));
        Assert.That(_viewModel.CurrentState, Is.TypeOf<LoadedViewModel>());
    }

    [Test]
    public async Task FailedImportShowsEmptyWithMessage()
    {
        _importService.Next = new ImportResult.Failure(ImportError.NotIfc, "Not an .ifc file: a.txt");

        await _viewModel.OpenPathCommand.ExecuteAsync("a.txt").WaitAsync(TestTimeout);

        var empty = (EmptyStateViewModel)_viewModel.CurrentState;
        Assert.That(empty.ErrorMessage, Is.EqualTo("Not an .ifc file: a.txt"));
    }

    [Test]
    public async Task CancelReturnsToPreviouslyLoadedModel()
    {
        _importService.Next = Loaded;
        await _viewModel.OpenPathCommand.ExecuteAsync("a.ifc").WaitAsync(TestTimeout);
        var loaded = _viewModel.CurrentState;

        await CancelBlockingImportAsync("b.ifc");

        Assert.That(_viewModel.CurrentState, Is.SameAs(loaded));
    }

    [Test]
    public async Task CancelAfterFailureShowsEmptyWithoutStaleError()
    {
        _importService.Next = new ImportResult.Failure(ImportError.ParseFailed, "broken");
        await _viewModel.OpenPathCommand.ExecuteAsync("a.ifc").WaitAsync(TestTimeout);

        await CancelBlockingImportAsync("b.ifc");

        Assert.That(((EmptyStateViewModel)_viewModel.CurrentState).HasError, Is.False);
    }

    [Test]
    public async Task CommandsAreDisabledWhileImporting()
    {
        var import = _viewModel.OpenPathCommand.ExecuteAsync("a.ifc");

        Assert.That(_viewModel.CurrentState, Is.TypeOf<ImportingViewModel>());
        Assert.That(_viewModel.OpenPathCommand.CanExecute("b.ifc"), Is.False);
        Assert.That(_viewModel.OpenIfcCommand.CanExecute(null), Is.False);
        Assert.That(_viewModel.OpenPackageCommand.CanExecute(null), Is.False);

        _importService.Release(Loaded);
        await import.WaitAsync(TestTimeout);
        Assert.That(_viewModel.OpenPathCommand.CanExecute("b.ifc"), Is.True);
    }

    [Test]
    public async Task FileDroppedWhilePickerIsOpenIsImportedOnce()
    {
        var browse = _viewModel.OpenIfcCommand.ExecuteAsync(null);
        var drop = _viewModel.OpenPathCommand.ExecuteAsync("dropped.ifc");

        _picker.Choose("picked.ifc");
        await browse.WaitAsync(TestTimeout);
        _importService.Release(Loaded);
        await drop.WaitAsync(TestTimeout);

        Assert.That(_importService.Calls.Select(c => c.Path), Is.EqualTo(new[] { "dropped.ifc" }));
    }

    [Test]
    public async Task OpenPackageButtonOpensThePickedFolder()
    {
        _importService.Next = Loaded;
        var browse = _viewModel.OpenPackageCommand.ExecuteAsync(null);

        _picker.Choose("/models/a.plumb");
        await browse.WaitAsync(TestTimeout);

        Assert.That(_importService.Calls.Single().Method, Is.EqualTo("open"));
    }

    [Test]
    public async Task PickerFailureShowsError()
    {
        _picker.Fail(new InvalidOperationException("no window"));

        await _viewModel.OpenIfcCommand.ExecuteAsync(null).WaitAsync(TestTimeout);

        Assert.That(((EmptyStateViewModel)_viewModel.CurrentState).ErrorMessage, Does.Contain("no window"));
    }

    [Test]
    public async Task PickerCancelDoesNotImport()
    {
        var browse = _viewModel.OpenIfcCommand.ExecuteAsync(null);
        _picker.Choose(null);
        await browse.WaitAsync(TestTimeout);

        Assert.That(_importService.Calls, Is.Empty);
    }

    [Test]
    public async Task RevealShowsThePackageInTheFileManager()
    {
        _importService.Next = Loaded;
        await _viewModel.OpenPathCommand.ExecuteAsync("a.ifc").WaitAsync(TestTimeout);
        var loaded = (LoadedViewModel)_viewModel.CurrentState;

        loaded.RevealCommand.Execute(null);

        Assert.That(loaded.RevealLabel, Is.EqualTo("Show in Test"));
        Assert.That(_revealer.Revealed, Is.EqualTo(new[] { "/models/a.plumb" }));
    }

    [Test]
    public async Task UnsavedPackageShowsAWarningAndHidesReveal()
    {
        _importService.Next = Loaded with { Package = new PackageState.NotSaved("Could not save a.plumb: read-only") };

        await _viewModel.OpenPathCommand.ExecuteAsync("a.ifc").WaitAsync(TestTimeout);

        var loaded = (LoadedViewModel)_viewModel.CurrentState;
        Assert.That(loaded.Warning, Is.EqualTo("Could not save a.plumb: read-only"));
        Assert.That(loaded.CanReveal, Is.False);
        Assert.That(loaded.RevealCommand.CanExecute(null), Is.False);
    }

    [Test]
    public async Task MissingGeometryShowsAWarningButKeepsReveal()
    {
        var geometry = new GeometryState.NotBuilt(GeometryError.ConverterMissing, "not found at /app/tools/ifcconvert/IfcConvert");
        _importService.Next = Loaded with { Package = new PackageState.Saved("/models/a.plumb", geometry) };

        await _viewModel.OpenPathCommand.ExecuteAsync("a.ifc").WaitAsync(TestTimeout);

        var loaded = (LoadedViewModel)_viewModel.CurrentState;
        Assert.That(loaded.Warning, Is.EqualTo("3D geometry was not built: IfcConvert is not installed next to Plumb."));
        Assert.That(loaded.CanReveal, Is.True);
    }

    [Test]
    public async Task SavedPackageHasNoWarning()
    {
        _importService.Next = Loaded;

        await _viewModel.OpenPathCommand.ExecuteAsync("a.ifc").WaitAsync(TestTimeout);

        var loaded = (LoadedViewModel)_viewModel.CurrentState;
        Assert.That(loaded.Warning, Is.Null);
        Assert.That(loaded.CanReveal, Is.True);
    }

    [Test]
    public async Task FailedRevealShowsTheReason()
    {
        _importService.Next = Loaded;
        await _viewModel.OpenPathCommand.ExecuteAsync("a.ifc").WaitAsync(TestTimeout);
        var loaded = (LoadedViewModel)_viewModel.CurrentState;
        _revealer.Failure = "The package no longer exists.";

        loaded.RevealCommand.Execute(null);

        Assert.That(loaded.RevealError, Is.EqualTo("The package no longer exists."));
    }

    private async Task CancelBlockingImportAsync(string path)
    {
        var import = _viewModel.OpenPathCommand.ExecuteAsync(path);
        ((ImportingViewModel)_viewModel.CurrentState).CancelCommand.Execute(null);
        await import.WaitAsync(TestTimeout);
    }

    /// <summary>
    /// Returns <see cref="Next"/> immediately when set; otherwise blocks until <see cref="Release"/> or cancellation.
    /// </summary>
    private sealed class FakeImportService : IImportService
    {
        private readonly List<TaskCompletionSource<ImportResult>> _pending = [];

        public ImportResult? Next { get; set; }

        public List<(string Method, string Path, string? OutputDirectory)> Calls { get; } = [];

        public Task<ImportResult> RunAsync(
            string ifcPath,
            string outputDirectory,
            IProgress<ImportProgress> progress,
            CancellationToken cancellationToken)
        {
            Calls.Add(("import", ifcPath, outputDirectory));
            return Load(cancellationToken);
        }

        public Task<ImportResult> OpenPackageAsync(string packagePath, IProgress<ImportProgress> progress, CancellationToken cancellationToken)
        {
            Calls.Add(("open", packagePath, null));
            return Load(cancellationToken);
        }

        public void Release(ImportResult result) => _pending.ForEach(pending => pending.TrySetResult(result));

        private Task<ImportResult> Load(CancellationToken cancellationToken)
        {
            if (Next is { } next)
            {
                Next = null;
                return Task.FromResult(next);
            }

            var pending = new TaskCompletionSource<ImportResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            cancellationToken.Register(() =>
                pending.TrySetResult(new ImportResult.Failure(ImportError.Cancelled, "cancelled")));
            _pending.Add(pending);
            return pending.Task;
        }
    }

    private sealed class FakeFilePicker : IFilePickerService
    {
        private readonly TaskCompletionSource<string?> _choice = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<string?> PickIfcFileAsync() => _choice.Task;

        public Task<string?> PickPackageAsync() => _choice.Task;

        public void Choose(string? path) => _choice.SetResult(path);

        public void Fail(Exception error) => _choice.SetException(error);
    }

    private sealed class FakeRevealer : IFileRevealer
    {
        public string ActionLabel => "Show in Test";

        public List<string> Revealed { get; } = [];

        public string? Failure { get; set; }

        public bool TryReveal(string path, [NotNullWhen(false)] out string? error)
        {
            Revealed.Add(path);
            error = Failure;
            return error == null;
        }
    }
}
