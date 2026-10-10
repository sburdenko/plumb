using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.Logging;
using Plumb.App.Logging;
using Plumb.App.Platform;
using Plumb.App.Screens.Start.Recent;
using Plumb.App.Shell;
using Plumb.Import;
using Plumb.Geometry;
using Plumb.Ifc;

namespace Plumb.App;

public sealed partial class App : Application
{
    private static readonly TimeSpan GeometryTimeout = TimeSpan.FromMinutes(10);
    private static readonly Uri DockIcon = new("avares://Plumb.App/Assets/plumb-dock.png");

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var loggerFactory = AppLogging.CreateFactory();
            var logger = loggerFactory.CreateLogger<App>();
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
                logger.LogCritical(e.ExceptionObject as Exception, "Unhandled exception");
            XbimSetup.Configure(loggerFactory);
            SetDockIcon(logger);

            var window = new MainWindow();
            var viewModel = new MainWindowViewModel(
                new ImportService(
                    loggerFactory.CreateLogger<ImportService>(),
                    new IfcConvertRunner(IfcConvertRunner.DefaultExecutablePath, GeometryTimeout, loggerFactory.CreateLogger<IfcConvertRunner>())),
                new PlatformServices(
                    new StorageFilePickerService(window),
                    new SystemFileRevealer(loggerFactory.CreateLogger<SystemFileRevealer>()),
                    new ViewerLauncher(loggerFactory.CreateLogger<ViewerLauncher>()),
                    new WindowClipboard(window, loggerFactory.CreateLogger<WindowClipboard>())),
                new RecentModelFileStore(RecentModelFileStore.DefaultPath, loggerFactory.CreateLogger<RecentModelFileStore>()),
                TimeProvider.System,
                loggerFactory.CreateLogger<MainWindowViewModel>());
            window.DataContext = viewModel;
            SystemMenus.Install(this, window, viewModel);
            if (this.TryGetFeature<IActivatableLifetime>() is { } activatable)
            {
                activatable.Activated += (_, e) => OpenFromSystem(e, viewModel);
            }

            desktop.MainWindow = window;
            desktop.Exit += (_, _) => loggerFactory.Dispose();
            logger.LogInformation("Plumb started");

            if (desktop.Args is [var initialFile, ..])
            {
                Dispatcher.UIThread.Post(() => Open(viewModel, initialFile));
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>A file double-clicked in Finder, chosen in Open With, or dropped on the Dock icon.</summary>
    private static void OpenFromSystem(ActivatedEventArgs e, MainWindowViewModel viewModel)
    {
        if (e is FileActivatedEventArgs { Files: [var file, ..] } && file.TryGetLocalPath() is { } path)
        {
            Dispatcher.UIThread.Post(() => Open(viewModel, path));
        }
    }

    private static void Open(MainWindowViewModel viewModel, string path)
    {
        if (viewModel.OpenPathCommand.CanExecute(path))
        {
            viewModel.OpenPathCommand.Execute(path);
        }
    }

    private static void SetDockIcon(ILogger logger)
    {
        if (!OperatingSystem.IsMacOS() || MacDockIcon.RunsInsideBundle)
        {
            return;
        }

        try
        {
            using var stream = AssetLoader.Open(DockIcon);
            using var png = new MemoryStream();
            stream.CopyTo(png);
            MacDockIcon.Apply(png.ToArray());
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or InvalidOperationException or IOException)
        {
            logger.LogWarning(ex, "Could not set the Dock icon");
        }
    }
}
