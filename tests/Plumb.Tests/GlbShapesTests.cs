using System.Numerics;
using System.Text.Json.Nodes;
using Plumb.Core.Clashes;
using Plumb.Package;

namespace Plumb.Tests;

[TestFixture]
public sealed class GlbShapesTests
{
    private static readonly float[] UnitTriangle = [0, 0, 0, 1, 0, 0, 0, 1, 0];

    private TempDirectory _temp = null!;

    [SetUp]
    public void CreateFolder() => _temp = new TempDirectory();

    [TearDown]
    public void DeleteFolder() => _temp.Dispose();

    [Test]
    public void ANamedNodeIsAnElementPlacedWhereItStands()
    {
        var glb = new GlbBuilder();
        var mesh = glb.AddMesh(UnitTriangle);
        glb.AddNode(new JsonObject { ["name"] = "wall", ["mesh"] = mesh, ["translation"] = new JsonArray(10f, 0f, 0f) });

        var shape = Read(glb).Single();

        Assert.That(shape.GlobalId, Is.EqualTo("wall"));
        Assert.That(shape.Triangles.Single(), Is.EqualTo(new Triangle(new Vector3(10, 0, 0), new Vector3(11, 0, 0), new Vector3(10, 1, 0))));
    }

    [Test]
    public void AParentsMatrixAppliesToItsChildren()
    {
        var glb = new GlbBuilder();
        var mesh = glb.AddMesh(UnitTriangle);
        var child = glb.AddNode(new JsonObject { ["name"] = "duct", ["mesh"] = mesh, ["scale"] = new JsonArray(2f, 2f, 2f) }, inScene: false);
        glb.AddNode(new JsonObject
        {
            ["matrix"] = new JsonArray(1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 5f, 1f),
            ["children"] = new JsonArray(child),
        });

        var triangle = Read(glb).Single().Triangles.Single();

        Assert.That(triangle, Is.EqualTo(new Triangle(new Vector3(0, 0, 5), new Vector3(2, 0, 5), new Vector3(0, 2, 5))));
    }

    [Test]
    public void AnUnnamedMeshBelongsToTheNamedNodeAboveIt()
    {
        var glb = new GlbBuilder();
        var mesh = glb.AddMesh(UnitTriangle);
        var part = glb.AddNode(new JsonObject { ["mesh"] = mesh, ["translation"] = new JsonArray(0f, 0f, 1f) }, inScene: false);
        glb.AddNode(new JsonObject { ["name"] = "stair", ["mesh"] = mesh, ["children"] = new JsonArray(part) });

        var shape = Read(glb).Single();

        Assert.That(shape.GlobalId, Is.EqualTo("stair"));
        Assert.That(shape.Triangles, Has.Count.EqualTo(2));
    }

    [Test]
    public void AMeshUsedByTwoElementsBelongsToBoth()
    {
        var glb = new GlbBuilder();
        var mesh = glb.AddMesh(UnitTriangle);
        glb.AddNode(new JsonObject { ["name"] = "left", ["mesh"] = mesh });
        glb.AddNode(new JsonObject { ["name"] = "right", ["mesh"] = mesh, ["translation"] = new JsonArray(3f, 0f, 0f) });

        var shapes = Read(glb);

        Assert.That(shapes.Select(s => s.GlobalId), Is.EqualTo(new[] { "left", "right" }));
        Assert.That(shapes[1].Bounds.Min.X, Is.EqualTo(3));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void IndicesPickTheCorners(bool wideIndices)
    {
        var glb = new GlbBuilder();
        var mesh = glb.AddMesh([0, 0, 0, 1, 0, 0, 1, 1, 0, 0, 1, 0], [0, 1, 2, 0, 2, 3], wideIndices);
        glb.AddNode(new JsonObject { ["name"] = "slab", ["mesh"] = mesh });

        var triangles = Read(glb).Single().Triangles;

        Assert.That(triangles, Has.Count.EqualTo(2));
        Assert.That(triangles[1].C, Is.EqualTo(new Vector3(0, 1, 0)));
    }

    [Test]
    public void InterleavedPositionsAreReadByTheirStride()
    {
        var glb = new GlbBuilder();
        var mesh = glb.AddMesh(UnitTriangle, positionStride: 24);
        glb.AddNode(new JsonObject { ["name"] = "beam", ["mesh"] = mesh });

        Assert.That(Read(glb).Single().Triangles.Single().B, Is.EqualTo(new Vector3(1, 0, 0)));
    }

    [Test]
    public void LinesAndPointsHaveNoSurface()
    {
        var glb = new GlbBuilder();
        var lines = glb.AddMesh(UnitTriangle, mode: 1);
        glb.AddNode(new JsonObject { ["name"] = "grid", ["mesh"] = lines });

        Assert.That(Read(glb), Is.Empty);
    }

    [Test]
    public void AFileThatIsNotBinaryGltfIsRejected()
    {
        var path = _temp.WriteFile("model.glb", "not a glb");

        Assert.That(() => GlbShapes.Read(path), Throws.InstanceOf<PackageFormatException>());
    }

    [Test]
    public void EveryElementOfTheDuplexPackageHasAShape()
    {
        var glb = Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "samples", "Duplex.plumb", "model.glb");
        Assume.That(File.Exists(glb), "the Duplex package is not in samples/");

        var shapes = GlbShapes.Read(glb);

        Assert.That(shapes.Select(s => s.GlobalId), Is.EquivalentTo(GlbFile.NodeNames(glb).Distinct()));
        Assert.That(shapes.Select(s => s.Triangles.Count), Has.All.Positive);
    }

    private IReadOnlyList<ElementShape> Read(GlbBuilder glb) => GlbShapes.Read(glb.Write(Path.Combine(_temp.Path, "model.glb")));
}
