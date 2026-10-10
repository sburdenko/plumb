namespace Plumb.App.Screens.Model.Viewport;

public sealed class ViewportNotSavedViewModel(string reason) : ViewportViewModel
{
    public override string GeometryInfo => "no package";

    public string Reason { get; } = reason;
}
