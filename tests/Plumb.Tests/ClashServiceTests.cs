using System.Numerics;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using Plumb.Core.Clashes;
using Plumb.Core.Model;
using Plumb.Core.Search;
using Plumb.Import;

namespace Plumb.Tests;

[TestFixture]
public sealed class ClashServiceTests
{
    private static readonly ElementRecord[] Elements =
    [
        new("slab", "IfcSlab", "Floor", null, null),
        new("wall", "IfcWall", "Wall", null, null),
        new("duct", "IfcDuctSegment", "Duct", null, null),
    ];

    private readonly List<ClashStep> _steps = [];
    private ClashService _service = null!;
    private TempDirectory _package = null!;

    [SetUp]
    public void WritePackage()
    {
        _steps.Clear();
        _service = new ClashService(NullLogger<ClashService>.Instance);
        _package = new TempDirectory();
        var glb = new GlbBuilder();
        AddBox(glb, "slab", new Vector3(0, 0, 0), new Vector3(4, 4, 0.2f));
        AddBox(glb, "wall", new Vector3(0, 0, 0.15f), new Vector3(4, 0.2f, 3));
        AddBox(glb, "duct", new Vector3(2, -1, 1), new Vector3(2.3f, 1, 1.3f));
        glb.Write(Path.Combine(_package.Path, "model.glb"));
    }

    [TearDown]
    public void DeletePackage() => _package.Dispose();

    [Test]
    public async Task FindsClashesBetweenTheTwoGroupsOnly()
    {
        var result = await FindAsync("IfcWall", "IfcSlab");

        var found = (ClashResult.Found)result;
        Assert.That(found.Clashes.Select(c => (c.FirstId, c.SecondId)), Is.EqualTo(new[] { ("wall", "slab") }));
        Assert.That(_steps, Is.EqualTo(new[] { ClashStep.ReadingGeometry, ClashStep.Comparing }));
    }

    [Test]
    public async Task ASecondSearchOnTheSamePackageDoesNotReadTheGeometryAgain()
    {
        await FindAsync("IfcWall", "IfcSlab");
        _steps.Clear();

        var result = await FindAsync("IfcDuctSegment", "IfcWall");

        Assert.That(((ClashResult.Found)result).Clashes.Single().FirstId, Is.EqualTo("duct"));
        Assert.That(_steps, Is.EqualTo(new[] { ClashStep.Comparing }));
    }

    [Test]
    public async Task APackageWithoutGeometryCannotBeSearched()
    {
        File.Delete(Path.Combine(_package.Path, "model.glb"));

        var result = await FindAsync("IfcWall", "IfcSlab");

        Assert.That(((ClashResult.Failed)result).Message, Does.Contain("3D geometry"));
    }

    [Test]
    public async Task UnreadableGeometryIsReported()
    {
        File.WriteAllText(Path.Combine(_package.Path, "model.glb"), "not a glb");

        var result = await FindAsync("IfcWall", "IfcSlab");

        Assert.That(((ClashResult.Failed)result).Message, Does.Contain("model.glb"));
    }

    [Test]
    public async Task ACancelledSearchSaysSo()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        var result = await FindAsync("IfcWall", "IfcSlab", cancelled.Token);

        Assert.That(result, Is.TypeOf<ClashResult.Cancelled>());
    }

    private Task<ClashResult> FindAsync(string first, string second, CancellationToken cancellationToken = default)
    {
        var search = new ElementSearch(Elements, []);
        return _service.FindAsync(
            _package.Path,
            search.Find(((ParseResult.Parsed)QueryParser.Parse(first)).Query),
            search.Find(((ParseResult.Parsed)QueryParser.Parse(second)).Query),
            new SyncProgress<ClashStep>(_steps.Add),
            cancellationToken);
    }

    private static void AddBox(GlbBuilder glb, string name, Vector3 min, Vector3 max)
    {
        var positions = Cuboid.Triangles(min, max).SelectMany(t => new[] { t.A, t.B, t.C }).SelectMany(v => new[] { v.X, v.Y, v.Z }).ToArray();
        glb.AddNode(new JsonObject { ["name"] = name, ["mesh"] = glb.AddMesh(positions) });
    }
}
