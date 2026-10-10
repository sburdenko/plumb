using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Plumb.App.Screens.Model;

public sealed partial class LoadedView : UserControl
{
    private TopLevel? _window;

    public LoadedView() => InitializeComponent();

    // Listening on the window catches the shortcut wherever focus is, including when nothing is focused.
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _window = TopLevel.GetTopLevel(this);
        _window?.AddHandler(KeyDownEvent, FocusFilterOnFind, RoutingStrategies.Tunnel);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _window?.RemoveHandler(KeyDownEvent, FocusFilterOnFind);
        _window = null;
        base.OnDetachedFromVisualTree(e);
    }

    /// <summary>⌘F on macOS, Ctrl+F elsewhere: the platform's command modifier with F.</summary>
    private void FocusFilterOnFind(object? sender, KeyEventArgs e)
    {
        var command = Application.Current?.PlatformSettings?.HotkeyConfiguration.CommandModifiers ?? KeyModifiers.Control;
        if (e.Key != Key.F || e.KeyModifiers != command)
        {
            return;
        }

        FilterBox.Focus();
        FilterBox.SelectAll();
        e.Handled = true;
    }
}
