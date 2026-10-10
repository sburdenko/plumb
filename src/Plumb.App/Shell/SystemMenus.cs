using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Plumb.App.Screens.Start.Recent;

namespace Plumb.App.Shell;

/// <summary>
/// The menus macOS shows outside the window: File in the menu bar and the Dock icon's menu, both with the recent
/// models. Windows and Linux do not show native menus; there the window's buttons and Ctrl shortcuts do the same.
/// </summary>
internal static class SystemMenus
{
    public static void Install(Application application, Window window, MainWindowViewModel viewModel)
    {
        var openRecent = new NativeMenu();
        var dock = new NativeMenu();
        NativeMenu.SetMenu(window, new NativeMenu { Items = { FileMenu(viewModel, openRecent) } });
        NativeDock.SetMenu(application, dock);

        void Refresh()
        {
            Fill(openRecent, viewModel.Recent.MenuItems);
            Fill(dock, viewModel.Recent.MenuItems);
        }

        viewModel.Recent.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(RecentModelsViewModel.MenuItems))
            {
                Refresh();
            }
        };
        Refresh();
    }

    private static NativeMenuItem FileMenu(MainWindowViewModel viewModel, NativeMenu openRecent) => new("File")
    {
        Menu = new NativeMenu
        {
            Items =
            {
                new NativeMenuItem("Open IFC…")
                {
                    Command = viewModel.OpenIfcCommand,
                    Gesture = new KeyGesture(Key.O, KeyModifiers.Meta),
                },
                new NativeMenuItem("Open Package…")
                {
                    Command = viewModel.OpenPackageCommand,
                    Gesture = new KeyGesture(Key.O, KeyModifiers.Meta | KeyModifiers.Shift),
                },
                new NativeMenuItem("Open Recent") { Menu = openRecent },
            },
        },
    };

    // Native menus are built from mutable item lists, so each refresh refills the same menu.
    private static void Fill(NativeMenu menu, IReadOnlyList<RecentModelItemViewModel> items)
    {
        menu.Items.Clear();
        foreach (var item in items)
        {
            menu.Items.Add(new NativeMenuItem(item.Title) { Command = item.OpenCommand, ToolTip = item.Location });
        }

        if (items.Count == 0)
        {
            menu.Items.Add(new NativeMenuItem("No recent models") { IsEnabled = false });
        }
    }
}
