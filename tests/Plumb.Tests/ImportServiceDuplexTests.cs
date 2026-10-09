using Microsoft.Extensions.Logging.Abstractions;
using Plumb.Core.Import;
using Plumb.Import;
using Plumb.Core.Model;
using Plumb.Core.Tree;
using Xbim.Ifc4.Interfaces;
using Xbim.IO.Memory;

namespace Plumb.Tests;

[TestFixture]
public sealed class ImportServiceDuplexTests
{
    private IfcModelData _model = null!;
    private readonly List<ImportProgress> _progress = [];
    private TempDirectory _temp = null!;

    [OneTimeSetUp]
    public async Task ImportDuplex()
    {
        _temp = new TempDirectory();
        var service = new ImportService(NullLogger<ImportService>.Instance, FakeGeometryConverter.Builds());
        var result = await service.RunAsync(
            Samples.Duplex, _temp.Path, new SyncProgress<ImportProgress>(_progress.Add), CancellationToken.None);

        Assert.That(result, Is.TypeOf<ImportResult.Success>(), () => result.ToString());
        _model = ((ImportResult.Success)result).Model;
    }

    [OneTimeTearDown]
    public void DeletePackage() => _temp.Dispose();

    [Test]
    public void ReportsSourceFileAndSchema()
    {
        Assert.That(_model.SourceFile, Is.EqualTo("Duplex.ifc"));
        Assert.That(_model.IfcSchema, Is.EqualTo("IFC2X3"));
    }

    [Test]
    public void RootIsTheProject()
    {
        Assert.That(_model.Elements[0].IfcType, Is.EqualTo("IfcProject"));
        Assert.That(_model.Elements[0].ParentGlobalId, Is.Null);
    }

    [Test]
    public void ReadsAllStoreysOrderedByElevation()
    {
        var storeys = _model.Elements.Where(e => e.IfcType == "IfcBuildingStorey").Select(e => e.Name);

        Assert.That(storeys, Is.EqualTo(new[] { "T/FDN", "Level 1", "Level 2", "Roof" }));
    }

    [Test]
    public void TreeShowsStoreysWithWallGroups()
    {
        var project = (ElementNode)ModelTreeBuilder.Build(_model.Elements).Single();
        var building = (ElementNode)((ElementNode)project.Children.Single()).Children.Single();
        var level1 = building.Children.OfType<ElementNode>().Single(s => s.Label == "Level 1");

        Assert.That(level1.Children.Select(c => c.Label), Does.Contain("IfcWallStandardCase (21)"));
    }

    [Test]
    public void ElementCountMatchesXbimPlacedProductsPlusProject()
    {
        using var model = MemoryModel.OpenRead(Samples.Duplex);
        var expected = model.Instances.OfType<IIfcProduct>().Count(p => p is not IIfcOpeningElement) + 1;

        Assert.That(_model.Elements, Has.Count.EqualTo(expected));
    }

    [Test]
    public void GlobalIdsAreUnique()
    {
        Assert.That(_model.Elements.Select(e => e.GlobalId), Is.Unique);
    }

    [Test]
    public void ParentsComeBeforeChildren()
    {
        var seen = new HashSet<string>();
        foreach (var element in _model.Elements)
        {
            if (element.ParentGlobalId != null)
            {
                Assert.That(seen, Does.Contain(element.ParentGlobalId), element.GlobalId);
            }

            seen.Add(element.GlobalId);
        }
    }

    [Test]
    public void ContainedElementsPointToTheirStorey()
    {
        var byId = _model.Elements.ToDictionary(e => e.GlobalId);
        var walls = _model.Elements.Where(e => e.IfcType.StartsWith("IfcWall", StringComparison.Ordinal)).ToList();

        Assert.That(walls, Is.Not.Empty);
        Assert.That(walls.Select(w => byId[w.StoreyGlobalId!].IfcType), Has.All.EqualTo("IfcBuildingStorey"));
    }

    [Test]
    public void AggregatedPartsKeepTheStoreyOfTheirWhole()
    {
        var byId = _model.Elements.ToDictionary(e => e.GlobalId);
        var parts = _model.Elements
            .Where(e => e.ParentGlobalId != null && byId[e.ParentGlobalId].IfcType is "IfcStair" or "IfcRoof")
            .ToList();

        Assert.That(parts, Is.Not.Empty);
        foreach (var part in parts)
        {
            Assert.That(part.StoreyGlobalId, Is.Not.Null.And.EqualTo(byId[part.ParentGlobalId!].StoreyGlobalId), part.GlobalId);
        }
    }

    [Test]
    public void WallsHaveCommonPropertySet()
    {
        var wall = _model.Elements.First(e => e.IfcType == "IfcWallStandardCase");
        var isExternal = _model.Properties.Single(p => p.GlobalId == wall.GlobalId && p.Pset == "Pset_WallCommon" && p.Name == "IsExternal");

        Assert.That(isExternal.Value, Is.AnyOf("true", "false"));
    }

    [Test]
    public void LengthPropertiesUseProjectUnitAndInvariantFormat()
    {
        var length = _model.Properties.First(p => p.Pset == "PSet_Revit_Dimensions" && p.Name == "Length");

        Assert.That(length.Unit, Is.EqualTo("m"));
        Assert.That(length.Value, Does.Match(@"^-?\d+(\.\d+)?$"));
    }

    [Test]
    public void EveryPropertyBelongsToAnElement()
    {
        var ids = _model.Elements.Select(e => e.GlobalId).ToHashSet();

        Assert.That(_model.Properties.Select(p => p.GlobalId).Distinct(), Is.SubsetOf(ids));
    }

    [Test]
    public void PropertiesAreUniquePerElementSetAndName()
    {
        Assert.That(_model.Properties.Select(p => (p.GlobalId, p.Pset, p.Name)), Is.Unique);
    }

    [Test]
    public void ProgressIsMonotonicAndEndsAtHundred()
    {
        var percents = _progress.Select(p => p.Percent).ToList();

        Assert.That(percents, Is.Ordered);
        Assert.That(percents[^1], Is.EqualTo(100));
        Assert.That(_progress[0].Step, Is.EqualTo(ImportStep.Validating));
    }
}
