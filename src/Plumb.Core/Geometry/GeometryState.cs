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

        /// <param name="NodeCount">Nodes in the geometry file, or null until a package has been read.</param>
        public sealed record Built(int? NodeCount = null) : GeometryState;

        /// <param name="Detail">
        /// Technical detail such as IfcConvert's last error line; may be empty. Not a sentence for the user:
        /// the app words the message from <paramref name="Error"/>.
        /// </param>
        public sealed record NotBuilt(GeometryError Error, string Detail) : GeometryState;
    }
}
