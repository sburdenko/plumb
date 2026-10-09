using System;
using Plumb.Core.Model;

namespace Plumb.Core.Import
{
    /// <summary>
    /// Outcome of an import: either <see cref="Success"/> or <see cref="Failure"/>.
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
    }
}
