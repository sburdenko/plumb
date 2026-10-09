using Plumb.Core.Geometry;

namespace Plumb.Core.Import
{
    /// <summary>
    /// Whether a loaded model is backed by a <c>.plumb</c> package on disk.
    /// Saving can fail on its own (for example, next to a read-only file) without affecting the model.
    /// </summary>
    public abstract record PackageState
    {
        private PackageState()
        {
        }

        /// <param name="Geometry">Whether the package has its <c>model.glb</c>.</param>
        public sealed record Saved(string Path, GeometryState Geometry) : PackageState;

        /// <param name="Reason">A message for the user.</param>
        public sealed record NotSaved(string Reason) : PackageState;
    }
}
