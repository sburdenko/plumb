using Plumb.App.Screens.Start.Recent;

namespace Plumb.Tests;

[TestFixture]
public sealed class RecentModelFileStoreTests
{
    private static readonly DateTimeOffset Opened = new(2026, 10, 9, 15, 30, 0, TimeSpan.FromHours(3));

    private TempDirectory _folder = null!;
    private CollectingLogger<RecentModelFileStore> _logger = null!;
    private string _file = null!;

    [SetUp]
    public void CreateFolder()
    {
        _folder = new TempDirectory();
        _logger = new CollectingLogger<RecentModelFileStore>();
        _file = Path.Combine(_folder.Path, "Plumb", "recent.json");
    }

    [TearDown]
    public void RemoveFolder() => _folder.Dispose();

    [Test]
    public void SavedModelsLoadBack()
    {
        var pinned = new RecentModel(Rooted("Duplex.ifc"), Rooted("Duplex.plumb"), "Duplex", "IFC2X3", 246, Opened, IsPinned: true);
        var noPackage = new RecentModel(Rooted("Office.ifc"), null, "Office", "IFC4", 1200, Opened.AddDays(-1), IsPinned: false);
        var store = Store();

        store.Save(RecentModelList.Of([pinned, noPackage]));

        Assert.That(Store().Load().Models, Is.EqualTo(new[] { pinned, noPackage }), _logger.Text);
    }

    [Test]
    public void SavingCreatesTheFolderAndLeavesNoTemporaryFile()
    {
        Store().Save(RecentModelList.Empty.Record(new RecentModel(Rooted("a.ifc"), null, "a", "IFC4", 1, Opened, IsPinned: false)));

        Assert.That(Directory.GetFiles(Path.GetDirectoryName(_file)!), Is.EqualTo(new[] { _file }));
    }

    [Test]
    public void NoFileMeansNoModels()
    {
        Assert.That(Store().Load().Models, Is.Empty);
        Assert.That(_logger.Text, Is.Empty);
    }

    [Test]
    public void ACorruptFileMeansNoModelsAndIsLogged()
    {
        WriteFile("{ not json");

        Assert.That(Store().Load().Models, Is.Empty);
        Assert.That(_logger.Text, Does.Contain("Warning"));
    }

    [Test]
    public void AnUnknownVersionMeansNoModels()
    {
        WriteFile("""{ "version": 99, "models": [] }""");

        Assert.That(Store().Load().Models, Is.Empty);
        Assert.That(_logger.Text, Does.Contain("99"));
    }

    [Test]
    public void EntriesWithoutAnAbsolutePathAreSkipped()
    {
        var kept = Rooted("kept.ifc").Replace("\\", "\\\\");
        WriteFile($$"""
            {
              "version": 1,
              "models": [
                { "sourcePath": "relative.ifc", "name": "relative", "schema": "IFC4", "elementCount": 1, "lastOpened": "2026-10-09T12:00:00+00:00", "isPinned": false },
                { "name": "nowhere", "schema": "IFC4", "elementCount": 1, "lastOpened": "2026-10-09T12:00:00+00:00", "isPinned": false },
                { "sourcePath": "{{kept}}", "name": "kept", "schema": "IFC4", "elementCount": 1, "lastOpened": "2026-10-09T12:00:00+00:00", "isPinned": false }
              ]
            }
            """);

        Assert.That(Store().Load().Models.Select(m => m.Name), Is.EqualTo(new[] { "kept" }));
        Assert.That(_logger.Text, Does.Contain("relative"));
    }

    private RecentModelFileStore Store() => new(_file, _logger);

    private string Rooted(string name) => Path.Combine(_folder.Path, name);

    private void WriteFile(string json)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_file)!);
        File.WriteAllText(_file, json);
    }
}
