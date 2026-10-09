using Microsoft.Extensions.Logging.Abstractions;
using Plumb.App.Services;
using Plumb.App.ViewModels;
using Plumb.Core.Import;
using Plumb.Core.Model;

namespace Plumb.Tests;

[TestFixture]
public sealed class MainWindowViewModelTests
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(5);

    private static readonly ImportResult.Success Loaded = new(
        new IfcModelData("a.ifc", "IFC4", [new ElementRecord("P", "IfcProject", "Project", null, null)], []),
        TimeSpan.FromSeconds(1));

    private FakeImportService _importService = null!;
    private FakeFilePicker _picker = null!;
    private MainWindowViewModel _viewModel = null!;

    [SetUp]
    public void CreateViewModel()
    {
        _importService = new FakeImportService();
        _picker = new FakeFilePicker();
        _viewModel = new MainWindowViewModel(_importService, _picker, NullLogger<MainWindowViewModel>.Instance);
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

        await _viewModel.ImportFileCommand.ExecuteAsync("a.ifc").WaitAsync(TestTimeout);

        Assert.That(_viewModel.CurrentState, Is.TypeOf<LoadedViewModel>());
        Assert.That(((LoadedViewModel)_viewModel.CurrentState).FileName, Is.EqualTo("a.ifc"));
    }

    [Test]
    public async Task FailedImportShowsEmptyWithMessage()
    {
        _importService.Next = new ImportResult.Failure(ImportError.NotIfc, "Not an .ifc file: a.txt");

        await _viewModel.ImportFileCommand.ExecuteAsync("a.txt").WaitAsync(TestTimeout);

        var empty = (EmptyStateViewModel)_viewModel.CurrentState;
        Assert.That(empty.ErrorMessage, Is.EqualTo("Not an .ifc file: a.txt"));
    }

    [Test]
    public async Task CancelReturnsToPreviouslyLoadedModel()
    {
        _importService.Next = Loaded;
        await _viewModel.ImportFileCommand.ExecuteAsync("a.ifc").WaitAsync(TestTimeout);
        var loaded = _viewModel.CurrentState;

        await CancelBlockingImportAsync("b.ifc");

        Assert.That(_viewModel.CurrentState, Is.SameAs(loaded));
    }

    [Test]
    public async Task CancelAfterFailureShowsEmptyWithoutStaleError()
    {
        _importService.Next = new ImportResult.Failure(ImportError.ParseFailed, "broken");
        await _viewModel.ImportFileCommand.ExecuteAsync("a.ifc").WaitAsync(TestTimeout);

        await CancelBlockingImportAsync("b.ifc");

        Assert.That(((EmptyStateViewModel)_viewModel.CurrentState).HasError, Is.False);
    }

    [Test]
    public async Task CommandsAreDisabledWhileImporting()
    {
        var import = _viewModel.ImportFileCommand.ExecuteAsync("a.ifc");

        Assert.That(_viewModel.CurrentState, Is.TypeOf<ImportingViewModel>());
        Assert.That(_viewModel.ImportFileCommand.CanExecute("b.ifc"), Is.False);
        Assert.That(_viewModel.BrowseCommand.CanExecute(null), Is.False);

        _importService.Release(Loaded);
        await import.WaitAsync(TestTimeout);
        Assert.That(_viewModel.ImportFileCommand.CanExecute("b.ifc"), Is.True);
    }

    [Test]
    public async Task FileDroppedWhilePickerIsOpenIsImportedOnce()
    {
        var browse = _viewModel.BrowseCommand.ExecuteAsync(null);
        var drop = _viewModel.ImportFileCommand.ExecuteAsync("dropped.ifc");

        _picker.Choose("picked.ifc");
        await browse.WaitAsync(TestTimeout);
        _importService.Release(Loaded);
        await drop.WaitAsync(TestTimeout);

        Assert.That(_importService.Paths, Is.EqualTo(new[] { "dropped.ifc" }));
    }

    [Test]
    public async Task PickerFailureShowsError()
    {
        _picker.Fail(new InvalidOperationException("no window"));

        await _viewModel.BrowseCommand.ExecuteAsync(null).WaitAsync(TestTimeout);

        Assert.That(((EmptyStateViewModel)_viewModel.CurrentState).ErrorMessage, Does.Contain("no window"));
    }

    [Test]
    public async Task PickerCancelDoesNotImport()
    {
        var browse = _viewModel.BrowseCommand.ExecuteAsync(null);
        _picker.Choose(null);
        await browse.WaitAsync(TestTimeout);

        Assert.That(_importService.Paths, Is.Empty);
    }

    private async Task CancelBlockingImportAsync(string path)
    {
        var import = _viewModel.ImportFileCommand.ExecuteAsync(path);
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

        public List<string> Paths { get; } = [];

        public Task<ImportResult> RunAsync(string ifcPath, IProgress<ImportProgress> progress, CancellationToken cancellationToken)
        {
            Paths.Add(ifcPath);
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

        public void Release(ImportResult result) => _pending.ForEach(pending => pending.TrySetResult(result));
    }

    private sealed class FakeFilePicker : IFilePickerService
    {
        private readonly TaskCompletionSource<string?> _choice = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<string?> PickIfcFileAsync() => _choice.Task;

        public void Choose(string? path) => _choice.SetResult(path);

        public void Fail(Exception error) => _choice.SetException(error);
    }
}
