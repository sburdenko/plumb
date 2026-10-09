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

        public sealed record Saved(string Path) : PackageState;

        /// <param name="Reason">A message for the user.</param>
        public sealed record NotSaved(string Reason) : PackageState;
    }
}
