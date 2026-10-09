using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using Plumb.App.Logging;
using Plumb.App.Services;
using Plumb.App.ViewModels;
using Plumb.App.Views;
using Plumb.Import;
using Plumb.Geometry;
using Plumb.Ifc;

namespace Plumb.App;

public sealed partial class App : Application
{
    private static readonly TimeSpan GeometryTimeout = TimeSpan.FromMinutes(10);

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

            var window = new MainWindow();
            var viewModel = new MainWindowViewModel(
                new ImportService(
                    loggerFactory.CreateLogger<ImportService>(),
                    new IfcConvertRunner(IfcConvertRunner.DefaultExecutablePath, GeometryTimeout, loggerFactory.CreateLogger<IfcConvertRunner>())),
                new StorageFilePickerService(window),
                new SystemFileRevealer(loggerFactory.CreateLogger<SystemFileRevealer>()),
                loggerFactory.CreateLogger<MainWindowViewModel>());
            window.DataContext = viewModel;

            desktop.MainWindow = window;
            desktop.Exit += (_, _) => loggerFactory.Dispose();
            logger.LogInformation("Plumb started");

            if (desktop.Args is [var initialFile, ..])
            {
                Dispatcher.UIThread.Post(() => viewModel.OpenPathCommand.Execute(initialFile));
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}
