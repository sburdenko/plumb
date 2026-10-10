using System;
using Plumb.Core.Model;

namespace Plumb.Core.Import
{
    /// <summary>
    /// Outcome of an import: <see cref="Success"/>, <see cref="Failure"/>, or <see cref="PackageOutdated"/> when a package
    /// has to be imported again from its IFC file.
    /// </summary>
    public abstract record ImportResult
    {
        private ImportResult()
        {
        }

        /// <param name="ImportDuration">How long reading the IFC file took; for an opened package, the value stored in it.</param>
        /// <param name="Package">Where the model was saved, or why it could not be.</param>
        public sealed record Success(IfcModelData Model, TimeSpan ImportDuration, PackageState Package) : ImportResult;

        public sealed record Failure(ImportError Error, string Message) : ImportResult;

        /// <param name="SourceFile">The IFC file name the package was made from.</param>
        /// <param name="SourcePath">That file next to the package, or null when it is no longer there.</param>
        public sealed record PackageOutdated(string SourceFile, string? SourcePath) : ImportResult;
    }
}
