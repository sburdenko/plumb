using System.Diagnostics.CodeAnalysis;
using System.Text;
using Plumb.App.Platform;
using Plumb.App.Screens.Model;
using Plumb.App.Screens.Model.Tree;
using Plumb.App.Screens.Model.Viewport;
using Plumb.App.Shell;
using Plumb.Core.Geometry;
using Plumb.Core.Import;
using Plumb.Core.Model;
using Plumb.Geometry;
using CommunityToolkit.Mvvm.Input;

namespace Plumb.Tests;

[TestFixture]
public sealed class LoadedViewModelTests
{
    private static readonly IfcModelData Model = new(
        "House.ifc",
        "IFC4",
        [
            new ElementRecord("P", "IfcProject", "House project", null, null),
            new ElementRecord("B", "IfcBuilding", "Duplex Residence", "P", null),
            new ElementRecord("L1", "IfcBuildingStorey", "Level 1", "B", "L1"),
            new ElementRecord("L2", "IfcBuildingStorey", "Level 2", "B", "L2"),
            new ElementRecord("W1", "IfcWall", "Wall A", "L1", "L1"),
            new ElementRecord("D2", "IfcDoor", "Door B", "L2", "L2"),
        ],
        [
            new PropertyRecord("W1", "Pset_WallCommon", "IsExternal", "true", null),
            new PropertyRecord("W1", "Qto_WallBaseQuantities", "Length", "4.57", "m"),
        ]);

    private static readonly OpenCommands Open = new(
        new AsyncRelayCommand(() => Task.CompletedTask),
        new AsyncRelayCommand(() => Task.CompletedTask),
        new AsyncRelayCommand<string?>(_ => Task.CompletedTask));

    [Test]
    public void HeaderAndStatusDescribeTheModel()
    {
        var loaded = Create(new PackageState.Saved("/m/House.plumb", new GeometryState.Built()));

        Assert.That(loaded.ProjectName, Is.EqualTo("Duplex Residence"));
        Assert.That(loaded.FileName, Is.EqualTo("House.ifc"));
        Assert.That(loaded.Schema, Is.EqualTo("IFC4"));
        Assert.That(loaded.Counts, Is.EqualTo("6 elements · 2 values"));
        Assert.That(loaded.LoadTime, Is.EqualTo("1.2 s"));
    }

    [Test]
    public void SavedWithGeometryShowsTheViewerAndTwoFineStatusItems()
    {
        var loaded = Create(new PackageState.Saved("/m/House.plumb", new GeometryState.Built()));

        Assert.That(loaded.Viewport, Is.TypeOf<ViewportReadyViewModel>());
        Assert.That(loaded.StatusItems.Select(i => (i.Text, i.IsProblem)), Is.EqualTo(new[] { ("Package saved", false), ("Geometry built", false) }));
        Assert.That(loaded.StatusItems[0].IsAction, Is.True, "Package saved reveals the package");
    }

    [Test]
    public void MissingGeometryExplainsItAndOffersReimport()
    {
        var geometry = new GeometryState.NotBuilt(GeometryError.ConverterMissing, "not found at /app/IfcConvert");
        var loaded = Create(new PackageState.Saved("/m/House.plumb", geometry));

        var viewport = (ViewportGeometryMissingViewModel)loaded.Viewport;
        Assert.That(viewport.Reason, Does.StartWith("IfcConvert was not found next to the app."));
        Assert.That(viewport.Detail, Is.EqualTo("not found at /app/IfcConvert"));
        Assert.That(viewport.SourcePath, Is.EqualTo("/m/House.ifc"));
        Assert.That(loaded.StatusItems[1].IsProblem, Is.True);
    }

    [Test]
    public void UnsavedPackageMarksBothStatusItemsAsProblems()
    {
        var loaded = Create(new PackageState.NotSaved("House.plumb was not saved: the folder m is read-only."));

        Assert.That(loaded.Viewport, Is.TypeOf<ViewportNotSavedViewModel>());
        Assert.That(loaded.StatusItems.Select(i => i.IsProblem), Has.All.True);
        Assert.That(loaded.StatusItems[0].Reason, Does.Contain("read-only"));
    }

