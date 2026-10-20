using System;
using System.Threading;
using System.Threading.Tasks;
using Plumb.Core.Search;

namespace Plumb.Core.Clashes
{
    /// <summary>
    /// Finds clashes in a saved package between two groups of elements. Never throws for expected failures;
    /// returns <see cref="ClashResult.Failed"/> or <see cref="ClashResult.Cancelled"/>.
    /// </summary>
    public interface IClashService
    {
        /// <param name="first">The elements of the first group, usually what one search matched.</param>
        /// <param name="second">The elements of the second group.</param>
        Task<ClashResult> FindAsync(
            string packagePath,
            ElementMatches first,
            ElementMatches second,
            IProgress<ClashStep> progress,
            CancellationToken cancellationToken);
    }
}
