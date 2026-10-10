using CommunityToolkit.Mvvm.Input;

namespace Plumb.App.Shell;

/// <summary>
/// The ways to open a model, shared by every window state that offers them.
/// </summary>
/// <param name="OpenPath">Opens a .plumb package as is, or imports anything else as IFC.</param>
public sealed record OpenCommands(IAsyncRelayCommand OpenIfc, IAsyncRelayCommand OpenPackage, IAsyncRelayCommand<string?> OpenPath);
