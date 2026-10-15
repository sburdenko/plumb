using Plumb.App.Screens.Model;
using Plumb.Core.Geometry;
using Plumb.Core.Import;
using static Plumb.Tests.MainWindowHarness;

namespace Plumb.Tests;

[TestFixture]
public sealed class MainWindowLoadedModelTests
{
    private MainWindowHarness _shell = null!;

    [SetUp]
    public void CreateShell() => _shell = new MainWindowHarness();

    [Test]
    public async Task SavedPackageHasNoWarning()
    {
        var loaded = await LoadAsync(Loaded);

        Assert.That(loaded.Warning, Is.Null);
        Assert.That(loaded.CanReveal, Is.True);
    }

    [Test]
    public async Task UnsavedPackageShowsAWarningAndHidesReveal()
    {
        var loaded = await LoadAsync(Loaded with { Package = new PackageState.NotSaved("Could not save a.plumb: read-only") });

        Assert.That(loaded.Warning, Is.EqualTo("Could not save a.plumb: read-only"));
        Assert.That(loaded.CanReveal, Is.False);
        Assert.That(loaded.RevealCommand.CanExecute(null), Is.False);
    }

    [Test]
    public async Task MissingGeometryShowsAWarningButKeepsReveal()
    {
        var geometry = new GeometryState.NotBuilt(GeometryError.ConverterMissing, "not found at /app/tools/ifcconvert/IfcConvert");

        var loaded = await LoadAsync(Loaded with { Package = new PackageState.Saved("/models/a.plumb", geometry) });

        Assert.That(loaded.Warning, Is.EqualTo("3D geometry was not built: IfcConvert is not installed next to Plumb."));
        Assert.That(loaded.CanReveal, Is.True);
    }

    [Test]
    public async Task RevealShowsThePackageInTheFileManager()
    {
        var loaded = await LoadAsync(Loaded);

        loaded.RevealCommand.Execute(null);

        Assert.That(loaded.RevealLabel, Is.EqualTo("Show in Test"));
        Assert.That(_shell.Revealer.Revealed, Is.EqualTo(new[] { "/models/a.plumb" }));
    }

    [Test]
    public async Task FailedRevealShowsTheReason()
    {
        var loaded = await LoadAsync(Loaded);
        _shell.Revealer.Failure = "The package no longer exists.";

        loaded.RevealCommand.Execute(null);

        Assert.That(loaded.ActionError, Is.EqualTo("The package no longer exists."));
    }

    [Test]
    public async Task OpenIn3DStartsTheViewerWithThePackage()
    {
        var loaded = await LoadAsync(Loaded);

        loaded.OpenIn3DCommand.Execute(null);

        Assert.That(_shell.Viewer.Opened, Is.EqualTo(new[] { "/models/a.plumb" }));
        Assert.That(loaded.ActionError, Is.Null);
    }

    [Test]
    public async Task OpenIn3DIsUnavailableWithoutGeometry()
    {
        var geometry = new GeometryState.NotBuilt(GeometryError.ConverterFailed, "exit code 1");

        var loaded = await LoadAsync(Loaded with { Package = new PackageState.Saved("/models/a.plumb", geometry) });

        Assert.That(loaded.CanOpenIn3D, Is.False);
        Assert.That(loaded.OpenIn3DCommand.CanExecute(null), Is.False);
    }

    [Test]
    public async Task OpenIn3DIsUnavailableWhenThePackageWasNotSaved()
    {
        var loaded = await LoadAsync(Loaded with { Package = new PackageState.NotSaved("read-only") });

        Assert.That(loaded.CanOpenIn3D, Is.False);
    }

    [Test]
    public async Task MissingViewerShowsTheReason()
    {
        var loaded = await LoadAsync(Loaded);
        _shell.Viewer.Failure = "The 3D viewer was not found.";

        loaded.OpenIn3DCommand.Execute(null);

        Assert.That(loaded.ActionError, Is.EqualTo("The 3D viewer was not found."));
    }

    private async Task<LoadedViewModel> LoadAsync(ImportResult result)
    {
        _shell.Import.Next = result;
        await _shell.OpenAsync("a.ifc");
        return _shell.Current<LoadedViewModel>();
    }
}
