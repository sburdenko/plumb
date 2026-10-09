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
/// The import pipeline: validate, snapshot the IFC file, read it and convert its geometry side by side,
/// write a package draft, publish it. Each stage is done by its own component; this class only orders them
/// and maps failures.
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

            progress.Report(new ImportProgress(ImportStep.Snapshot, 0));
            var prepared = PrepareDraft(ifcPath, draft, cancellationToken);
            return prepared switch
            {
                PreparedDraft.Ready ready => await ImportIntoDraftAsync(ready, draft, packagePath, stopwatch, progress, cancellationToken)
                    .ConfigureAwait(false),
                PreparedDraft.Unavailable unavailable => ReadWithoutSaving(ifcPath, packagePath, unavailable.Reason, stopwatch, progress, cancellationToken),
                _ => throw new UnreachableException(),
            };
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
    /// Creates the draft folder and copies the source into it. Failing here only means the model cannot be
    /// saved (for example, a read-only folder); cancellation still cancels.
    /// </summary>
    private PreparedDraft PrepareDraft(string ifcPath, PackageDraft draft, CancellationToken cancellationToken)
    {
        try
        {
            Directory.CreateDirectory(draft.Location);
            Directory.CreateDirectory(draft.SourceFolder);
            var snapshot = Path.Combine(draft.SourceFolder, Path.GetFileName(ifcPath));
            var sha256 = SourceSnapshot.Copy(ifcPath, snapshot, cancellationToken);
            return new PreparedDraft.Ready(snapshot, sha256);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Cannot prepare a package draft next to {Path}", ifcPath);
            return new PreparedDraft.Unavailable(ex);
        }
    }

    /// <summary>
    /// Reads the model and converts its geometry at the same time, both from the snapshot, then saves the package.
    /// If reading fails or is cancelled, the conversion is stopped and awaited before the error propagates.
    /// </summary>
    private async Task<ImportResult> ImportIntoDraftAsync(
        PreparedDraft.Ready source,
        PackageDraft draft,
        string packagePath,
        Stopwatch stopwatch,
        IProgress<ImportProgress> progress,
        CancellationToken cancellationToken)
    {
        using var stopGeometry = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var glbPath = Path.Combine(draft.Location, PackageLayout.GeometryFile);
        var geometryTask = _geometry.ConvertAsync(source.SnapshotPath, glbPath, stopGeometry.Token);

        IfcModelData model;
        try
        {
            model = IfcModelReader.Read(source.SnapshotPath, new StepProgress(progress, ImportStep.ReadingModel, 0, ReadEnd), cancellationToken);
        }
        catch
        {
            await stopGeometry.CancelAsync().ConfigureAwait(false);
            await ObserveAsync(geometryTask).ConfigureAwait(false);
            throw;
        }

        progress.Report(new ImportProgress(ImportStep.ConvertingGeometry, ReadEnd));
        var geometry = await geometryTask.ConfigureAwait(false);
        LogGeometry(geometry, source.SnapshotPath);

        var manifest = CreateManifest(model, source.Sha256, stopwatch.Elapsed, geometry);
        var package = Save(model, manifest, geometry, draft, packagePath, progress, cancellationToken);
        var importDuration = package is PackageState.Saved ? TimeSpan.FromMilliseconds(manifest.ImportDurationMs) : stopwatch.Elapsed;
        return new ImportResult.Success(model, importDuration, package);
    }

    /// <summary>
    /// Writes the database, index and manifest into the draft and publishes it.
    /// </summary>
    private PackageState Save(
        IfcModelData model,
        PackageManifest manifest,
        GeometryState geometry,
        PackageDraft draft,
        string packagePath,
        IProgress<ImportProgress> progress,
        CancellationToken cancellationToken)
    {
        try
        {
            progress.Report(new ImportProgress(ImportStep.WritingPackage, GeometryEnd));
            var writeProgress = new StepProgress(progress, ImportStep.WritingPackage, GeometryEnd, WriteEnd);
            PackageWriter.Write(draft.Location, model, manifest, writeProgress, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            progress.Report(new ImportProgress(ImportStep.Finalizing, WriteEnd));
            draft.Publish();
            progress.Report(new ImportProgress(ImportStep.Finalizing, 100));
            return new PackageState.Saved(packagePath, geometry);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Model loaded but not saved to {Package}", packagePath);
            progress.Report(new ImportProgress(ImportStep.Finalizing, 100));
            return new PackageState.NotSaved(DescribeSaveFailure(packagePath, ex));
        }
    }

    /// <summary>
    /// No draft could be prepared, so the model is read straight from the source and shown without a package.
    /// </summary>
    private static ImportResult ReadWithoutSaving(
        string ifcPath,
        string packagePath,
        Exception reason,
        Stopwatch stopwatch,
        IProgress<ImportProgress> progress,
        CancellationToken cancellationToken)
    {
        var model = IfcModelReader.Read(ifcPath, new StepProgress(progress, ImportStep.ReadingModel, 0, ReadEnd), cancellationToken);
        progress.Report(new ImportProgress(ImportStep.Finalizing, 100));
        var package = new PackageState.NotSaved(DescribeSaveFailure(packagePath, reason));
        return new ImportResult.Success(model, stopwatch.Elapsed, package);
    }

    private async Task ObserveAsync(Task<GeometryState> geometryTask)
    {
        try
        {
            await geometryTask.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Geometry conversion stopped after reading failed");
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
    /// A short reason for the user; the exception, with internal draft paths, goes to the log.
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

    private static PackageManifest CreateManifest(IfcModelData model, string sourceSha256, TimeSpan importDuration, GeometryState geometry)
    {
        var notBuilt = geometry as GeometryState.NotBuilt;
        return new PackageManifest(
            PackageLayout.FormatVersion,
            model.SourceFile,
            sourceSha256,
            model.IfcSchema,
            DateTime.UtcNow,
            model.Elements.Count,
            (long)importDuration.TotalMilliseconds,
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

    /// <summary>Whether the draft folder and the source snapshot could be created.</summary>
    private abstract record PreparedDraft
    {
        private PreparedDraft()
        {
        }

        public sealed record Ready(string SnapshotPath, string Sha256) : PreparedDraft;

        public sealed record Unavailable(Exception Reason) : PreparedDraft;
    }
}
