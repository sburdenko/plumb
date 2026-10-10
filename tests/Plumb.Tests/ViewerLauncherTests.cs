using Plumb.App.Platform;

namespace Plumb.Tests;

[TestFixture]
public sealed class ViewerLauncherTests
{
    private TempDirectory _temp = null!;

    [SetUp]
    public void CreateTemp() => _temp = new TempDirectory();

    [TearDown]
    public void DeleteTemp() => _temp.Dispose();

    [Test]
    public void MissingLocationHasNoViewer()
    {
        Assert.That(ViewerLauncher.FindExecutable(Path.Combine(_temp.Path, "viewer-build")), Is.Null);
    }

    [Test]
    public void ExecutablePathIsUsedAsIs()
    {
        var executable = _temp.WriteFile("custom-viewer", "binary");

        Assert.That(ViewerLauncher.FindExecutable(executable), Is.EqualTo(executable));
    }

    [Test]
    [Platform(Include = "MacOsX")]
    public void FindsTheBinaryInsideTheAppBundle()
    {
        var binaries = Path.Combine(_temp.Path, "PlumbViewer.app", "Contents", "MacOS");
        Directory.CreateDirectory(binaries);
        var binary = Path.Combine(binaries, "Plumb Viewer");
        File.WriteAllText(binary, "binary");

        Assert.That(ViewerLauncher.FindExecutable(_temp.Path), Is.EqualTo(binary));
        Assert.That(ViewerLauncher.FindExecutable(Path.Combine(_temp.Path, "PlumbViewer.app")), Is.EqualTo(binary));
    }

    [Test]
    [Platform(Include = "Linux")]
    public void FindsTheLinuxPlayer()
    {
        var player = _temp.WriteFile("PlumbViewer.x86_64", "binary");

        Assert.That(ViewerLauncher.FindExecutable(_temp.Path), Is.EqualTo(player));
    }

    [Test]
    public void MissingPackageIsReportedBeforeStartingTheViewer()
    {
        var launcher = new ViewerLauncher(Microsoft.Extensions.Logging.Abstractions.NullLogger<ViewerLauncher>.Instance);

        var opened = launcher.TryOpen(Path.Combine(_temp.Path, "Gone.plumb"), out var error);

        Assert.That(opened, Is.False);
        Assert.That(error, Is.EqualTo("Gone.plumb no longer exists."));
    }

    [Test]
    [Platform(Include = "Win")]
    public void FindsTheWindowsExecutable()
    {
        var folder = Path.Combine(_temp.Path, "PlumbViewer");
        Directory.CreateDirectory(folder);
        var exe = Path.Combine(folder, "PlumbViewer.exe");
        File.WriteAllText(exe, "binary");

        Assert.That(ViewerLauncher.FindExecutable(_temp.Path), Is.EqualTo(exe));
    }
}
