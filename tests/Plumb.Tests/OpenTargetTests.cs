using Plumb.App.Screens.Start.Recent;
using Plumb.Core.Package;

namespace Plumb.Tests;

[TestFixture]
public sealed class OpenTargetTests
{
    private static readonly DateTime Imported = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

    private TempDirectory _folder = null!;
    private string _source = null!;
    private string _package = null!;

    [SetUp]
    public void CreateModel()
    {
        _folder = new TempDirectory();
        _source = _folder.WriteFile("Duplex.ifc", "ISO-10303-21;");
        _package = Path.Combine(_folder.Path, "Duplex.plumb");
        Directory.CreateDirectory(_package);
        File.WriteAllText(Path.Combine(_package, PackageLayout.ManifestFile), "{}");
        File.SetLastWriteTimeUtc(_source, Imported.AddMinutes(-5));
        File.SetLastWriteTimeUtc(Path.Combine(_package, PackageLayout.ManifestFile), Imported);
    }

    [TearDown]
    public void RemoveModel() => _folder.Dispose();

    [Test]
    public void OpensThePackageWhenTheSourceIsUnchanged()
    {
        Assert.That(OpenTarget.For(Model()), Is.EqualTo(new OpenTarget.Package(_package)));
    }

    [Test]
    public void ImportsTheSourceAgainWhenItWasSavedAfterTheImport()
    {
        File.SetLastWriteTimeUtc(_source, Imported.AddMinutes(10));

        Assert.That(OpenTarget.For(Model()), Is.EqualTo(new OpenTarget.Source(_source)));
    }

    [Test]
    public void ImportsTheSourceWhenThePackageIsGone()
    {
        Directory.Delete(_package, recursive: true);

        Assert.That(OpenTarget.For(Model()), Is.EqualTo(new OpenTarget.Source(_source)));
    }

    [Test]
    public void OpensThePackageWhenTheSourceIsGone()
    {
        File.Delete(_source);

        Assert.That(OpenTarget.For(Model()), Is.EqualTo(new OpenTarget.Package(_package)));
    }

    [Test]
    public void IsMissingWhenNeitherExists()
    {
        File.Delete(_source);
        Directory.Delete(_package, recursive: true);

        Assert.That(OpenTarget.For(Model()), Is.TypeOf<OpenTarget.Missing>());
    }

    [Test]
    public void AModelWithoutAPackageOpensItsSource()
    {
        var model = Model() with { PackagePath = null };

        Assert.That(OpenTarget.For(model), Is.EqualTo(new OpenTarget.Source(_source)));
    }

    private RecentModel Model() =>
        new(_source, _package, "Duplex", "IFC2X3", 246, DateTimeOffset.UnixEpoch, IsPinned: false);
}
