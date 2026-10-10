using Plumb.Ifc;
using Xbim.Ifc4;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.MeasureResource;
using Xbim.IO.Memory;

namespace Plumb.Tests;

[TestFixture]
public sealed class ProjectUnitsTests
{
    [Test]
    public void MetresNeedNoConversion()
    {
        Assert.That(LengthToMetres(model => SiLength(model, prefix: null)), Is.EqualTo(1));
    }

    [Test]
    public void MillimetresAreScaledByTheirPrefix()
    {
        Assert.That(LengthToMetres(model => SiLength(model, IfcSIPrefix.MILLI)), Is.EqualTo(0.001).Within(1e-12));
    }

    [Test]
    public void FeetAreScaledByTheirConversionFactor()
    {
        Assert.That(LengthToMetres(Feet), Is.EqualTo(0.3048).Within(1e-12));
    }

    [Test]
    public void AProjectWithoutALengthUnitIsTakenToBeInMetres()
    {
        Assert.That(LengthToMetres(model => null), Is.EqualTo(1));
    }

    private static double LengthToMetres(Func<MemoryModel, IfcNamedUnit?> lengthUnit)
    {
        using var model = new MemoryModel(new EntityFactoryIfc4());
        using var transaction = model.BeginTransaction("units");
        var unit = lengthUnit(model);
        var project = model.Instances.New<IfcProject>(p =>
        {
            p.GlobalId = "0YvctVUKr0kugbFTf53O9L";
            p.UnitsInContext = model.Instances.New<IfcUnitAssignment>(a =>
            {
                if (unit != null)
                {
                    a.Units.Add(unit);
                }
            });
        });
        transaction.Commit();

        return ProjectUnits.From(project).LengthToMetres;
    }

    private static IfcSIUnit SiLength(MemoryModel model, IfcSIPrefix? prefix) =>
        model.Instances.New<IfcSIUnit>(u =>
        {
            u.UnitType = IfcUnitEnum.LENGTHUNIT;
            u.Name = IfcSIUnitName.METRE;
            u.Prefix = prefix;
        });

    private static IfcNamedUnit Feet(MemoryModel model) =>
        model.Instances.New<IfcConversionBasedUnit>(u =>
        {
            u.UnitType = IfcUnitEnum.LENGTHUNIT;
            u.Name = "FOOT";
            u.Dimensions = model.Instances.New<IfcDimensionalExponents>(d => d.LengthExponent = 1);
            u.ConversionFactor = model.Instances.New<IfcMeasureWithUnit>(m =>
            {
                m.ValueComponent = new IfcLengthMeasure(0.3048);
                m.UnitComponent = SiLength(model, prefix: null);
            });
        });
}
