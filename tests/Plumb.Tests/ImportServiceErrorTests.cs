using Microsoft.Extensions.Logging.Abstractions;
using Plumb.Core.Import;
using Plumb.Import;

namespace Plumb.Tests;

[TestFixture]
public sealed class ImportServiceErrorTests
{
    private const string StepHeaderWithoutProject =
        "ISO-10303-21;\nHEADER;\nFILE_DESCRIPTION((''),'2;1');\nFILE_NAME('x','2026-01-01T00:00:00',(''),(''),'','','');\nFILE_SCHEMA(('IFC2X3'));\nENDSEC;\nDATA;\n#1=IFCCARTESIANPOINT((0.,0.,0.));\nENDSEC;\nEND-ISO-10303-21;\n";

    private readonly ImportService _service = new(NullLogger<ImportService>.Instance, FakeGeometryConverter.Builds());
    private readonly IProgress<ImportProgress> _noProgress = new SyncProgress<ImportProgress>(_ => { });
    private TempDirectory _output = null!;

    private string Output => _output.Path;

    [SetUp]
    public void CreateOutput() => _output = new TempDirectory();

    [TearDown]
    public void DeleteOutput() => _output.Dispose();

    [Test]
    public async Task MissingFileReturnsFileNotFound()
    {
        var result = await _service.RunAsync("/definitely/not/here.ifc", Output, _noProgress, CancellationToken.None);

        AssertFailure(result, ImportError.FileNotFound);
    }

    [Test]
    public async Task PlainFolderReturnsNotIfcWithAClearMessage()
    {
        using var temp = new TempDirectory();
        var folder = Path.Combine(temp.Path, "Drawings");
        Directory.CreateDirectory(folder);

        var result = await _service.RunAsync(folder, Output, _noProgress, CancellationToken.None);

        AssertFailure(result, ImportError.NotIfc);
        Assert.That(((ImportResult.Failure)result).Message, Does.Contain("folder"));
    }

    [Test]
    public async Task WrongExtensionReturnsNotIfc()
    {
        using var temp = new TempDirectory();
        var path = temp.WriteFile("model.txt", StepHeaderWithoutProject);

        var result = await _service.RunAsync(path, Output, _noProgress, CancellationToken.None);

        AssertFailure(result, ImportError.NotIfc);
    }

    [Test]
    public async Task IfcExtensionWithoutStepHeaderReturnsNotIfc()
    {
        using var temp = new TempDirectory();
        var path = temp.WriteFile("model.ifc", "hello, I am not a building");

        var result = await _service.RunAsync(path, Output, _noProgress, CancellationToken.None);

        AssertFailure(result, ImportError.NotIfc);
    }

    [Test]
    public async Task StepFileWithoutProjectReturnsParseFailed()
    {
        using var temp = new TempDirectory();
        var path = temp.WriteFile("empty.ifc", StepHeaderWithoutProject);

        var result = await _service.RunAsync(path, Output, _noProgress, CancellationToken.None);

        AssertFailure(result, ImportError.ParseFailed);
    }

    [Test]
    public async Task StepFileWithUtf8BomPassesValidation()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "bom.ifc");
        File.WriteAllText(path, StepHeaderWithoutProject, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        var result = await _service.RunAsync(path, Output, _noProgress, CancellationToken.None);

        AssertFailure(result, ImportError.ParseFailed);
    }

    [Test]
    public async Task AlreadyCancelledTokenReturnsCancelled()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var result = await _service.RunAsync(Samples.Duplex, Output, _noProgress, cts.Token);

        AssertFailure(result, ImportError.Cancelled);
    }

    [TestCase(10)]
    [TestCase(55)]
    public async Task CancelWhileReadingReturnsCancelled(int atPercent)
    {
        using var cts = new CancellationTokenSource();
        var progress = new SyncProgress<ImportProgress>(p =>
        {
            if (p.Step == ImportStep.ReadingModel && p.Percent >= atPercent)
            {
                cts.Cancel();
            }
        });

        var result = await _service.RunAsync(Samples.Duplex, Output, progress, cts.Token);

        AssertFailure(result, ImportError.Cancelled);
    }

    private static void AssertFailure(ImportResult result, ImportError expected)
    {
        Assert.That(result, Is.TypeOf<ImportResult.Failure>(), () => result.ToString());
        Assert.That(((ImportResult.Failure)result).Error, Is.EqualTo(expected), () => result.ToString());
    }
}
