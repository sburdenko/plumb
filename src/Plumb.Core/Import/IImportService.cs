using System;
using System.Threading;
using System.Threading.Tasks;

namespace Plumb.Core.Import
{
    /// <summary>
    /// Turns an IFC file into <see cref="Model.IfcModelData"/>.
    /// </summary>
    public interface IImportService
    {
        /// <summary>
        /// Reads an IFC file into memory. Never throws for expected failures; returns <see cref="ImportResult.Failure"/>.
        /// </summary>
        Task<ImportResult> RunAsync(string ifcPath, IProgress<ImportProgress> progress, CancellationToken cancellationToken);
    }
}
