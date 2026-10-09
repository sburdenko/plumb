using Plumb.Core.Model;
using Plumb.Core.Tree;

namespace Plumb.Tests;

[TestFixture]
public sealed class ModelTreeFilterTests
{
    private static readonly ElementRecord[] Records =
    [
        new("P", "IfcProject", "Project", null, null),
        new("L1", "IfcBuildingStorey", "Level 1", "P", "L1"),
        new("L2", "IfcBuildingStorey", "Level 2", "P", "L2"),
        new("W1", "IfcWall", "Exterior wall", "L1", "L1"),
        new("W2", "IfcWall", "Interior wall", "L1", "L1"),
        new("D1", "IfcDoor", "Exterior door", "L2", "L2"),
    ];

    private readonly IReadOnlyList<ModelTreeNode> _tree = ModelTreeBuilder.Build(Records);

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void BlankQueryReturnsTreeUnchanged(string? query)
    {
        Assert.That(ModelTreeFilter.Apply(_tree, query), Is.SameAs(_tree));
    }

    [Test]
    public void NameMatchKeepsAncestorsAndUpdatesGroupCounts()
    {
        var result = ModelTreeFilter.Apply(_tree, "interior");

        var project = (ElementNode)result.Single();
        var level1 = (ElementNode)project.Children.Single();
        Assert.That(level1.Label, Is.EqualTo("Level 1"));
        Assert.That(level1.Children.Single().Label, Is.EqualTo("IfcWall (1)"));
        Assert.That(level1.Children.Single().Children.Single().Label, Is.EqualTo("Interior wall"));
    }

    [Test]
    public void MatchIsCaseInsensitiveAcrossBranches()
    {
        var result = ModelTreeFilter.Apply(_tree, "EXTERIOR");

        var project = (ElementNode)result.Single();
        Assert.That(project.Children.Select(c => c.Label), Is.EqualTo(new[] { "Level 1", "Level 2" }));
    }

    [Test]
    public void TypeMatchKeepsTheWholeGroup()
    {
        var result = ModelTreeFilter.Apply(_tree, "ifcwall");

        var level1 = (ElementNode)((ElementNode)result.Single()).Children.Single();
        Assert.That(level1.Children.Single().Label, Is.EqualTo("IfcWall (2)"));
    }

    [Test]
    public void NoMatchReturnsEmpty()
    {
        Assert.That(ModelTreeFilter.Apply(_tree, "zzz"), Is.Empty);
    }

    [Test]
    public void FilteringDoesNotMutateTheSourceTree()
    {
        ModelTreeFilter.Apply(_tree, "interior");

        var level1 = (ElementNode)((ElementNode)_tree[0]).Children[0];
        Assert.That(level1.Children.Single().Label, Is.EqualTo("IfcWall (2)"));
    }
}
