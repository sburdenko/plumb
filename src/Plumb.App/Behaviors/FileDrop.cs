using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;

namespace Plumb.App.Behaviors;

/// <summary>
/// Attached property that executes a command with the local path of a file dropped on the control.
/// </summary>
public sealed class FileDrop : AvaloniaObject
{
    public static readonly AttachedProperty<ICommand?> CommandProperty =
        AvaloniaProperty.RegisterAttached<FileDrop, Control, ICommand?>("Command");

    static FileDrop()
    {
        CommandProperty.Changed.AddClassHandler<Control>(OnCommandChanged);
    }

    private FileDrop()
    {
    }

    public static ICommand? GetCommand(Control control) => control.GetValue(CommandProperty);

    public static void SetCommand(Control control, ICommand? value) => control.SetValue(CommandProperty, value);

    private static void OnCommandChanged(Control control, AvaloniaPropertyChangedEventArgs args)
    {
        control.RemoveHandler(DragDrop.DragOverEvent, OnDragOver);
        control.RemoveHandler(DragDrop.DropEvent, OnDrop);

        var hasCommand = args.NewValue is ICommand;
        DragDrop.SetAllowDrop(control, hasCommand);
        if (hasCommand)
        {
            control.AddHandler(DragDrop.DragOverEvent, OnDragOver);
            control.AddHandler(DragDrop.DropEvent, OnDrop);
        }
    }

    private static void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = TryGetAccepted(sender, e) is not null ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private static void OnDrop(object? sender, DragEventArgs e)
    {
        if (TryGetAccepted(sender, e) is var (command, path))
        {
            command.Execute(path);
        }

        e.Handled = true;
    }

    private static (ICommand Command, string Path)? TryGetAccepted(object? sender, DragEventArgs e)
    {
        var command = (sender as Control)?.GetValue(CommandProperty);
        var path = e.DataTransfer.TryGetFile()?.TryGetLocalPath();
        return command != null && !string.IsNullOrEmpty(path) && command.CanExecute(path) ? (command, path) : null;
    }
}
