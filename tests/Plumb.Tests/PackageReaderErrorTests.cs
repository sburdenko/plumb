using Plumb.Core.Geometry;
using Plumb.Core.Model;
using Plumb.Core.Package;
using Plumb.Package;

namespace Plumb.Tests;

[TestFixture]
public sealed class PackageReaderErrorTests
{
    private static readonly IfcModelData Model = new(
        "House.ifc", "IFC4", [new ElementRecord("P", "IfcProject", "Project", null, null)], []);

    private static readonly PackageManifest Manifest = new(
        PackageLayout.FormatVersion, "House.ifc", "ab12", "IFC4", DateTime.UtcNow, 1, 10);

    private static readonly IProgress<int> NoProgress = new SyncProgress<int>(_ => { });

    private TempDirectory _temp = null!;
    private string _package = null!;

    [SetUp]
    public void WritePackage()
    {
        _temp = new TempDirectory();
        _package = Path.Combine(_temp.Path, "House.plumb");
        PackageWriter.Write(_package, Model, Manifest, NoProgress, CancellationToken.None);
    }

    [TearDown]
    public void DeletePackage() => _temp.Dispose();

    [Test]
    public void MissingFolderThrowsDirectoryNotFound()
    {
        Assert.That(
            () => PackageReader.Read(Path.Combine(_temp.Path, "Nope.plumb"), CancellationToken.None),
            Throws.InstanceOf<DirectoryNotFoundException>());
    }

    [TestCase(PackageLayout.ManifestFile)]
    [TestCase(PackageLayout.DatabaseFile)]
    public void MissingFileIsAFormatError(string file)
    {
        File.Delete(Path.Combine(_package, file));

        Assert.That(() => PackageReader.Read(_package, CancellationToken.None), Throws.InstanceOf<PackageFormatException>());
    }

    [Test]
    public void BrokenManifestIsAFormatError()
    {
        File.WriteAllText(Path.Combine(_package, PackageLayout.ManifestFile), "{ not json");

        Assert.That(() => PackageReader.Read(_package, CancellationToken.None), Throws.InstanceOf<PackageFormatException>());
    }

    [Test]
    public void NewerFormatVersionIsAFormatError()
    {
        var path = Path.Combine(_package, PackageLayout.ManifestFile);
        File.WriteAllText(path, File.ReadAllText(path).Replace("\"formatVersion\": 1", "\"formatVersion\": 99"));

        Assert.That(
            () => PackageReader.Read(_package, CancellationToken.None),
            Throws.InstanceOf<PackageFormatException>().With.Message.Contains("99"));
    }

    [TestCase("sourceFile")]
    [TestCase("ifcSchema")]
    [TestCase("elementCount")]
    public void ManifestMissingFieldIsAFormatError(string field)
    {
        var path = Path.Combine(_package, PackageLayout.ManifestFile);
        var manifest = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        manifest.Remove(field);
        File.WriteAllText(path, manifest.ToJsonString());

        Assert.That(
            () => PackageReader.Read(_package, CancellationToken.None),
            Throws.InstanceOf<PackageFormatException>().With.Message.Contains(field));
    }

    [Test]
    public void ManifestWithNullSourceFileIsAFormatError()
    {
        var path = Path.Combine(_package, PackageLayout.ManifestFile);
        var manifest = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        manifest["sourceFile"] = null;
        File.WriteAllText(path, manifest.ToJsonString());

        Assert.That(() => PackageReader.Read(_package, CancellationToken.None), Throws.InstanceOf<PackageFormatException>());
    }

    [Test]
    public void GeometryErrorFromANewerVersionStillOpensThePackage()
    {
        var path = Path.Combine(_package, PackageLayout.ManifestFile);
        var manifest = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        manifest["geometryError"] = "GpuOutOfMemory";
        manifest["geometryDetail"] = "needs 12 GB";
        File.WriteAllText(path, manifest.ToJsonString());

        var contents = PackageReader.Read(_package, CancellationToken.None);

        Assert.That(contents.Geometry, Is.EqualTo(new GeometryState.NotBuilt(GeometryError.Unknown, "needs 12 GB")));
    }

    [Test]
    public void DatabaseThatIsNotSqliteIsAFormatError()
    {
        File.WriteAllText(Path.Combine(_package, PackageLayout.DatabaseFile), "plain text");

        Assert.That(() => PackageReader.Read(_package, CancellationToken.None), Throws.InstanceOf<PackageFormatException>());
    }
}
