using Plumb.Core.Model;
using Plumb.Core.Search;

namespace Plumb.Tests;

[TestFixture]
public sealed class ElementSearchTests
{
    private static readonly ElementRecord[] Elements =
    [
        new("P", "IfcProject", "Project", null, null),
        new("L1", "IfcBuildingStorey", "Level 1", "P", "L1"),
        new("W1", "IfcWall", "Exterior wall", "L1", "L1"),
        new("W2", "IfcWall", "Interior wall", "L1", "L1", Tag: "139117"),
        new("W3", "IfcWallStandardCase", "Party wall", "L1", "L1"),
        new("D1", "IfcDoor", "Exterior door", "L1", "L1"),
    ];

    private static readonly PropertyRecord[] Properties =
    [
        new("W1", "Pset_WallCommon", "FireRating", "REI 90", null),
        new("W2", "Pset_WallCommon", "FireRating", "REI 30", null),
        new("W3", "Pset_WallCommon", "FireRating", "rei 90", null, PropertySource.Type),
        new("W1", "Qto_WallBaseQuantities", "Length", "4.57", "m"),
        new("W2", "Qto_WallBaseQuantities", "Length", "12.5", "m"),
        new("W3", "Qto_WallBaseQuantities", "Length", "not measured", null),
        new("D1", "Pset_DoorCommon", "Size", "900x2100", null),
        new("D1", "Pset_DoorCommon", "Length", "2.1", "m"),
    ];

    private readonly ElementSearch _search = new(Elements, Properties);

    [Test]
    public void AnEmptyQueryMatchesEveryElement()
    {
        Assert.That(Ids(), Is.EquivalentTo(Elements.Select(e => e.GlobalId)));
    }

    [TestCase("exterior", new[] { "W1", "D1" })]
    [TestCase("EXTERIOR", new[] { "W1", "D1" })]
    [TestCase("ifcwallstandardcase", new[] { "W3" })]
    [TestCase("d1", new[] { "D1" })]
    [TestCase("#139117", new[] { "W2" })]
    [TestCase("139117", new[] { "W2" })]
    [TestCase("zzz", new string[0])]
    public void TextMatchesNameClassGlobalIdAndTag(string term, string[] expected)
    {
        Assert.That(Ids(new Condition.Text(term)), Is.EquivalentTo(expected));
    }

    [Test]
    public void EqualsIgnoresCaseAndTheSourceOfTheValue()
    {
        Assert.That(Ids(new Condition.PropertyEquals("FireRating", "rei 90")), Is.EquivalentTo(new[] { "W1", "W3" }));
    }

    [Test]
    public void ThePropertyNameIgnoresCase()
    {
        Assert.That(Ids(new Condition.PropertyEquals("firerating", "REI 30")), Is.EquivalentTo(new[] { "W2" }));
    }

    [Test]
    public void EqualsComparesNumbersAsNumbers()
    {
        Assert.That(Ids(new Condition.PropertyEquals("Length", "4.570")), Is.EquivalentTo(new[] { "W1" }));
    }

    [Test]
    public void ContainsLooksInsideTheValue()
    {
        Assert.That(Ids(new Condition.PropertyContains("Size", "x21")), Is.EquivalentTo(new[] { "D1" }));
    }

    [Test]
    public void ANumberConditionSkipsValuesThatAreNotNumbers()
    {
        Assert.That(Ids(new Condition.PropertyNumber("Length", NumberComparison.Greater, 4)), Is.EquivalentTo(new[] { "W1", "W2" }));
    }

    [TestCase(NumberComparison.Less, 4.57, new string[] { "D1" })]
    [TestCase(NumberComparison.LessOrEqual, 4.57, new[] { "W1", "D1" })]
    [TestCase(NumberComparison.Greater, 4.57, new[] { "W2" })]
    [TestCase(NumberComparison.GreaterOrEqual, 4.57, new[] { "W1", "W2" })]
    public void NumbersAreComparedByTheOperator(NumberComparison comparison, double number, string[] expected)
    {
        Assert.That(Ids(new Condition.PropertyNumber("Length", comparison, number)), Is.EquivalentTo(expected));
    }

    [Test]
    public void EveryConditionMustHold()
    {
        var ids = Ids(new Condition.Text("wall"), new Condition.PropertyEquals("FireRating", "REI 90"), new Condition.PropertyNumber("Length", NumberComparison.Less, 10));

        Assert.That(ids, Is.EquivalentTo(new[] { "W1" }));
    }

    [Test]
    public void APropertyNoElementHasMatchesNothing()
    {
        Assert.That(Ids(new Condition.PropertyEquals("NoSuchProperty", "x")), Is.Empty);
    }

    private IEnumerable<string> Ids(params Condition[] conditions) =>
        _search.Find(new ElementQuery(conditions));
}
