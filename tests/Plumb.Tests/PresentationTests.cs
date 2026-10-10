using Plumb.App.Formatting;
using Plumb.App.Screens.Importing;
using Plumb.App.Screens.Model.Tree;
using Plumb.Core.Import;
using Plumb.Core.Model;
using Plumb.Core.Tree;

namespace Plumb.Tests;

[TestFixture]
public sealed class PresentationTests
{
    private static readonly ElementRecord[] Records =
    [
        new("P", "IfcProject", "0001", null, null),
        new("S", "IfcSite", "Default", "P", null),
        new("B", "IfcBuilding", null, "S", null),
        new("L1", "IfcBuildingStorey", "Level 1", "B", "L1", Elevation: 3.1),
        new("W1", "IfcWallStandardCase", "Basic Wall:Exterior - Brick:138062", "L1", "L1", Tag: "138062"),
        new("W2", "IfcWallStandardCase", "Basic Wall:Interior:138063", "L1", "L1"),
        new("ST", "IfcStair", "Stair", "L1", "L1"),
        new("SF", "IfcStairFlight", "Flight", "ST", "L1"),
    ];

    [TestCase("Basic Wall:Exterior - Brick on Block:138062", "Exterior - Brick on Block:138062")]
    [TestCase("M_Single-Flush: 0762 x 2032mm", "0762 x 2032mm")]
    [TestCase("Level 1", "Level 1")]
    [TestCase(":leading", ":leading")]
    [TestCase("trailing:", "trailing:")]
    public void ElementLabelDropsTheRevitFamily(string name, string expected)
    {
        Assert.That(ElementLabel.WithoutFamily(name), Is.EqualTo(expected));
    }

    [TestCase(15, "15 ms")]
    [TestCase(999, "999 ms")]
    [TestCase(1234, "1.2 s")]
    public void DurationsUseMillisecondsBelowASecond(int milliseconds, string expected)
    {
        Assert.That(Durations.Format(TimeSpan.FromMilliseconds(milliseconds)), Is.EqualTo(expected));
    }

    [TestCase(6.1, "+6.100")]
    [TestCase(0d, "±0.000")]
    [TestCase(-0.0004, "±0.000")]
    [TestCase(-1.22, "\u22121.220")]
    public void ElevationsAreMetresWithThreeDecimalsAndATrueMinus(double metres, string expected)
    {
        Assert.That(Elevations.Format(metres), Is.EqualTo(expected));
    }

    [Test]
    public void TreeRowsFollowTheDesignPerLevel()
    {
        var project = Row(ModelTreeBuilder.Build(Records).Single());
        var site = project.Children.Single();
        var building = site.Children.Single();
        var storey = building.Children.Single();
        var walls = storey.Children.Single(c => c.Title == "IfcWallStandardCase");
        var wall = walls.Children.First();

        Assert.That((project.Title, project.Subtitle, project.Indent), Is.EqualTo(("IfcProject", "\"0001\"", 10d)));
        Assert.That((site.Title, site.Subtitle, site.Indent), Is.EqualTo(("IfcSite", "\"Default\"", 24d)));
        Assert.That((building.Title, building.Subtitle, building.Indent), Is.EqualTo(("IfcBuilding", (string?)null, 38d)));
        Assert.That((storey.Title, storey.Subtitle, storey.Meta, storey.Indent, storey.IsStrong), Is.EqualTo(("Level 1", "+3.100", "4", 52d, true)));
        Assert.That((walls.Meta, walls.Indent, walls.IsStrong), Is.EqualTo(("2", 66d, false)));
        Assert.That((wall.Title, wall.Label, wall.Meta, wall.Indent), Is.EqualTo(("Exterior - Brick:138062", "Basic Wall:Exterior - Brick:138062", "#138062", 86d)));
    }

    [Test]
    public void TypeGroupKeysAreStableAcrossRebuilds()
    {
        var first = Row(ModelTreeBuilder.Build(Records).Single());
        var second = Row(ModelTreeFilter.Apply(ModelTreeBuilder.Build(Records), "wall").Single());

        string GroupKey(TreeNodeViewModel root) =>
            root.Children.Single().Children.Single().Children.Single().Children.Single(c => c.Title == "IfcWallStandardCase").Key;

        Assert.That(GroupKey(second), Is.EqualTo(GroupKey(first)));
    }

    [Test]
    public void ImportStepsMoveFromPendingToRunningToDone()
    {
        using var cancellation = new CancellationTokenSource();
        var importing = new ImportingViewModel("Duplex.ifc", opensPackage: false, cancellation);

        importing.Report(new ImportProgress(ImportStep.Validating, 0));
        importing.Report(new ImportProgress(ImportStep.ReadingModel, 30));

        Assert.That(importing.Steps.Select(s => s.State), Is.EqualTo(new[]
        {
            StepState.Done, StepState.Done, StepState.Running, StepState.Pending, StepState.Pending, StepState.Pending,
        }));
        Assert.That(importing.Steps[2].StatusText, Is.EqualTo("running"));
        Assert.That(importing.Steps[3].StatusText, Is.EqualTo("—"));
        Assert.That(importing.Percent, Is.EqualTo(30));
    }

    [Test]
    public void TheLastStepFinishesAtHundredPercent()
    {
        using var cancellation = new CancellationTokenSource();
        var importing = new ImportingViewModel("Duplex.ifc", opensPackage: false, cancellation);

        importing.Report(new ImportProgress(ImportStep.Finalizing, 100));

        Assert.That(importing.Steps.Select(s => s.IsDone), Has.All.True);
    }

    [Test]
    public void OpeningAPackageHasOneStep()
    {
        using var cancellation = new CancellationTokenSource();
        var opening = new ImportingViewModel("Duplex.plumb", opensPackage: true, cancellation);

        Assert.That(opening.Kicker, Is.EqualTo("OPENING"));
        Assert.That(opening.Steps.Select(s => s.Name), Is.EqualTo(new[] { "Open package" }));
    }

    private static TreeNodeViewModel Row(ModelTreeNode node) =>
        TreeNodeViewModel.From(node, string.Empty, 0, (_, _) => true, (_, _) => { });
}
