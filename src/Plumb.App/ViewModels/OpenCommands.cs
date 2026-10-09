using CommunityToolkit.Mvvm.Input;

namespace Plumb.App.ViewModels;

/// <summary>
/// The "Open IFC" and "Open package" buttons, shared by every window state that shows them.
/// </summary>
public sealed record OpenCommands(IAsyncRelayCommand OpenIfc, IAsyncRelayCommand OpenPackage);
