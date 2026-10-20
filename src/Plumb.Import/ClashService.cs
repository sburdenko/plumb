using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Plumb.Core.Clashes;
using Plumb.Core.Package;
using Plumb.Core.Search;
using Plumb.Package;

namespace Plumb.Import;

/// <summary>
/// Reads a package's <c>model.glb</c> once and runs <see cref="ClashDetector"/> on it, off the calling thread. The
/// shapes of the last package searched are kept, so changing the groups and searching again starts at once.
/// </summary>
public sealed class ClashService(ILogger<ClashService> logger) : IClashService
{
    /// <summary>Crossings no deeper than this are modelling joins, not clashes.</summary>
    public const float Tolerance = 0.01f;

    private readonly Lock _gate = new();
    private (string PackagePath, IReadOnlyList<ElementShape> Shapes)? _lastRead;

    public Task<ClashResult> FindAsync(
        string packagePath,
        ElementMatches first,
        ElementMatches second,
        IProgress<ClashStep> progress,
        CancellationToken cancellationToken) =>
        Task.Run(() => Find(packagePath, first, second, progress, cancellationToken), CancellationToken.None);

    private ClashResult Find(
        string packagePath,
        ElementMatches first,
        ElementMatches second,
        IProgress<ClashStep> progress,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var clock = Stopwatch.StartNew();
            var shapes = ShapesOf(packagePath, progress);
            cancellationToken.ThrowIfCancellationRequested();

            progress.Report(ClashStep.Comparing);
            var clashes = ClashDetector.Find(
                shapes.Where(shape => first.Contains(shape.GlobalId)).ToList(),
                shapes.Where(shape => second.Contains(shape.GlobalId)).ToList(),
                Tolerance,
                cancellationToken);
            logger.LogInformation("{Count} clashes between {First} and {Second} elements in {Elapsed}", clashes.Count, first.Count, second.Count, clock.Elapsed);
            return new ClashResult.Found(clashes, clock.Elapsed);
        }
        catch (OperationCanceledException)
        {
            return new ClashResult.Cancelled();
        }
        catch (FileNotFoundException)
        {
            return new ClashResult.Failed("This package has no 3D geometry to search for clashes.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PackageFormatException)
        {
            logger.LogWarning(ex, "Cannot read the geometry of {Package}", packagePath);
            return new ClashResult.Failed($"Cannot read {PackageLayout.GeometryFile}: {ex.Message}");
        }
    }

    private IReadOnlyList<ElementShape> ShapesOf(string packagePath, IProgress<ClashStep> progress)
    {
        lock (_gate)
        {
            if (_lastRead is { } last && last.PackagePath == packagePath)
            {
                return last.Shapes;
            }

            // Forget the previous package first, so two models' triangles are never held at once.
            _lastRead = null;
            progress.Report(ClashStep.ReadingGeometry);
            var shapes = GlbShapes.Read(Path.Combine(packagePath, PackageLayout.GeometryFile));
            _lastRead = (packagePath, shapes);
            return shapes;
        }
    }
}
