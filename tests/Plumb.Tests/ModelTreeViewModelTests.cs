using Plumb.App.Screens.Model.Filter;
using Plumb.App.Screens.Model.Tree;
using Plumb.Core.Model;
using Plumb.Core.Search;

namespace Plumb.Tests;

[TestFixture]
public sealed class ModelTreeViewModelTests
{
    private static readonly ElementRecord[] Elements =
    [
        new ElementRecord("P", "IfcProject", "House project", null, null),
        new ElementRecord("B", "IfcBuilding", "Duplex Residence", "P", null),
        new ElementRecord("L1", "IfcBuildingStorey", "Level 1", "B", "L1"),
        new ElementRecord("L2", "IfcBuildingStorey", "Level 2", "B", "L2"),
        new ElementRecord("W1", "IfcWall", "Wall A", "L1", "L1"),
        new ElementRecord("D2", "IfcDoor", "Door B", "L2", "L2"),
    ];

    private static readonly PropertyRecord[] Properties =
    [
        new PropertyRecord("W1", "Pset_WallCommon", "IsExternal", "true", null),
        new PropertyRecord("W1", "Qto_WallBaseQuantities", "Length", "4.57", "m"),
        new PropertyRecord("D2", "Pset_DoorCommon", "FireRating", "REI 30", null),
    ];

    [Test]
    public void SelectingAnElementMarksItsStoreyAndTellsTheScreen()
    {
        TreeNodeViewModel? told = null;
        var tree = new ModelTreeViewModel(Elements, new ElementSearch(Elements, Properties), node => told = node);
        var wall = Storey(tree, "Level 1").Children.Single().Children.Single();

        tree.SelectedNode = wall;

        Assert.That(Storey(tree, "Level 1").IsActiveStorey, Is.True);
        Assert.That(Storey(tree, "Level 2").IsActiveStorey, Is.False);
        Assert.That(told, Is.SameAs(wall));
    }

    [Test]
    public void APropertyConditionNarrowsTheTreeAndCountsTheMatches()
    {
        var tree = Tree();

        tree.Filter = "Length > 4";

        var labels = Flatten(tree.Nodes).Select(n => n.Label).ToList();
        Assert.That(labels, Does.Contain("Wall A"));
        Assert.That(labels, Does.Not.Contain("Door B"));
        Assert.That(tree.FilterStatus, Is.TypeOf<FilterMatchedViewModel>());
        Assert.That(tree.FilterStatus!.Text, Is.EqualTo("1 element matches"));
    }

    [Test]
    public void TextAndAPropertyAreCombined()
    {
        var tree = Tree();

        tree.Filter = "door; FireRating = REI 30";

        var labels = Flatten(tree.Nodes).Select(n => n.Label).ToList();
        Assert.That(labels, Does.Contain("Door B"));
        Assert.That(labels, Does.Not.Contain("Wall A"));
    }

    [Test]
    public void NothingMatchingIsSaidPlainly()
    {
        var tree = Tree();

        tree.Filter = "nothing like this";

        Assert.That(tree.Nodes, Is.Empty);
        Assert.That(tree.FilterStatus!.Text, Is.EqualTo("No element matches"));
    }

    [Test]
    public void AnUnfinishedConditionKeepsTheTreeAndSaysWhatIsMissing()
    {
        var tree = Tree();
        tree.Filter = "Wall";
        var before = tree.Nodes;

        tree.Filter = "Wall; Length >";

        Assert.That(tree.Nodes, Is.SameAs(before));
        Assert.That(tree.FilterStatus, Is.TypeOf<FilterRejectedViewModel>());
        Assert.That(tree.FilterStatus!.Text, Does.Contain("number"));
    }

    [Test]
    public void ClearingTheFilterRemovesItsStatus()
    {
        var tree = Tree();
        tree.Filter = "Length > 4";

        tree.ClearFilterCommand.Execute(null);

        Assert.That(tree.FilterStatus, Is.Null);
        Assert.That(tree.Nodes, Is.Not.Empty);
    }

    [Test]
    public void ClearingTheFilterRestoresTheExpansionTheUserChose()
    {
        var tree = Tree();
        Storey(tree, "Level 2").IsExpanded = true;

        tree.Filter = "Wall";
        tree.ClearFilterCommand.Execute(null);

        Assert.That(Storey(tree, "Level 2").IsExpanded, Is.True);
        Assert.That(Storey(tree, "Level 1").IsExpanded, Is.False);
    }

    [Test]
    public void SelectingAnElementByIdOpensTheRowsAboveIt()
    {
        var tree = Tree();

        tree.SelectElement("D2");

        Assert.That(tree.SelectedNode?.Element?.GlobalId, Is.EqualTo("D2"));
        Assert.That(Storey(tree, "Level 2").IsExpanded, Is.True);
        Assert.That(Storey(tree, "Level 2").Children.Single().IsExpanded, Is.True);
        Assert.That(Storey(tree, "Level 1").IsExpanded, Is.False);
    }

    [Test]
    public void AnElementInTheSearchResultKeepsTheSearch()
    {
        var tree = Tree();
        tree.Filter = "Wall";

        tree.SelectElement("W1");

        Assert.That(tree.Filter, Is.EqualTo("Wall"));
        Assert.That(tree.SelectedNode?.Element?.GlobalId, Is.EqualTo("W1"));
    }

    [Test]
    public void AnElementOutsideTheSearchResultClearsTheSearchToShowIt()
    {
        var tree = Tree();
        tree.Filter = "Wall";

        tree.SelectElement("D2");

        Assert.That(tree.Filter, Is.Null);
        Assert.That(tree.SelectedNode?.Element?.GlobalId, Is.EqualTo("D2"));
    }

    [TestCase(null)]
    [TestCase("not-in-the-tree")]
    public void NothingOrAnElementNotInTheTreeClearsTheSelection(string? globalId)
    {
        var tree = Tree();
        tree.SelectElement("W1");

        tree.SelectElement(globalId);

        Assert.That(tree.SelectedNode, Is.Null);
    }

    private static ModelTreeViewModel Tree() => new(Elements, new ElementSearch(Elements, Properties), _ => { });

    private static IEnumerable<TreeNodeViewModel> Flatten(IEnumerable<TreeNodeViewModel> nodes) =>
        nodes.SelectMany(node => Flatten(node.Children).Prepend(node));

    private static TreeNodeViewModel Storey(ModelTreeViewModel tree, string name) =>
        tree.Nodes.Single().Children.Single().Children.Single(n => n.Title == name);
}
