using Plumb.App.Screens.Failed;
using Plumb.App.Screens.Model;
using Plumb.App.Screens.Start;
using Plumb.Core.Import;
using static Plumb.Tests.MainWindowHarness;

namespace Plumb.Tests;

[TestFixture]
public sealed class MainWindowFailureTests
{
    private MainWindowHarness _shell = null!;

    [SetUp]
    public void CreateShell() => _shell = new MainWindowHarness();

    [Test]
    public async Task FailedImportShowsTheFailureWithTheFileAndReason()
    {
        _shell.Import.Next = new ImportResult.Failure(ImportError.NotIfc, "Not an .ifc file: a.txt");

        await _shell.OpenAsync("/models/a.txt");

        var failed = _shell.Current<FailedViewModel>();
        Assert.That(failed.FileName, Is.EqualTo("a.txt"));
        Assert.That(failed.Message, Is.EqualTo("Not an .ifc file: a.txt"));
    }

    [Test]
    public async Task AFailedOpenIsNotRemembered()
    {
        _shell.Import.Next = new ImportResult.Failure(ImportError.ParseFailed, "broken");

        await _shell.OpenAsync("a.ifc");

        Assert.That(_shell.Recent.SaveCount, Is.Zero);
    }

    [Test]
    public async Task TryAgainOpensTheSameFile()
    {
        _shell.Import.Next = new ImportResult.Failure(ImportError.ParseFailed, "broken");
        await _shell.OpenAsync("/models/a.ifc");
        _shell.Import.Next = Loaded;

        await _shell.Current<FailedViewModel>().TryAgainCommand.ExecuteAsync(null).WaitAsync(TestTimeout);

        Assert.That(_shell.Import.Calls.Select(c => c.Path), Is.EqualTo(new[] { "/models/a.ifc", "/models/a.ifc" }));
        Assert.That(_shell.ViewModel.CurrentState, Is.TypeOf<LoadedViewModel>());
    }

    [Test]
    public async Task PickerFailureShowsError()
    {
        _shell.Picker.Fail(new InvalidOperationException("no window"));

        await _shell.ViewModel.OpenIfcCommand.ExecuteAsync(null).WaitAsync(TestTimeout);

        Assert.That(_shell.Current<EmptyStateViewModel>().ErrorMessage, Does.Contain("no window"));
    }

    [Test]
    public async Task CancelReturnsToPreviouslyLoadedModel()
    {
        _shell.Import.Next = Loaded;
        await _shell.OpenAsync("a.ifc");
        var loaded = _shell.ViewModel.CurrentState;

        await _shell.CancelBlockingImportAsync("b.ifc");

        Assert.That(_shell.ViewModel.CurrentState, Is.SameAs(loaded));
    }

    [Test]
    public async Task CancelAfterFailureShowsEmptyWithoutStaleError()
    {
        _shell.Import.Next = new ImportResult.Failure(ImportError.ParseFailed, "broken");
        await _shell.OpenAsync("a.ifc");

        await _shell.CancelBlockingImportAsync("b.ifc");

        Assert.That(_shell.Current<EmptyStateViewModel>().HasError, Is.False);
    }
}
