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
        /// <param name="PackagePath">The <c>.plumb</c> folder that holds this model.</param>
        public sealed record Success(IfcModelData Model, TimeSpan ImportDuration, string PackagePath) : ImportResult;

        public sealed record Failure(ImportError Error, string Message) : ImportResult;
    }
}
