using Plumb.App.Screens.Failed;
using Plumb.App.Screens.Importing;
using Plumb.App.Screens.Model;
using Plumb.App.Screens.Start;
using Plumb.Core.Import;
using static Plumb.Tests.MainWindowHarness;

namespace Plumb.Tests;

[TestFixture]
public sealed class MainWindowOpeningTests
{
    private MainWindowHarness _shell = null!;

    [SetUp]
    public void CreateShell() => _shell = new MainWindowHarness();

    [Test]
    public void StartsEmptyWithoutError()
    {
        Assert.That(_shell.ViewModel.CurrentState, Is.TypeOf<EmptyStateViewModel>());
        Assert.That(_shell.Current<EmptyStateViewModel>().HasError, Is.False);
    }

    [Test]
    public void TheEmptyStateListsRecentModels()
    {
        Assert.That(_shell.Current<EmptyStateViewModel>().Recent, Is.Not.Null);
    }

    [Test]
    public async Task SuccessfulImportShowsLoadedModel()
    {
        _shell.Import.Next = Loaded;

        await _shell.OpenAsync("a.ifc");

        Assert.That(_shell.ViewModel.CurrentState, Is.TypeOf<LoadedViewModel>());
        Assert.That(_shell.Current<LoadedViewModel>().FileName, Is.EqualTo("a.ifc"));
    }

    [Test]
    public async Task ASuccessfulOpenIsRemembered()
    {
        _shell.Import.Next = Loaded;

        await _shell.OpenAsync("a.ifc");

        Assert.That(_shell.Recent.Saved.Models.Single().PackagePath, Is.EqualTo(Path.GetFullPath("/models/a.plumb")));
    }

    [Test]
    public async Task IfcFileIsImportedIntoItsOwnFolder()
    {
        _shell.Import.Next = Loaded;
        var ifc = Path.Combine(Path.GetTempPath(), "models", "a.ifc");

        await _shell.OpenAsync(ifc);

        Assert.That(_shell.Import.Calls, Is.EqualTo(new[] { ("import", ifc, Path.GetDirectoryName(ifc)) }));
    }

    [Test]
    public async Task PackageIsOpenedNotImported()
    {
        _shell.Import.Next = Loaded;

        await _shell.OpenAsync("/models/a.plumb");

        Assert.That(_shell.Import.Calls, Is.EqualTo(new[] { ("open", "/models/a.plumb", (string?)null) }));
        Assert.That(_shell.ViewModel.CurrentState, Is.TypeOf<LoadedViewModel>());
    }

    [Test]
    public async Task OpeningAPackageSaysOpenedAndImportingSaysImported()
    {
        _shell.Import.Next = Loaded;
        await _shell.OpenAsync("/models/a.plumb");
        var opened = _shell.Current<LoadedViewModel>();
        _shell.Import.Next = Loaded;
        await _shell.OpenAsync("/models/a.ifc");
        var imported = _shell.Current<LoadedViewModel>();

        Assert.That(opened.LoadLabel, Is.EqualTo("Opened in"));
        Assert.That(opened.FileName, Is.EqualTo("a.plumb"));
        Assert.That(imported.LoadLabel, Is.EqualTo("Imported in"));
    }

    [Test]
    public async Task AnOutdatedPackageIsImportedAgainFromItsIfcFile()
    {
        var ifc = Path.GetFullPath("/models/a.ifc");
        _shell.Import.Next = new ImportResult.PackageOutdated("a.ifc", ifc);
        _shell.Import.Next = Loaded;

        await _shell.OpenAsync("/models/a.plumb");

        Assert.That(_shell.Import.Calls.Select(call => (call.Method, call.Path)),
            Is.EqualTo(new[] { ("open", "/models/a.plumb"), ("import", ifc) }));
        Assert.That(_shell.ViewModel.CurrentState, Is.TypeOf<LoadedViewModel>());
        Assert.That(_shell.Current<LoadedViewModel>().FileName, Is.EqualTo("a.ifc"));
    }

    [Test]
    public async Task AnOutdatedPackageWithoutItsIfcFileExplainsWhatToDo()
    {
        _shell.Import.Next = new ImportResult.PackageOutdated("a.ifc", SourcePath: null);

        await _shell.OpenAsync("/models/a.plumb");

        Assert.That(_shell.Current<FailedViewModel>().Message, Does.Contain("earlier version of Plumb").And.Contain("a.ifc"));
    }

    [Test]
    public async Task OpenPackageButtonOpensThePickedFolder()
    {
        _shell.Import.Next = Loaded;
        var browse = _shell.ViewModel.OpenPackageCommand.ExecuteAsync(null);

        _shell.Picker.Choose("/models/a.plumb");
        await browse.WaitAsync(TestTimeout);

        Assert.That(_shell.Import.Calls.Single().Method, Is.EqualTo("open"));
    }

    [Test]
    public async Task PickerCancelDoesNotImport()
    {
        var browse = _shell.ViewModel.OpenIfcCommand.ExecuteAsync(null);
        _shell.Picker.Choose(null);
        await browse.WaitAsync(TestTimeout);

        Assert.That(_shell.Import.Calls, Is.Empty);
    }

    [Test]
    public async Task FileDroppedWhilePickerIsOpenIsImportedOnce()
    {
        var browse = _shell.ViewModel.OpenIfcCommand.ExecuteAsync(null);
        var drop = _shell.ViewModel.OpenPathCommand.ExecuteAsync("dropped.ifc");

        _shell.Picker.Choose("picked.ifc");
        await browse.WaitAsync(TestTimeout);
        _shell.Import.Release(Loaded);
        await drop.WaitAsync(TestTimeout);

        Assert.That(_shell.Import.Calls.Select(c => c.Path), Is.EqualTo(new[] { "dropped.ifc" }));
    }

    [Test]
    public async Task CommandsAreDisabledWhileImporting()
    {
        var import = _shell.ViewModel.OpenPathCommand.ExecuteAsync("a.ifc");

        Assert.That(_shell.ViewModel.CurrentState, Is.TypeOf<ImportingViewModel>());
        Assert.That(_shell.ViewModel.OpenPathCommand.CanExecute("b.ifc"), Is.False);
        Assert.That(_shell.ViewModel.OpenIfcCommand.CanExecute(null), Is.False);
        Assert.That(_shell.ViewModel.OpenPackageCommand.CanExecute(null), Is.False);

        _shell.Import.Release(Loaded);
        await import.WaitAsync(TestTimeout);
        Assert.That(_shell.ViewModel.OpenPathCommand.CanExecute("b.ifc"), Is.True);
    }
}
