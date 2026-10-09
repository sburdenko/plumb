using System.Text.Json;
using Microsoft.Data.Sqlite;
using Plumb.Core.Model;
using Plumb.Core.Package;
using Plumb.Package;

namespace Plumb.Tests;

[TestFixture]
public sealed class PackageRoundTripTests
{
    private static readonly IfcModelData Model = new(
        "House.ifc",
        "IFC4",
        [
            new ElementRecord("P", "IfcProject", "Project", null, null),
            new ElementRecord("B", "IfcBuilding", null, "P", null),
            new ElementRecord("L1", "IfcBuildingStorey", "Level 1", "B", "L1"),
            new ElementRecord("W1", "IfcWall", "Wall 'A'", "L1", "L1"),
            new ElementRecord("ST", "IfcStair", "Stair", "L1", "L1"),
            new ElementRecord("SF", "IfcStairFlight", "Flight", "ST", "L1"),
        ],
        [
            new PropertyRecord("W1", "Pset_WallCommon", "IsExternal", "true", null),
            new PropertyRecord("W1", "Qto_WallBaseQuantities", "NetSideArea", "13.6", "m²"),
            new PropertyRecord("W1", "Pset_WallCommon", "FireRating", null, null),
            new PropertyRecord("ST", "Pset_StairCommon", "NumberOfRiser", "16", null),
        ]);

    private static readonly PackageManifest Manifest = new(
        PackageLayout.FormatVersion,
        "House.ifc",
        "ab12",
        "IFC4",
        new DateTime(2026, 10, 8, 12, 0, 0, DateTimeKind.Utc),
        6,
        4210);

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
    public void WritesAllPackageFiles()
    {
        Assert.That(Directory.GetFiles(_package).Select(Path.GetFileName),
            Is.EquivalentTo(new[] { PackageLayout.ManifestFile, PackageLayout.DatabaseFile, PackageLayout.ElementIndexFile }));
    }

    [Test]
    public void ReadsBackTheSameElementsInTheSameOrder()
    {
        var contents = PackageReader.Read(_package, CancellationToken.None);

        Assert.That(contents.Model.Elements, Is.EqualTo(Model.Elements));
    }

    [Test]
    public void ReadsBackTheSameProperties()
    {
        var contents = PackageReader.Read(_package, CancellationToken.None);

        Assert.That(contents.Model.Properties, Is.EqualTo(Model.Properties));
    }

    [Test]
    public void ReadsBackSourceAndSchemaFromTheManifest()
    {
        var contents = PackageReader.Read(_package, CancellationToken.None);

        Assert.That(contents.Manifest, Is.EqualTo(Manifest));
        Assert.That(contents.Model.SourceFile, Is.EqualTo("House.ifc"));
        Assert.That(contents.Model.IfcSchema, Is.EqualTo("IFC4"));
    }

    [Test]
    public void ManifestUsesTheDocumentedFieldNames()
    {
        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(_package, PackageLayout.ManifestFile)));
        var names = json.RootElement.EnumerateObject().Select(p => p.Name);

        Assert.That(names, Is.EquivalentTo(new[]
        {
            "formatVersion", "sourceFile", "sourceSha256", "ifcSchema", "createdUtc", "elementCount", "importDurationMs",
        }));
        Assert.That(json.RootElement.GetProperty("createdUtc").GetString(), Is.EqualTo("2026-10-08T12:00:00Z"));
    }

    [Test]
    public void ElementIndexListsEveryElementWithItsStoreyName()
    {
        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(_package, PackageLayout.ElementIndexFile)));
        var entries = json.RootElement.EnumerateArray().ToList();

        Assert.That(entries.Select(e => e.GetProperty("id").GetString()), Is.EqualTo(Model.Elements.Select(e => e.GlobalId)));
        var wall = entries.Single(e => e.GetProperty("id").GetString() == "W1");
        Assert.That(wall.GetProperty("type").GetString(), Is.EqualTo("IfcWall"));
        Assert.That(wall.GetProperty("name").GetString(), Is.EqualTo("Wall 'A'"));
        Assert.That(wall.GetProperty("storey").GetString(), Is.EqualTo("Level 1"));
    }

    [Test]
    public void DatabaseHasTheDocumentedTablesAndIndex()
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(_package, PackageLayout.DatabaseFile),
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false,
        }.ToString();
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type IN ('table', 'index') AND name NOT LIKE 'sqlite_%'";
        var names = new List<string>();
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                names.Add(reader.GetString(0));
            }
        }

        Assert.That(names, Is.EquivalentTo(new[] { "elements", "properties", "ix_properties_global_id" }));
    }

    [Test]
    public void CancelledWriteThrows()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.That(
            () => PackageWriter.Write(Path.Combine(_temp.Path, "Other.plumb"), Model, Manifest, NoProgress, cts.Token),
            Throws.InstanceOf<OperationCanceledException>());
    }
}