    [Test]
    public void SelectingAnElementMarksItsStoreyAndFillsTheDetails()
    {
        var loaded = Create(new PackageState.Saved("/m/House.plumb", new GeometryState.Built()));
        var building = loaded.Nodes.Single().Children.Single();
        var level1 = building.Children.Single(n => n.Title == "Level 1");
        var level2 = building.Children.Single(n => n.Title == "Level 2");

        loaded.SelectedNode = level1.Children.Single().Children.Single();

        Assert.That(level1.IsActiveStorey, Is.True);
        Assert.That(level2.IsActiveStorey, Is.False);
        Assert.That(loaded.SelectedName, Is.EqualTo("Wall A"));
        Assert.That(loaded.SelectedElement!.Kicker, Is.EqualTo("IFCWALL"));
        Assert.That(loaded.SelectedElement.Groups.SelectMany(g => g.Rows).Select(r => r.FullValue), Is.EquivalentTo(new[] { "true", "4.57 m" }));
    }

    [Test]
    public void ClearingTheFilterRestoresTheExpansionTheUserChose()
    {
        var loaded = Create(new PackageState.Saved("/m/House.plumb", new GeometryState.Built()));
        Storey(loaded, "Level 2").IsExpanded = true;

        loaded.Filter = "Wall";
        loaded.ClearFilterCommand.Execute(null);

        Assert.That(Storey(loaded, "Level 2").IsExpanded, Is.True);
        Assert.That(Storey(loaded, "Level 1").IsExpanded, Is.False);
    }

    [Test]
    public void GlbNodesAreCountedFromTheJsonChunk()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "model.glb");
        File.WriteAllBytes(path, Glb("""{"asset":{"version":"2.0"},"nodes":[{"name":"A"},{"name":"B"},{"name":"C"}]}"""));

        Assert.That(GlbInfo.CountNodes(path), Is.EqualTo(3));
        Assert.That(GlbInfo.CountNodes(Path.Combine(temp.Path, "missing.glb")), Is.Null);
    }

    private static TreeNodeViewModel Storey(LoadedViewModel loaded, string name) =>
        loaded.Nodes.Single().Children.Single().Children.Single(n => n.Title == name);

    private static LoadedViewModel Create(PackageState package) =>
        new(new LoadedModel(new ImportResult.Success(Model, TimeSpan.FromSeconds(3), package), "/m/House.ifc", OpenedPackage: false, TimeSpan.FromMilliseconds(1234)),
            Open, new PlatformServices(new NoPicker(), new NoRevealer(), new NoViewer(), new FakeClipboard()), TimeProvider.System);

    private static byte[] Glb(string json)
    {
        var padded = json.PadRight((json.Length + 3) / 4 * 4);
        var chunk = Encoding.UTF8.GetBytes(padded);
        var bytes = new List<byte>();
        bytes.AddRange(Encoding.ASCII.GetBytes("glTF"));
        bytes.AddRange(BitConverter.GetBytes(2u));
        bytes.AddRange(BitConverter.GetBytes((uint)(12 + 8 + chunk.Length)));
        bytes.AddRange(BitConverter.GetBytes((uint)chunk.Length));
        bytes.AddRange(BitConverter.GetBytes(0x4E4F534Au));
        bytes.AddRange(chunk);
        return bytes.ToArray();
    }

    private sealed class NoPicker : IFilePickerService
    {
        public Task<string?> PickIfcFileAsync() => Task.FromResult<string?>(null);

        public Task<string?> PickPackageAsync() => Task.FromResult<string?>(null);
    }

    private sealed class NoRevealer : IFileRevealer
    {
        public string ActionLabel => "Show";

        public bool TryReveal(string path, [NotNullWhen(false)] out string? error)
        {
            error = null;
            return true;
        }
    }

    private sealed class NoViewer : IViewerLauncher
    {
        public bool TryOpen(string packagePath, [NotNullWhen(false)] out string? error)
        {
            error = null;
            return true;
        }
    }
}
