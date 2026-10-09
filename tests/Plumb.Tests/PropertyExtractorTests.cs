using Plumb.Core.Model;
using Plumb.Ifc;
using Xbim.Ifc4;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.PropertyResource;
using Xbim.Ifc4.SharedBldgElements;
using Xbim.IO.Memory;

namespace Plumb.Tests;

[TestFixture]
public sealed class PropertyExtractorTests
{
    private const string WallId = "2O2Fr$t4X7Zf8NOew3FKau";

    private List<PropertyRecord> _records = null!;

    [OneTimeSetUp]
    public void ExtractFromWallWithInstanceAndTypeProperties()
    {
        using var model = new MemoryModel(new EntityFactoryIfc4());
        using var transaction = model.BeginTransaction("setup");

        var project = model.Instances.New<IfcProject>(p => p.GlobalId = "0YvctVUKr0kugbFTf53O9L");
        var wall = model.Instances.New<IfcWall>(w => w.GlobalId = WallId);
        var instanceSet = PropertySet(model, "1YvctVUKr0kugbFTf53O9L", "Pset_WallCommon", ("FireRating", "REI 90"));
        var typeSet = PropertySet(model, "2YvctVUKr0kugbFTf53O9L", "Pset_WallCommon", ("FireRating", "REI 30"), ("AcousticRating", "45 dB"));
        var wallType = model.Instances.New<IfcWallType>(t =>
        {
            t.GlobalId = "3YvctVUKr0kugbFTf53O9L";
            t.HasPropertySets.Add(typeSet);
        });
        model.Instances.New<IfcRelDefinesByProperties>(r =>
        {
            r.GlobalId = "4YvctVUKr0kugbFTf53O9L";
            r.RelatedObjects.Add(wall);
            r.RelatingPropertyDefinition = instanceSet;
        });
        model.Instances.New<IfcRelDefinesByType>(r =>
        {
            r.GlobalId = "5YvctVUKr0kugbFTf53O9L";
            r.RelatedObjects.Add(wall);
            r.RelatingType = wallType;
        });
        transaction.Commit();

        _records = new PropertyExtractor(ProjectUnits.From(project)).Extract(wall).ToList();
    }

    [Test]
    public void InstanceValueWinsOverTypeValue()
    {
        var fireRating = _records.Single(r => r.Name == "FireRating");

        Assert.That(fireRating.Value, Is.EqualTo("REI 90"));
    }

    [Test]
    public void TypeOnlyPropertiesAreInherited()
    {
        var acoustic = _records.Single(r => r.Name == "AcousticRating");

        Assert.That(acoustic, Is.EqualTo(new PropertyRecord(WallId, "Pset_WallCommon", "AcousticRating", "45 dB", null)));
    }

    private static IfcPropertySet PropertySet(MemoryModel model, string globalId, string name, params (string Name, string Value)[] properties) =>
        model.Instances.New<IfcPropertySet>(set =>
        {
            set.GlobalId = globalId;
            set.Name = name;
            foreach (var (propertyName, value) in properties)
            {
                set.HasProperties.Add(model.Instances.New<IfcPropertySingleValue>(p =>
                {
                    p.Name = propertyName;
                    p.NominalValue = new IfcLabel(value);
                }));
            }
        });
}
