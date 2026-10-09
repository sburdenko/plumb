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

        public sealed record Success(IfcModelData Model, TimeSpan Duration) : ImportResult;

        public sealed record Failure(ImportError Error, string Message) : ImportResult;
    }
}
