using CommunityToolkit.Mvvm.Input;
using Plumb.App.Screens.Model;
using Plumb.App.Screens.Start.Recent;
using Plumb.Core.Geometry;
using Plumb.Core.Import;
using Plumb.Core.Model;
using Plumb.Core.Package;

namespace Plumb.Tests;

[TestFixture]
public sealed class RecentModelsViewModelTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 9, 15, 30, 0, TimeSpan.FromHours(3));

    private TempDirectory _folder = null!;
    private FakeRecentModelStore _store = null!;
    private FakeRevealer _revealer = null!;
    private List<string?> _opened = null!;

    [SetUp]
    public void CreateFakes()
    {
        _folder = new TempDirectory();
        _store = new FakeRecentModelStore();
        _revealer = new FakeRevealer();
        _opened = [];
    }

    [TearDown]
    public void RemoveFolder() => _folder.Dispose();

    [Test]
    public void StartsEmptyWithoutModels()
    {
        var recent = ViewModel();

        Assert.That(recent.IsEmpty, Is.True);
        Assert.That(recent.Pinned, Is.Empty);
        Assert.That(recent.Recent, Is.Empty);
    }

    [Test]
    public void ALoadedModelIsRememberedAndSaved()
    {
        var recent = ViewModel();
        var ifc = Path.Combine(_folder.Path, "Duplex.ifc");
        var package = Path.Combine(_folder.Path, "Duplex.plumb");

        recent.Record(new LoadedModel(Success(package), ifc, OpenedPackage: false, TimeSpan.FromSeconds(1)));

        var saved = _store.Saved.Models.Single();
        Assert.That(saved, Is.EqualTo(new RecentModel(ifc, package, "Duplex", "IFC2X3", 2, Now, IsPinned: false)));
        Assert.That(recent.Recent.Single().Title, Is.EqualTo("Duplex"));
        Assert.That(recent.IsEmpty, Is.False);
    }

    [Test]
    public void PinnedModelsHaveTheirOwnSection()
    {
        Seed(Model("a", 1, pinned: true), Model("b", 2), Model("c", 3));

        var recent = ViewModel();

        Assert.That(recent.Pinned.Select(i => i.Title), Is.EqualTo(new[] { "a" }));
        Assert.That(recent.Recent.Select(i => i.Title), Is.EqualTo(new[] { "b", "c" }));
    }

    [Test]
    public void PinningMovesTheModelAndIsSaved()
    {
        Seed(Model("a", 1), Model("b", 2));
        var recent = ViewModel();

        recent.Recent[1].TogglePinCommand.Execute(null);

        Assert.That(recent.Pinned.Select(i => i.Title), Is.EqualTo(new[] { "b" }));
        Assert.That(recent.Pinned[0].IsPinned, Is.True);
        Assert.That(_store.Saved.Models[0].IsPinned, Is.True);
    }

    [Test]
    public void ShowsTheTenNewestUntilAskedForAll()
    {
        Seed(Enumerable.Range(1, 14).Select(i => Model($"m{i}", i)).ToArray());
        var recent = ViewModel();

        Assert.That(recent.Recent, Has.Count.EqualTo(RecentModelsViewModel.ShownByDefault));
        Assert.That(recent.ShowAllText, Is.EqualTo("Show all 14"));

        recent.ShowAllCommand.Execute(null);

        Assert.That(recent.Recent, Has.Count.EqualTo(14));
        Assert.That(recent.HasHidden, Is.False);
    }

    [Test]
    public void TheFilterMatchesNamesAndFoldersAndShowsEveryMatch()
    {
        var models = Enumerable.Range(1, 12).Select(i => Model($"tower{i}", i)).Append(Model("Duplex", 13)).ToArray();
        Seed(models);
        var recent = ViewModel();

        recent.Filter = "TOWER";
        Assert.That(recent.Recent, Has.Count.EqualTo(12));

        recent.Filter = Path.GetFileName(_folder.Path);
        Assert.That(recent.Recent, Has.Count.EqualTo(13));

        recent.Filter = "nothing";
        Assert.That(recent.Recent, Is.Empty);
        Assert.That(recent.HasNoMatches, Is.True);
    }

    [Test]
    public void TheFilterAppearsOnlyForALongList()
    {
        Seed(Model("a", 1));
        Assert.That(ViewModel().CanFilter, Is.False);

        Seed(Enumerable.Range(1, RecentModelsViewModel.ShownByDefault + 1).Select(i => Model($"m{i}", i)).ToArray());
        Assert.That(ViewModel().CanFilter, Is.True);
    }

    [Test]
    public async Task OpeningAModelOpensItsPackage()
    {
        var model = Model("Duplex", 1);
        CreatePackage(model.PackagePath!);
        Seed(model);
        var recent = ViewModel();

        await recent.Recent[0].OpenCommand.ExecuteAsync(null);

        Assert.That(_opened, Is.EqualTo(new[] { model.PackagePath }));
    }

    [Test]
    public void AMissingModelCannotBeOpenedButCanBeRemoved()
    {
        Seed(Model("gone", 1));
        var recent = ViewModel();
        var item = recent.Recent[0];

        Assert.That(item.IsMissing, Is.True);
        Assert.That(item.OpenCommand.CanExecute(null), Is.False);
        Assert.That(item.RevealCommand.CanExecute(null), Is.False);

        item.RemoveCommand.Execute(null);

        Assert.That(recent.IsEmpty, Is.True);
        Assert.That(_store.Saved.Models, Is.Empty);
    }

    [Test]
    public void RevealShowsTheFileThatWouldOpen()
    {
        var model = Model("Duplex", 1);
        CreatePackage(model.PackagePath!);
        Seed(model);
        var recent = ViewModel();

        recent.Recent[0].RevealCommand.Execute(null);

        Assert.That(_revealer.Revealed, Is.EqualTo(new[] { model.PackagePath }));
    }

    [Test]
    public void RowsDescribeTheModel()
    {
        Seed(Model("Duplex", 2) with { ElementCount = 1246 });

        var item = ViewModel().Recent[0];

        Assert.That(item.Schema, Is.EqualTo("IFC4"));
        Assert.That(item.Elements, Is.EqualTo("1,246 elements"));
        Assert.That(item.LastOpened, Is.EqualTo("2 h ago"));
        Assert.That(item.Location, Does.EndWith(Path.GetFileName(_folder.Path)));
    }

    private RecentModelsViewModel ViewModel()
    {
        var open = new AsyncRelayCommand<string?>(path =>
        {
            _opened.Add(path);
            return Task.CompletedTask;
        });
        return new RecentModelsViewModel(_store, open, _revealer, new FixedClock(Now));
    }

    private void Seed(params RecentModel[] models) => _store.Save(RecentModelList.Of(models));

    private RecentModel Model(string name, int hoursAgo, bool pinned = false) =>
        new(Path.Combine(_folder.Path, name + ".ifc"), Path.Combine(_folder.Path, name + ".plumb"), name, "IFC4", 10, Now.AddHours(-hoursAgo), pinned);

    private static void CreatePackage(string path)
    {
        Directory.CreateDirectory(path);
        File.WriteAllText(Path.Combine(path, PackageLayout.ManifestFile), "{}");
    }

    private static ImportResult.Success Success(string packagePath) => new(
        new IfcModelData("Duplex.ifc", "IFC2X3",
            [new ElementRecord("P", "IfcProject", "Project", null, null), new ElementRecord("W", "IfcWall", "Wall", null, null)], []),
        TimeSpan.FromSeconds(1),
        new PackageState.Saved(packagePath, new GeometryState.Built()));
}
