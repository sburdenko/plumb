using Plumb.Core.Model;
using Plumb.Core.Tree;

namespace Plumb.Tests;

[TestFixture]
public sealed class ModelTreeBuilderTests
{
    private static readonly ElementRecord[] Records =
    [
        new("P", "IfcProject", "Project", null, null),
        new("S", "IfcSite", "Site", "P", null),
        new("B", "IfcBuilding", "Building", "S", null),
        new("L1", "IfcBuildingStorey", "Level 1", "B", "L1"),
        new("W1", "IfcWall", "Wall A", "L1", "L1"),
        new("D1", "IfcDoor", "Door A", "L1", "L1"),
        new("W2", "IfcWall", "Wall B", "L1", "L1"),
        new("ST", "IfcStair", "Stair", "L1", "L1"),
        new("SF", "IfcStairFlight", "Flight", "ST", "L1"),
        new("X", "IfcWall", "Orphan", "missing-parent", null),
    ];

    [Test]
    public void ElementsWithoutKnownParentBecomeRoots()
    {
        var roots = ModelTreeBuilder.Build(Records);

        Assert.That(roots.Select(r => r.Label), Is.EqualTo(new[] { "Project", "Orphan" }));
    }

    [Test]
    public void SpatialLevelsAreDirectChildren()
    {
        var storey = Storey(ModelTreeBuilder.Build(Records));

        Assert.That(storey.Element.GlobalId, Is.EqualTo("L1"));
    }

    [Test]
    public void ContainedElementsAreGroupedByTypeWithCounts()
    {
        var storey = Storey(ModelTreeBuilder.Build(Records));

        Assert.That(storey.Children.Select(c => c.Label), Is.EqualTo(new[] { "IfcDoor (1)", "IfcStair (1)", "IfcWall (2)" }));
    }

    [Test]
    public void AggregatedPartsNestUnderTheirWhole()
    {
        var storey = Storey(ModelTreeBuilder.Build(Records));
        var stair = storey.Children.OfType<TypeGroupNode>().Single(g => g.IfcType == "IfcStair").Elements.Single();

        Assert.That(stair.Children.Single().Label, Is.EqualTo("IfcStairFlight (1)"));
    }

    [Test]
    public void UnnamedElementIsLabelledByType()
    {
        var node = new ElementNode(new ElementRecord("G", "IfcBuilding", null, null, null), []);

        Assert.That(node.Label, Is.EqualTo("IfcBuilding"));
    }

    private static ElementNode Storey(IReadOnlyList<ModelTreeNode> roots)
    {
        var project = (ElementNode)roots[0];
        var site = (ElementNode)project.Children.Single();
        var building = (ElementNode)site.Children.Single();
        return (ElementNode)building.Children.Single();
    }
}
