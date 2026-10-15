using Microsoft.Extensions.Logging.Abstractions;
using Plumb.App.Platform;
using Plumb.App.Screens.Importing;
using Plumb.App.Shell;
using Plumb.Core.Geometry;
using Plumb.Core.Import;
using Plumb.Core.Model;

namespace Plumb.Tests;

/// <summary>A main window wired to fakes, shared by the main window fixtures.</summary>
internal sealed class MainWindowHarness
{
    public static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(5);

    public static readonly ImportResult.Success Loaded = new(
        new IfcModelData("a.ifc", "IFC4", [new ElementRecord("P", "IfcProject", "Project", null, null)], []),
        TimeSpan.FromSeconds(1),
        new PackageState.Saved("/models/a.plumb", new GeometryState.Built()));

    public MainWindowHarness()
    {
        ViewModel = new MainWindowViewModel(
            Import,
            new PlatformServices(Picker, Revealer, Viewer, new FakeClipboard()),
            Recent,
            TimeProvider.System,
            NullLogger<MainWindowViewModel>.Instance);
    }

    public FakeImportService Import { get; } = new();

    public FakeFilePicker Picker { get; } = new();

    public FakeRevealer Revealer { get; } = new();

    public FakeViewerLink Viewer { get; } = new();

    public FakeRecentModelStore Recent { get; } = new();

    public MainWindowViewModel ViewModel { get; }

    public Task OpenAsync(string path) => ViewModel.OpenPathCommand.ExecuteAsync(path).WaitAsync(TestTimeout);

    public T Current<T>() where T : ScreenViewModel => (T)ViewModel.CurrentState;

    public async Task CancelBlockingImportAsync(string path)
    {
        var import = ViewModel.OpenPathCommand.ExecuteAsync(path);
        Current<ImportingViewModel>().CancelCommand.Execute(null);
        await import.WaitAsync(TestTimeout);
    }
}
