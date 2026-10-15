using Plumb.App.Platform.Viewer;

namespace Plumb.App.Platform;

/// <summary>Everything the app asks of the operating system, passed around as one value.</summary>
public sealed record PlatformServices(
    IFilePickerService FilePicker,
    IFileRevealer Revealer,
    IViewerLink Viewer,
    ITextClipboard Clipboard);
