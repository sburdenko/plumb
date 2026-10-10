using Plumb.App.Shell;

namespace Plumb.App.Screens.Model.Viewport;

/// <summary>What the 3D area shows: the geometry is ready, was not built, or there is no package.</summary>
public abstract class ViewportViewModel : ViewModelBase
{
    /// <summary>Right side of the viewport toolbar, e.g. "model.glb · 215 nodes".</summary>
    public abstract string GeometryInfo { get; }
}
