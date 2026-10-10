using Plumb.App.Screens.Model.Properties;
using Plumb.Core.Model;

namespace Plumb.Tests;

[TestFixture]
public sealed class ElementDetailsTests
{
    private static readonly ElementRecord Wall = new("W1", "IfcWall", "Basic Wall:Exterior", "L1", "L1", Tag: "139117");

    [Test]
    public void TheTagIsShownAfterTheGlobalId()
    {
        Assert.That((Details(Wall).GlobalId, Details(Wall).TagSuffix), Is.EqualTo(("W1", " · #139117")));
        Assert.That(Details(Wall with { Tag = null }).TagSuffix, Is.Empty);
    }

    [Test]
    public void InstanceGroupsComeFirstAndEveryGroupSaysWhereItsValuesComeFrom()
    {
        var details = Details(
            Wall,
            new PropertyRecord("W1", "Pset_WallCommon", "AcousticRating", "45 dB", null, PropertySource.Type),
            new PropertyRecord("W1", "Pset_WallCommon", "FireRating", "REI 90", null),
            new PropertyRecord("W1", "Qto_WallBaseQuantities", "Length", "4.57", "m"));

        Assert.That(details.Groups.Select(g => (g.Name, g.Source)), Is.EqualTo(new[]
        {
            ("Pset_WallCommon", "INSTANCE"),
            ("Qto_WallBaseQuantities", "INSTANCE"),
            ("Pset_WallCommon", "TYPE"),
        }));
        Assert.That(details.Groups[2].Rows.Single().Key, Is.EqualTo("AcousticRating"));
    }

    private static ElementDetailsViewModel Details(ElementRecord element, params PropertyRecord[] properties) =>
        new(element, "Level 1", properties);
}
