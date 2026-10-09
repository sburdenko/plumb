using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Plumb.Core.Import;
using Plumb.Core.Model;
using Plumb.Core.Package;
using Plumb.Ifc;
using Plumb.Package;

namespace Plumb.Import;

/// <summary>
/// The import pipeline: validate, read the IFC file, write a package draft, publish it.
/// Each stage is done by its own component; this class only orders them and maps failures.
/// </summary>
public sealed class ImportService : IImportService
{
    private const int ReadEnd = 90;
    private const int WriteEnd = 99;

    private readonly ILogger<ImportService> _logger;

    public ImportService(ILogger<ImportService> logger)
    {
        _logger = logger;
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

        ImportResult Import()
        {
            progress.Report(new ImportProgress(ImportStep.Validating, 0));
            if (IfcFileValidator.Validate(ifcPath) is { } invalid)
            {
                return invalid;
            }

            var model = IfcModelReader.Read(ifcPath, new StepProgress(progress, ImportStep.ReadingModel, 0, ReadEnd), cancellationToken);
            var importDuration = stopwatch.Elapsed;

            progress.Report(new ImportProgress(ImportStep.WritingPackage, ReadEnd));
            var manifest = CreateManifest(ifcPath, model, importDuration, cancellationToken);
            PackageWriter.Write(draft.Location, model, manifest, new StepProgress(progress, ImportStep.WritingPackage, ReadEnd, WriteEnd), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            progress.Report(new ImportProgress(ImportStep.Finalizing, WriteEnd));
            draft.Publish();
            progress.Report(new ImportProgress(ImportStep.Finalizing, 100));

            return new ImportResult.Success(model, TimeSpan.FromMilliseconds(manifest.ImportDurationMs), packagePath);
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

        ImportResult Open()
        {
            progress.Report(new ImportProgress(ImportStep.OpeningPackage, 0));
            var contents = PackageReader.Read(packagePath, cancellationToken);
            progress.Report(new ImportProgress(ImportStep.OpeningPackage, 100));

            var importDuration = TimeSpan.FromMilliseconds(contents.Manifest.ImportDurationMs);
            return new ImportResult.Success(contents.Model, importDuration, packagePath);
        }

        var result = await RunInBackgroundAsync(Open, packagePath, ImportError.PackageInvalid, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Package opened: {Result}", Describe(result));
        return result;
    }

    private static PackageManifest CreateManifest(
        string ifcPath,
        IfcModelData model,
        TimeSpan importDuration,
        CancellationToken cancellationToken) =>
        new(
            PackageLayout.FormatVersion,
            model.SourceFile,
            SourceHash.Sha256(ifcPath, cancellationToken),
            model.IfcSchema,
            DateTime.UtcNow,
            model.Elements.Count,
            (long)importDuration.TotalMilliseconds);

    /// <summary>
    /// Runs the work off the caller's thread and maps every exception to a failure.
    /// </summary>
    /// <param name="unknownError">The error reported for exceptions that are not I/O or cancellation.</param>
    private async Task<ImportResult> RunInBackgroundAsync(
        Func<ImportResult> work,
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
        ImportResult.Success success => $"{success.Model.Elements.Count} elements, {success.Model.Properties.Count} properties",
        ImportResult.Failure failure => $"{failure.Error}: {failure.Message}",
        _ => throw new UnreachableException(),
    };
}
