using System.Diagnostics;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging.Abstractions;
using Plumb.Core.Geometry;
using Plumb.Geometry;
using Xbim.Ifc4.Interfaces;
using Xbim.IO.Memory;

namespace Plumb.Tests;

[TestFixture]
public sealed class IfcConvertRunnerTests
{
    private static readonly TimeSpan GenerousTimeout = TimeSpan.FromMinutes(2);

    private TempDirectory _temp = null!;
    private string _glb = null!;

    [SetUp]
    public void CreateTemp()
    {
        _temp = new TempDirectory();
        _glb = Path.Combine(_temp.Path, "model.glb");
    }

    [TearDown]
    public void DeleteTemp() => _temp.Dispose();

    [Test]
    public async Task ConvertsDuplexWithNodesNamedByGlobalId()
    {
        var state = await Runner(GenerousTimeout).ConvertAsync(Samples.Duplex, _glb, CancellationToken.None);

        Assert.That(state, Is.TypeOf<GeometryState.Built>());
        using var model = MemoryModel.OpenRead(Samples.Duplex);
        var productIds = model.Instances.OfType<IIfcProduct>().Select(p => p.GlobalId.ToString()).ToHashSet();
        var names = GlbFile.NodeNames(_glb);
        Assert.That(names, Is.Not.Empty);
        Assert.That(names, Is.SubsetOf(productIds));
    }

    [Test]
    public async Task MissingProgramIsConverterMissing()
    {
        var runner = new IfcConvertRunner(Path.Combine(_temp.Path, "nope", "IfcConvert"), GenerousTimeout, NullLogger<IfcConvertRunner>.Instance);

        var state = await runner.ConvertAsync(Samples.Duplex, _glb, CancellationToken.None);

        Assert.That(((GeometryState.NotBuilt)state).Error, Is.EqualTo(GeometryError.ConverterMissing));
    }

    [Test]
    public async Task UnreadableIfcIsConverterFailedWithTheReason()
    {
        var junk = _temp.WriteFile("junk.ifc", "not an ifc file");

        var state = await Runner(GenerousTimeout).ConvertAsync(junk, _glb, CancellationToken.None);

        var notBuilt = (GeometryState.NotBuilt)state;
        Assert.That(notBuilt.Error, Is.EqualTo(GeometryError.ConverterFailed));
        Assert.That(notBuilt.Reason, Does.Contain("Unable to parse"));
        Assert.That(File.Exists(_glb), Is.False);
    }

    [Test]
    public async Task SlowConversionTimesOutAndLeavesNoFile()
    {
        var state = await Runner(TimeSpan.FromMilliseconds(50)).ConvertAsync(Samples.Duplex, _glb, CancellationToken.None);

        Assert.That(((GeometryState.NotBuilt)state).Error, Is.EqualTo(GeometryError.Timeout));
        Assert.That(File.Exists(_glb), Is.False);
        AssertNoConverterRunning();
    }

    [Test]
    public void CancellationStopsTheConverterAndThrows()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        Assert.That(
            async () => await Runner(GenerousTimeout).ConvertAsync(Samples.Duplex, _glb, cts.Token),
            Throws.InstanceOf<OperationCanceledException>());
        Assert.That(File.Exists(_glb), Is.False);
        AssertNoConverterRunning();
    }

    [Test]
    [Platform(Exclude = "Win", Reason = "Uses a shell script in place of IfcConvert")]
    [UnsupportedOSPlatform("windows")]
    public async Task TimeoutReturnsEvenWhenAnOrphanKeepsTheOutputOpen()
    {
        var script = ConverterScripts.LeavesOrphanHoldingOutput(_temp.Path);
        var runner = new IfcConvertRunner(script, TimeSpan.FromMilliseconds(200), NullLogger<IfcConvertRunner>.Instance);
        try
        {
            var state = await runner.ConvertAsync(Samples.Duplex, _glb, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10));

            Assert.That(((GeometryState.NotBuilt)state).Error, Is.EqualTo(GeometryError.Timeout));
        }
        finally
        {
            ConverterScripts.KillOrphans();
        }
    }

    [Test]
    [Platform(Exclude = "Win", Reason = "Uses a shell script in place of IfcConvert")]
    [UnsupportedOSPlatform("windows")]
    public async Task ErrorDetailIsTheLastErrorLineWithoutItsTags()
    {
        var script = ConverterScripts.FailsWith(
            _temp.Path,
            stdout: "[notice] [N1] converting",
            stderr: "[error] [E2] [2026-10-09 13:24:31] Cannot read [section] of a.ifc");
        var runner = new IfcConvertRunner(script, GenerousTimeout, NullLogger<IfcConvertRunner>.Instance);

        var state = await runner.ConvertAsync(Samples.Duplex, _glb, CancellationToken.None);

        var notBuilt = (GeometryState.NotBuilt)state;
        Assert.That(notBuilt.Error, Is.EqualTo(GeometryError.ConverterFailed));
        Assert.That(notBuilt.Reason, Does.EndWith("Cannot read [section] of a.ifc"));
    }

    private static IfcConvertRunner Runner(TimeSpan timeout) =>
        new(Converters.IfcConvert, timeout, NullLogger<IfcConvertRunner>.Instance);

    private static void AssertNoConverterRunning()
    {
        var running = Process.GetProcessesByName("IfcConvert");
        try
        {
            Assert.That(running, Is.Empty, "IfcConvert must be stopped, not left running");
        }
        finally
        {
            foreach (var process in running)
            {
                process.Dispose();
            }
        }
    }
}
