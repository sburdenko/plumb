using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Plumb.Core.Geometry;
using Plumb.Core.Import;
using Plumb.Core.Package;
using Plumb.Geometry;
using Plumb.Import;

namespace Plumb.Tests;

/// <summary>
/// The import pipeline with the real IfcConvert.
/// </summary>
[TestFixture]
public sealed class GeometryPipelineTests
{
    private readonly IProgress<ImportProgress> _noProgress = new SyncProgress<ImportProgress>(_ => { });

    private TempDirectory _temp = null!;
    private string _ifc = null!;
    private string _package = null!;

    [SetUp]
    public void CopySample()
    {
        _temp = new TempDirectory();
        _ifc = Path.Combine(_temp.Path, "Duplex.ifc");
        File.Copy(Samples.Duplex, _ifc);
        _package = Path.Combine(_temp.Path, "Duplex.plumb");
    }

    [TearDown]
    public void DeleteTemp() => _temp.Dispose();

    [Test]
    public async Task EveryGeometryNodeIsAnElementOfThePackage()
    {
        var service = Service(Converters.IfcConvert);

        var imported = (ImportResult.Success)await service.RunAsync(_ifc, _temp.Path, _noProgress, CancellationToken.None);

        Assert.That(imported.Package, Is.EqualTo(new PackageState.Saved(_package, new GeometryState.Built())));
        var opened = (ImportResult.Success)await service.OpenPackageAsync(_package, _noProgress, CancellationToken.None);
        var elementIds = opened.Model.Elements.Select(e => e.GlobalId).ToHashSet();
        var nodeNames = GlbFile.NodeNames(Path.Combine(_package, PackageLayout.GeometryFile));
        Assert.That(nodeNames, Is.Not.Empty);
        Assert.That(nodeNames, Is.SubsetOf(elementIds));
    }

    [Test]
    public async Task CancelDuringConversionLeavesNoTempFiles()
    {
        using var cts = new CancellationTokenSource();
        var progress = new SyncProgress<ImportProgress>(p =>
        {
            if (p.Step == ImportStep.ConvertingGeometry)
            {
                cts.CancelAfter(TimeSpan.FromMilliseconds(200));
            }
        });

        var result = await Service(Converters.IfcConvert).RunAsync(_ifc, _temp.Path, progress, cts.Token);

        Assert.That(((ImportResult.Failure)result).Error, Is.EqualTo(ImportError.Cancelled));
        Assert.That(Directory.GetFileSystemEntries(_temp.Path).Select(Path.GetFileName), Is.EqualTo(new[] { "Duplex.ifc" }));
        var running = Process.GetProcessesByName("IfcConvert");
        Assert.That(running, Is.Empty);
        Array.ForEach(running, p => p.Dispose());
    }

    [Test]
    public async Task MissingConverterStillLoadsTheModel()
    {
        var service = Service(Path.Combine(_temp.Path, "missing", "IfcConvert"));

        var result = await service.RunAsync(_ifc, _temp.Path, _noProgress, CancellationToken.None);

        var success = (ImportResult.Success)result;
        Assert.That(success.Model.Elements, Is.Not.Empty);
        var saved = (PackageState.Saved)success.Package;
        Assert.That(((GeometryState.NotBuilt)saved.Geometry).Error, Is.EqualTo(GeometryError.ConverterMissing));
    }

    private static ImportService Service(string converterPath) =>
        new(
            NullLogger<ImportService>.Instance,
            new IfcConvertRunner(converterPath, TimeSpan.FromMinutes(2), NullLogger<IfcConvertRunner>.Instance));
}
