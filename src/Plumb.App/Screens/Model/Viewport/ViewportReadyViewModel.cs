using System.Globalization;
using CommunityToolkit.Mvvm.Input;

namespace Plumb.App.Screens.Model.Viewport;

public sealed class ViewportReadyViewModel(int? nodeCount, IRelayCommand openIn3D) : ViewportViewModel
{
    public override string GeometryInfo => nodeCount is { } count
        ? string.Format(CultureInfo.InvariantCulture, "model.glb · {0:N0} nodes", count)
        : "model.glb";

    public string Detail => nodeCount is { } count
        ? string.Format(CultureInfo.InvariantCulture, "model.glb is ready · {0:N0} nodes", count)
        : "model.glb is ready";

    public IRelayCommand OpenIn3D { get; } = openIn3D;
}
