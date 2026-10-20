namespace Plumb.App.Screens.Model.Clashes;

/// <summary>The model has no 3D geometry to search, so the panel says why instead of offering a search.</summary>
public sealed class ClashesUnavailableViewModel(string reason) : ClashesStateViewModel
{
    public string Reason { get; } = reason;
}
