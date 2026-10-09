namespace Plumb.Core.Geometry
{
    /// <summary>
    /// Whether a package has its 3D geometry. Geometry can fail on its own without affecting the model.
    /// </summary>
    public abstract record GeometryState
    {
        private GeometryState()
        {
        }

        public sealed record Built : GeometryState;

        /// <param name="Reason">A message for the user.</param>
        public sealed record NotBuilt(GeometryError Error, string Reason) : GeometryState;
    }
}
