using System;
using System.Threading;
using System.Threading.Tasks;

namespace Plumb.Core.Import
{
    /// <summary>
    /// Loads a model either by importing an IFC file or by opening a <c>.plumb</c> package.
    /// Never throws for expected failures; returns <see cref="ImportResult.Failure"/>.
    /// </summary>
    public interface IImportService
    {
        /// <summary>
        /// Reads an IFC file and writes <c>&lt;name&gt;.plumb</c> into <paramref name="outputDirectory"/>,
        /// replacing an existing package only once the new one is complete.
        /// </summary>
        Task<ImportResult> RunAsync(
            string ifcPath,
            string outputDirectory,
            IProgress<ImportProgress> progress,
            CancellationToken cancellationToken);

        /// <summary>
        /// Loads a package written by <see cref="RunAsync"/> without reading the IFC file again.
        /// </summary>
        Task<ImportResult> OpenPackageAsync(string packagePath, IProgress<ImportProgress> progress, CancellationToken cancellationToken);
    }
}
