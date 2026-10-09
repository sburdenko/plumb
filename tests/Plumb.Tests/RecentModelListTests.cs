using Plumb.App.Recent;

namespace Plumb.Tests;

[TestFixture]
public sealed class RecentModelListTests
{
    private static readonly DateTimeOffset Monday = new(2026, 10, 5, 9, 0, 0, TimeSpan.Zero);

    [Test]
    public void NewestOpenedComesFirst()
    {
        var list = RecentModelList.Empty
            .Record(Model("a", Monday))
            .Record(Model("b", Monday.AddHours(1)));

        Assert.That(Names(list), Is.EqualTo(new[] { "b", "a" }));
    }

    [Test]
    public void OpeningAgainMovesTheModelUpWithoutADuplicate()
    {
        var list = RecentModelList.Empty
            .Record(Model("a", Monday))
            .Record(Model("b", Monday.AddHours(1)))
            .Record(Model("a", Monday.AddHours(2)));

        Assert.That(Names(list), Is.EqualTo(new[] { "a", "b" }));
    }

    [Test]
    public void AnIfcFileAndItsPackageAreOneModel()
    {
        var imported = new RecentModel("/m/Duplex.ifc", "/m/Duplex.plumb", "Duplex", "IFC2X3", 246, Monday, IsPinned: false);
        var reopened = new RecentModel(null, "/m/Duplex.plumb", "Duplex", "IFC2X3", 246, Monday.AddDays(1), IsPinned: false);

        var list = RecentModelList.Empty.Record(imported).Record(reopened);

        Assert.That(list.Models, Is.EqualTo(new[] { reopened }));
    }

    [Test]
    public void PinnedModelsStayAboveNewerOnes()
    {
        var list = RecentModelList.Empty.Record(Model("a", Monday));
        list = list.SetPinned(list.Models[0], pinned: true)
            .Record(Model("b", Monday.AddHours(1)))
            .Record(Model("c", Monday.AddHours(2)));

        Assert.That(Names(list), Is.EqualTo(new[] { "a", "c", "b" }));
    }

    [Test]
    public void OpeningAPinnedModelKeepsItPinned()
    {
        var list = RecentModelList.Empty.Record(Model("a", Monday));
        list = list.SetPinned(list.Models[0], pinned: true).Record(Model("a", Monday.AddHours(1)));

        Assert.That(list.Models.Single().IsPinned, Is.True);
    }

    [Test]
    public void UnpinningPutsTheModelBackInTimeOrder()
    {
        var list = RecentModelList.Empty.Record(Model("a", Monday)).Record(Model("b", Monday.AddHours(1)));
        list = list.SetPinned(list.Models[1], pinned: true);
        list = list.SetPinned(list.Models[0], pinned: false);

        Assert.That(Names(list), Is.EqualTo(new[] { "b", "a" }));
    }

    [Test]
    public void RemoveDropsOnlyThatModel()
    {
        var list = RecentModelList.Empty.Record(Model("a", Monday)).Record(Model("b", Monday.AddHours(1)));

        list = list.Remove(list.Models[0]);

        Assert.That(Names(list), Is.EqualTo(new[] { "a" }));
    }

    [Test]
    public void OnlyTheNewestUnpinnedModelsAreKept()
    {
        var list = RecentModelList.Empty.Record(Model("pinned", Monday.AddYears(-1)));
        list = list.SetPinned(list.Models[0], pinned: true);
        for (var i = 0; i < RecentModelList.MaxUnpinned + 5; i++)
        {
            list = list.Record(Model($"m{i}", Monday.AddMinutes(i)));
        }

        Assert.That(list.Models, Has.Count.EqualTo(RecentModelList.MaxUnpinned + 1));
        Assert.That(list.Models[0].Name, Is.EqualTo("pinned"));
        Assert.That(list.Models[1].Name, Is.EqualTo($"m{RecentModelList.MaxUnpinned + 4}"));
        Assert.That(Names(list), Does.Not.Contain("m0"));
    }

    [Test]
    public void OfSortsAndDropsDuplicatesKeepingTheNewest()
    {
        var older = Model("a", Monday);
        var newer = Model("a", Monday.AddHours(3));

        var list = RecentModelList.Of([older, Model("b", Monday.AddHours(1)), newer]);

        Assert.That(list.Models[0], Is.EqualTo(newer));
        Assert.That(Names(list), Is.EqualTo(new[] { "a", "b" }));
    }

    private static RecentModel Model(string name, DateTimeOffset opened) =>
        new($"/models/{name}.ifc", $"/models/{name}.plumb", name, "IFC4", 10, opened, IsPinned: false);

    private static IEnumerable<string> Names(RecentModelList list) => list.Models.Select(m => m.Name);
}
