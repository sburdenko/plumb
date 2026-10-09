using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Plumb.Core.Geometry;
using Plumb.Core.Import;
using Plumb.Core.Package;
using Plumb.Import;
using Xbim.Ifc4.Interfaces;
using Xbim.IO.Memory;

namespace Plumb.Tests;

[TestFixture]
public sealed class ImportServicePackageTests
{
    private readonly ImportService _service = new(NullLogger<ImportService>.Instance, FakeGeometryConverter.Builds());
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
    public async Task ImportWritesPackageNextToSource()
    {
        var result = await ImportAsync();

        Assert.That(result.Package, Is.EqualTo(new PackageState.Saved(_package, new GeometryState.Built())));
        Assert.That(Directory.GetFiles(_package).Select(Path.GetFileName), Is.EquivalentTo(new[]
        {
            PackageLayout.ManifestFile, PackageLayout.DatabaseFile, PackageLayout.ElementIndexFile, PackageLayout.GeometryFile,
        }));
    }

    [Test]
    public async Task DatabaseElementCountMatchesXbim()
    {
        await ImportAsync();
        using var model = MemoryModel.OpenRead(_ifc);
        var expected = model.Instances.OfType<IIfcProduct>().Count(p => p is not IIfcOpeningElement) + 1;

        Assert.That(CountRows("SELECT COUNT(*) FROM elements"), Is.EqualTo(expected));
    }

