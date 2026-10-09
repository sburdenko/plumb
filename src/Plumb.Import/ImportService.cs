using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Plumb.Core.Geometry;
using Plumb.Core.Import;
using Plumb.Core.Model;
using Plumb.Core.Package;
using Plumb.Geometry;
using Plumb.Ifc;
using Plumb.Package;

namespace Plumb.Import;

/// <summary>
/// The import pipeline: validate, read the IFC file, convert its geometry, write a package draft, publish it.
/// Each stage is done by its own component; this class only orders them and maps failures.
/// </summary>
public sealed class ImportService : IImportService
{
    private const int ReadEnd = 60;
    private const int GeometryEnd = 85;
    private const int WriteEnd = 99;

    private readonly ILogger<ImportService> _logger;
    private readonly IGeometryConverter _geometry;

    public ImportService(ILogger<ImportService> logger, IGeometryConverter geometry)
    {
        _logger = logger;
        _geometry = geometry;
    }

    /// <inheritdoc />
    public async Task<ImportResult> RunAsync(
        string ifcPath,
        string outputDirectory,
        IProgress<ImportProgress> progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(progress);
        _logger.LogInformation("Import started: {Path}", ifcPath);
        var stopwatch = Stopwatch.StartNew();
        var packagePath = PackageLayout.PackagePathFor(ifcPath, outputDirectory);
        using var draft = new PackageDraft(packagePath, _logger);

        async Task<ImportResult> Import()
        {
            progress.Report(new ImportProgress(ImportStep.Validating, 0));
            if (IfcFileValidator.Validate(ifcPath) is { } invalid)
            {
                return invalid;
            }

            // Hashed before parsing so the manifest describes the bytes the model was read from.
            var sourceSha256 = SourceHash.Sha256(ifcPath, cancellationToken);
            var model = IfcModelReader.Read(ifcPath, new StepProgress(progress, ImportStep.ReadingModel, 0, ReadEnd), cancellationToken);
            var source = new SourceFile(ifcPath, sourceSha256, model, stopwatch);

            var saved = await SaveAsync(source, draft, packagePath, progress, cancellationToken).ConfigureAwait(false);
            return new ImportResult.Success(model, saved.ImportDuration, saved.Package);
        }

        var result = await RunInBackgroundAsync(Import, ifcPath, ImportError.ParseFailed, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Import finished in {Elapsed} ms: {Result}", stopwatch.ElapsedMilliseconds, Describe(result));
        return result;
    }

    /// <inheritdoc />
    public async Task<ImportResult> OpenPackageAsync(string packagePath, IProgress<ImportProgress> progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(progress);
        _logger.LogInformation("Opening package: {Path}", packagePath);

        Task<ImportResult> Open()
        {
            progress.Report(new ImportProgress(ImportStep.OpeningPackage, 0));
            var contents = PackageReader.Read(packagePath, cancellationToken);
            progress.Report(new ImportProgress(ImportStep.OpeningPackage, 100));

            var importDuration = TimeSpan.FromMilliseconds(contents.Manifest.ImportDurationMs);
            var package = new PackageState.Saved(packagePath, contents.Geometry);
            return Task.FromResult<ImportResult>(new ImportResult.Success(contents.Model, importDuration, package));
        }

        var result = await RunInBackgroundAsync(Open, packagePath, ImportError.PackageInvalid, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Package opened: {Result}", Describe(result));
        return result;
    }

    /// <summary>
    /// Converts geometry into the draft, writes the package and publishes it. Failing to save only means
    /// the model is not saved; the model itself was read and is still returned. Cancellation still cancels.
    /// </summary>
    private async Task<SaveOutcome> SaveAsync(
        SourceFile source,
        PackageDraft draft,
        string packagePath,
        IProgress<ImportProgress> progress,
        CancellationToken cancellationToken)
    {
        try
        {
            Directory.CreateDirectory(draft.Location);

            progress.Report(new ImportProgress(ImportStep.ConvertingGeometry, ReadEnd));
            var glbPath = Path.Combine(draft.Location, PackageLayout.GeometryFile);
            var geometry = await _geometry.ConvertAsync(source.Path, glbPath, cancellationToken).ConfigureAwait(false);
            LogGeometry(geometry, source.Path);
            var manifest = CreateManifest(source, geometry);

            progress.Report(new ImportProgress(ImportStep.WritingPackage, GeometryEnd));
            var writeProgress = new StepProgress(progress, ImportStep.WritingPackage, GeometryEnd, WriteEnd);
            PackageWriter.Write(draft.Location, source.Model, manifest, writeProgress, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            progress.Report(new ImportProgress(ImportStep.Finalizing, WriteEnd));
            draft.Publish();
            progress.Report(new ImportProgress(ImportStep.Finalizing, 100));
            return new SaveOutcome(new PackageState.Saved(packagePath, geometry), TimeSpan.FromMilliseconds(manifest.ImportDurationMs));
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Model loaded but not saved to {Package}", packagePath);
            progress.Report(new ImportProgress(ImportStep.Finalizing, 100));
            return new SaveOutcome(new PackageState.NotSaved(DescribeSaveFailure(packagePath, ex)), source.Stopwatch.Elapsed);
        }
    }

    private void LogGeometry(GeometryState geometry, string ifcPath)
    {
        if (geometry is GeometryState.NotBuilt notBuilt)
        {
            _logger.LogWarning("Geometry not built for {Path} ({Error}): {Detail}", ifcPath, notBuilt.Error, notBuilt.Detail);
        }
    }

    /// <summary>
    /// A short reason for the user; the exception, with the internal draft path, goes to the log.
    /// </summary>
    private static string DescribeSaveFailure(string packagePath, Exception error)
    {
        var name = Path.GetFileName(packagePath);
        var folder = Path.GetFileName(Path.GetDirectoryName(packagePath));
        var reason = error is UnauthorizedAccessException
            ? $"the folder {folder} is read-only."
            : "the folder next to the source file could not be written to.";
        return $"{name} was not saved: {reason}";
    }

    private static PackageManifest CreateManifest(SourceFile source, GeometryState geometry)
    {
        var notBuilt = geometry as GeometryState.NotBuilt;
        return new PackageManifest(
            PackageLayout.FormatVersion,
            source.Model.SourceFile,
            source.Sha256,
            source.Model.IfcSchema,
            DateTime.UtcNow,
            source.Model.Elements.Count,
            (long)source.Stopwatch.Elapsed.TotalMilliseconds,
            notBuilt?.Error,
            notBuilt?.Detail);
    }

    /// <summary>
    /// Runs the work off the caller's thread and maps every exception to a failure.
    /// </summary>
    /// <param name="unknownError">The error reported for exceptions that are not I/O or cancellation.</param>
    private async Task<ImportResult> RunInBackgroundAsync(
        Func<Task<ImportResult>> work,
        string path,
        ImportError unknownError,
        CancellationToken cancellationToken)
    {
        var name = Path.GetFileName(path);
        try
        {
            return await Task.Run(work, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested)
        {
            // xBIM wraps exceptions thrown from its progress callback, so the cancellation may not surface as OperationCanceledException.
            return new ImportResult.Failure(ImportError.Cancelled, "Import was cancelled.");
        }
        catch (DirectoryNotFoundException)
        {
            return new ImportResult.Failure(ImportError.FileNotFound, $"Not found: {path}");
        }
        catch (PackageFormatException ex)
        {
            _logger.LogWarning(ex, "Invalid package {Path}", path);
            return new ImportResult.Failure(ImportError.PackageInvalid, $"{name} is not a valid Plumb package: {ex.Message}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "I/O error with {Path}", path);
            return new ImportResult.Failure(ImportError.IoError, $"Cannot access {name}: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load {Path}", path);
            return new ImportResult.Failure(unknownError, $"Cannot load {name}: {ex.Message}");
        }
    }

    private static string Describe(ImportResult result) => result switch
    {
        ImportResult.Success success => $"{success.Model.Elements.Count} elements, {success.Model.Properties.Count} properties, {success.Package}",
        ImportResult.Failure failure => $"{failure.Error}: {failure.Message}",
        _ => throw new UnreachableException(),
    };

    /// <summary>The IFC file being imported, with what has been read from it so far.</summary>
    private sealed record SourceFile(string Path, string Sha256, IfcModelData Model, Stopwatch Stopwatch);

    private sealed record SaveOutcome(PackageState Package, TimeSpan ImportDuration);
}
