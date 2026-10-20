using System.Numerics;
using Plumb.App.Screens.Model.Clashes;
using Plumb.App.Screens.Model.Filter;
using Plumb.Core.Clashes;
using Plumb.Core.Geometry;
using Plumb.Core.Import;
using Plumb.Core.Model;
using Plumb.Core.Search;

namespace Plumb.Tests;

[TestFixture]
public sealed class ClashesViewModelTests
{
    private const string Package = "/m/House.plumb";
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(5);

    private static readonly ElementRecord[] Elements =
    [
        new("W1", "IfcWall", "Basic Wall:Exterior - Brick", null, null),
        new("W2", "IfcWall", "Basic Wall:Interior", null, null),
        new("S1", "IfcSlab", "Floor:Concrete 200", null, null),
        new("D1", "IfcDuctSegment", "Rectangular Duct:600x400", null, null),
    ];

    private FakeClashService _service = null!;
    private List<string> _selected = null!;

    [SetUp]
    public void CreateFakes()
    {
        _service = new FakeClashService();
        _selected = [];
    }

    [Test]
    public void StartsIdleAndCannotSearchWithoutGroups()
    {
        var clashes = ViewModel();

        Assert.That(clashes.State, Is.TypeOf<ClashesIdleViewModel>());
        Assert.That(clashes.FindCommand.CanExecute(null), Is.False);
    }

    [Test]
    public void EachGroupSaysHowManyElementsItMatches()
    {
        var clashes = ViewModel();

        clashes.FirstQuery = "IfcWall";
        clashes.SecondQuery = "Area >";

        Assert.That(clashes.FirstStatus, Is.TypeOf<FilterMatchedViewModel>());
        Assert.That(clashes.FirstStatus!.Text, Is.EqualTo("2 elements match"));
        Assert.That(clashes.SecondStatus, Is.TypeOf<FilterRejectedViewModel>());
    }

    [Test]
    public void SearchingNeedsBothGroupsToMatchSomething()
    {
        var clashes = ViewModel();
        clashes.FirstQuery = "IfcWall";
        clashes.SecondQuery = "IfcBeam";
        Assert.That(clashes.FindCommand.CanExecute(null), Is.False);

        clashes.SecondQuery = "IfcSlab";

        Assert.That(clashes.FindCommand.CanExecute(null), Is.True);
    }

    [Test]
    public async Task TheSearchCoversThePackageAndTheMatchedElements()
    {
        var clashes = Ready("IfcWall", "IfcSlab");
        _service.Next = new ClashResult.Found([], TimeSpan.Zero);

        await clashes.FindCommand.ExecuteAsync(null).WaitAsync(Patience);

        Assert.That(_service.Calls, Is.EqualTo(new[] { (Package, 2, 1) }));
    }

    [Test]
    public async Task ClashesAreListedWithBothElementsAndHowDeepTheyGo()
    {
        var clashes = Ready("IfcWall", "IfcSlab");
        _service.Next = new ClashResult.Found(
            [new Clash("W1", "S1", 0.168f, Vector3.Zero), new Clash("W2", "S1", 0.02f, Vector3.Zero)],
            TimeSpan.FromMilliseconds(97));

        await clashes.FindCommand.ExecuteAsync(null).WaitAsync(Patience);

        var found = (ClashesFoundViewModel)clashes.State;
        Assert.That(found.Summary, Is.EqualTo("2 clashes"));
        var first = found.Rows[0];
        Assert.That((first.Penetration, first.FirstName, first.FirstType, first.SecondName), Is.EqualTo(("16.8 cm", "Exterior - Brick", "IfcWall", "Concrete 200")));
        Assert.That(first.Description, Is.EqualTo("Basic Wall:Exterior - Brick (IfcWall)\n× Floor:Concrete 200 (IfcSlab)\n16.8 cm into each other"));
    }

    [Test]
    public async Task NoClashesIsSaidPlainly()
    {
        var clashes = Ready("IfcWall", "IfcSlab");
        _service.Next = new ClashResult.Found([], TimeSpan.Zero);

        await clashes.FindCommand.ExecuteAsync(null).WaitAsync(Patience);

        Assert.That(clashes.State, Is.TypeOf<ClashesNoneViewModel>());
    }

    [Test]
    public async Task AFailedSearchShowsWhy()
    {
        var clashes = Ready("IfcWall", "IfcSlab");
        _service.Next = new ClashResult.Failed("Cannot read model.glb: broken");

        await clashes.FindCommand.ExecuteAsync(null).WaitAsync(Patience);

        Assert.That(((ClashesFailedViewModel)clashes.State).Message, Is.EqualTo("Cannot read model.glb: broken"));
    }

    [Test]
    public async Task ARunningSearchShowsItsStepAndCanBeCancelled()
    {
        var clashes = Ready("IfcWall", "IfcSlab");

        var search = clashes.FindCommand.ExecuteAsync(null);

        Assert.That(((ClashesSearchingViewModel)clashes.State).Step, Is.EqualTo("Reading 3D geometry…"));
        Assert.That(clashes.FindCommand.CanExecute(null), Is.False);
        clashes.FindCommand.Cancel();
        await search.WaitAsync(Patience);
        Assert.That(clashes.State, Is.TypeOf<ClashesIdleViewModel>());
    }

    [Test]
    public async Task ChoosingAClashSelectsItsFirstElement()
    {
        var clashes = Ready("IfcWall", "IfcSlab");
        _service.Next = new ClashResult.Found([new Clash("W1", "S1", 0.1f, Vector3.Zero)], TimeSpan.Zero);
        await clashes.FindCommand.ExecuteAsync(null).WaitAsync(Patience);
        var found = (ClashesFoundViewModel)clashes.State;

        found.SelectedRow = found.Rows[0];

        Assert.That(_selected, Is.EqualTo(new[] { "W1" }));
    }

    [Test]
    public void WithoutGeometryThereIsNothingToSearch()
    {
        var geometry = new GeometryState.NotBuilt(GeometryError.ConverterMissing, "not found");
        var clashes = ViewModel(new PackageState.Saved(Package, geometry));
        clashes.FirstQuery = "IfcWall";
        clashes.SecondQuery = "IfcSlab";

        Assert.That(clashes.State, Is.TypeOf<ClashesUnavailableViewModel>());
        Assert.That(clashes.FindCommand.CanExecute(null), Is.False);
    }

    [Test]
    public void AnUnsavedModelHasNothingToSearch()
    {
        var clashes = ViewModel(new PackageState.NotSaved("read-only"));

        Assert.That(((ClashesUnavailableViewModel)clashes.State).Reason, Does.Contain("not saved"));
    }

    private ClashesViewModel Ready(string first, string second)
    {
        var clashes = ViewModel();
        clashes.FirstQuery = first;
        clashes.SecondQuery = second;
        return clashes;
    }

    private ClashesViewModel ViewModel(PackageState? package = null) => new(
        package ?? new PackageState.Saved(Package, new GeometryState.Built()),
        Elements,
        new ElementSearch(Elements, []),
        _service,
        _selected.Add);
}