    [Test]
    public async Task EveryElementIndexIdExistsInTheDatabase()
    {
        await ImportAsync();
        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(_package, PackageLayout.ElementIndexFile)));
        var ids = json.RootElement.EnumerateArray().Select(e => e.GetProperty("id").GetString()).ToList();

        Assert.That(ids, Is.Not.Empty);
        Assert.That(CountRows("SELECT COUNT(*) FROM elements"), Is.EqualTo(ids.Count));
        foreach (var id in ids)
        {
            Assert.That(CountRows($"SELECT COUNT(*) FROM elements WHERE global_id = '{id!.Replace("'", "''")}'"), Is.EqualTo(1), id);
        }
    }

    [Test]
    public async Task ManifestDescribesTheSource()
    {
        var result = await ImportAsync();
        var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(_package, PackageLayout.ManifestFile))).RootElement;

        Assert.That(manifest.GetProperty("sourceFile").GetString(), Is.EqualTo("Duplex.ifc"));
        Assert.That(manifest.GetProperty("ifcSchema").GetString(), Is.EqualTo("IFC2X3"));
        Assert.That(manifest.GetProperty("elementCount").GetInt32(), Is.EqualTo(result.Model.Elements.Count));
        Assert.That(manifest.GetProperty("sourceSha256").GetString(),
            Is.EqualTo(Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(_ifc)))));
    }

    [Test]
    public async Task OpeningThePackageGivesTheSameModelWithoutTheIfcFile()
    {
        var imported = await ImportAsync();
        File.Delete(_ifc);

        var opened = await _service.OpenPackageAsync(_package, _noProgress, CancellationToken.None);

        var success = (ImportResult.Success)opened;
        Assert.That(success.Model.Elements, Is.EqualTo(imported.Model.Elements));
        Assert.That(success.Model.Properties, Is.EqualTo(imported.Model.Properties));
        Assert.That(success.ImportDuration, Is.EqualTo(imported.ImportDuration));
        Assert.That(success.Package, Is.EqualTo(new PackageState.Saved(_package, new GeometryState.Built())));
    }

    [Test]
    public async Task ReimportReplacesTheExistingPackage()
    {
        await ImportAsync();
        var marker = Path.Combine(_package, "stale.txt");
        File.WriteAllText(marker, "from the previous import");

        await ImportAsync();

        Assert.That(File.Exists(marker), Is.False);
        Assert.That(Directory.Exists(_package), Is.True);
        Assert.That(LeftoverDrafts(), Is.Empty);
    }

    [Test]
    public async Task CancelWhileWritingKeepsTheOldPackageAndLeavesNoDraft()
    {
        await ImportAsync();
        var marker = Path.Combine(_package, "stale.txt");
        File.WriteAllText(marker, "from the previous import");
        using var cts = new CancellationTokenSource();
        var draftExistedOnCancel = false;
        var progress = new SyncProgress<ImportProgress>(p =>
        {
            if (p.Step == ImportStep.WritingPackage && p.Percent > 90 && !cts.IsCancellationRequested)
            {
                draftExistedOnCancel = LeftoverDrafts().Any();
                cts.Cancel();
            }
        });

        var result = await _service.RunAsync(_ifc, _temp.Path, progress, cts.Token);

        Assert.That(((ImportResult.Failure)result).Error, Is.EqualTo(ImportError.Cancelled));
        Assert.That(draftExistedOnCancel, Is.True, "cancellation must hit while the draft is on disk");
        Assert.That(File.Exists(marker), Is.True);
        Assert.That(LeftoverDrafts(), Is.Empty);
    }

    [Test]
    public async Task FailedImportLeavesNothingBehind()
    {
        var broken = Path.Combine(_temp.Path, "Broken.ifc");
        File.WriteAllText(broken, "ISO-10303-21;\nHEADER;\nENDSEC;\nDATA;\nENDSEC;\nEND-ISO-10303-21;\n");

        var result = await _service.RunAsync(broken, _temp.Path, _noProgress, CancellationToken.None);

        Assert.That(result, Is.TypeOf<ImportResult.Failure>());
        Assert.That(Directory.GetDirectories(_temp.Path), Is.Empty);
    }

    [Test]
    public async Task ProgressWalksThroughEveryStepToHundred()
    {
        var reports = new List<ImportProgress>();

        await _service.RunAsync(_ifc, _temp.Path, new SyncProgress<ImportProgress>(reports.Add), CancellationToken.None);

        Assert.That(reports.Select(r => r.Step).Distinct(), Is.EqualTo(new[]
        {
            ImportStep.Validating, ImportStep.ReadingModel, ImportStep.ConvertingGeometry, ImportStep.WritingPackage, ImportStep.Finalizing,
        }));
        Assert.That(reports.Select(r => r.Percent), Is.Ordered);
        Assert.That(reports[^1].Percent, Is.EqualTo(100));
    }

    [Test]
    public async Task UnwritableOutputStillLoadsTheModel()
    {
        var result = await _service.RunAsync(_ifc, outputDirectory: _ifc, _noProgress, CancellationToken.None);

        var success = (ImportResult.Success)result;
        Assert.That(success.Model.Elements, Is.Not.Empty);
        var notSaved = (PackageState.NotSaved)success.Package;
        Assert.That(notSaved.Reason, Does.StartWith("Duplex.plumb was not saved"));
        Assert.That(notSaved.Reason, Does.Not.Contain(".draft"));
        Assert.That(LeftoverDrafts(), Is.Empty);
    }

    [Test]
    [Platform(Exclude = "Win", Reason = "Unix file modes; Windows folder permissions work differently")]
    [UnsupportedOSPlatform("windows")]
    public async Task ReadOnlySourceFolderStillLoadsTheModel()
    {
        var folder = Path.Combine(_temp.Path, "readonly");
        Directory.CreateDirectory(folder);
        var ifc = Path.Combine(folder, "Duplex.ifc");
        File.Copy(_ifc, ifc);
        File.SetUnixFileMode(folder, UnixFileMode.UserRead | UnixFileMode.UserExecute);
        try
        {
            var result = await _service.RunAsync(ifc, folder, _noProgress, CancellationToken.None);

            var success = (ImportResult.Success)result;
            Assert.That(success.Model.Elements, Is.Not.Empty);
            var notSaved = (PackageState.NotSaved)success.Package;
            Assert.That(notSaved.Reason, Is.EqualTo("Duplex.plumb was not saved: the folder readonly is read-only."));
            Assert.That(Directory.GetDirectories(folder), Is.Empty);
        }
        finally
        {
            File.SetUnixFileMode(folder, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    [Test]
    public async Task FailedGeometryStillSavesThePackageAndRemembersWhy()
    {
        const string detail = "exit code 1: Unable to parse input file";
        var service = new ImportService(NullLogger<ImportService>.Instance, FakeGeometryConverter.Fails(GeometryError.ConverterFailed, detail));

        var imported = (ImportResult.Success)await service.RunAsync(_ifc, _temp.Path, _noProgress, CancellationToken.None);
        var opened = (ImportResult.Success)await service.OpenPackageAsync(_package, _noProgress, CancellationToken.None);

        var expected = new PackageState.Saved(_package, new GeometryState.NotBuilt(GeometryError.ConverterFailed, detail));
        Assert.That(imported.Package, Is.EqualTo(expected));
        Assert.That(opened.Package, Is.EqualTo(expected));
        Assert.That(File.Exists(Path.Combine(_package, PackageLayout.GeometryFile)), Is.False);
    }

    [Test]
    public async Task GeometryStartsWhileTheModelIsStillBeingRead()
    {
        var readingFinished = false;
        bool? finishedWhenGeometryStarted = null;
        var service = new ImportService(
            NullLogger<ImportService>.Instance,
            FakeGeometryConverter.BuildsAfter(_ => finishedWhenGeometryStarted = readingFinished));
        var progress = new SyncProgress<ImportProgress>(p =>
            readingFinished |= p.Step is ImportStep.ReadingModel && p.Percent == 60 || p.Step > ImportStep.ReadingModel);

        await service.RunAsync(_ifc, _temp.Path, progress, CancellationToken.None);

        Assert.That(finishedWhenGeometryStarted, Is.False);
    }

    [Test]
    public async Task GeometryAndManifestDescribeTheSameBytes()
    {
        string? converterInput = null;
        string? hashOfConverterInput = null;
        var service = new ImportService(
            NullLogger<ImportService>.Instance,
            FakeGeometryConverter.BuildsAfter(path =>
            {
                converterInput = path;
                hashOfConverterInput = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
                File.AppendAllText(_ifc, "/* edited while importing */");
            }));

        await service.RunAsync(_ifc, _temp.Path, _noProgress, CancellationToken.None);

        var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(_package, PackageLayout.ManifestFile))).RootElement;
        Assert.That(converterInput, Is.Not.EqualTo(_ifc), "IfcConvert should read a snapshot, not the file being edited");
        Assert.That(manifest.GetProperty("sourceSha256").GetString(), Is.EqualTo(hashOfConverterInput));
        Assert.That(Directory.GetFiles(_temp.Path).Select(Path.GetFileName), Is.EqualTo(new[] { "Duplex.ifc" }), "the snapshot is removed");
    }

    [Test]
    public async Task FailedReadStopsTheGeometryAndLeavesNothingBehind()
    {
        var geometryCancelled = new TaskCompletionSource();
        var broken = Path.Combine(_temp.Path, "Broken.ifc");
        File.WriteAllText(broken, "ISO-10303-21;\nHEADER;\nFILE_SCHEMA(('IFC2X3'));\nENDSEC;\nDATA;\nENDSEC;\nEND-ISO-10303-21;\n");
        var service = new ImportService(NullLogger<ImportService>.Instance, FakeGeometryConverter.WaitsForCancellation(geometryCancelled));

        var result = await service.RunAsync(broken, _temp.Path, _noProgress, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(30));

        Assert.That(((ImportResult.Failure)result).Error, Is.EqualTo(ImportError.ParseFailed));
        Assert.That(geometryCancelled.Task.IsCompleted, Is.True, "geometry must be stopped when reading fails");
        Assert.That(Directory.GetFileSystemEntries(_temp.Path).Select(Path.GetFileName), Is.EquivalentTo(new[] { "Duplex.ifc", "Broken.ifc" }));
    }

    [Test]
    public async Task OpeningAMissingPackageReturnsFileNotFound()
    {
        var result = await _service.OpenPackageAsync(_package, _noProgress, CancellationToken.None);

        Assert.That(((ImportResult.Failure)result).Error, Is.EqualTo(ImportError.FileNotFound));
    }

    [Test]
    public async Task OpeningABrokenPackageReturnsPackageInvalid()
    {
        Directory.CreateDirectory(_package);

        var result = await _service.OpenPackageAsync(_package, _noProgress, CancellationToken.None);

        Assert.That(((ImportResult.Failure)result).Error, Is.EqualTo(ImportError.PackageInvalid));
    }

    private async Task<ImportResult.Success> ImportAsync()
    {
        var result = await _service.RunAsync(_ifc, _temp.Path, _noProgress, CancellationToken.None);
        Assert.That(result, Is.TypeOf<ImportResult.Success>(), () => result.ToString());
        return (ImportResult.Success)result;
    }

    private IEnumerable<string> LeftoverDrafts() =>
        Directory.GetDirectories(_temp.Path).Where(d => Path.GetFileName(d).StartsWith('.'));

    private long CountRows(string sql)
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
        command.CommandText = sql;
        return (long)command.ExecuteScalar()!;
    }
}
