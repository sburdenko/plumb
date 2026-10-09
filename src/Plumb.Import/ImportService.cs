using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Plumb.Core.Import;
using Plumb.Ifc;

namespace Plumb.Import;

public sealed class ImportService : IImportService
{
    private readonly ILogger<ImportService> _logger;

    public ImportService(ILogger<ImportService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ImportResult> RunAsync(string ifcPath, IProgress<ImportProgress> progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(progress);
        _logger.LogInformation("Import started: {Path}", ifcPath);
        var stopwatch = Stopwatch.StartNew();

        ImportResult Import()
        {
            progress.Report(new ImportProgress(ImportStep.Validating, 0));
            if (IfcFileValidator.Validate(ifcPath) is { } invalid)
            {
                return invalid;
            }

            var model = IfcModelReader.Read(ifcPath, new StepProgress(progress, ImportStep.ReadingModel), cancellationToken);
            return new ImportResult.Success(model, stopwatch.Elapsed);
        }

        var result = await RunInBackgroundAsync(Import, ifcPath, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Import finished in {Elapsed} ms: {Result}", stopwatch.ElapsedMilliseconds, Describe(result));
        return result;
    }

    /// <summary>
    /// Runs the whole import, validation included, off the caller's thread and maps every exception to a failure.
    /// </summary>
    private async Task<ImportResult> RunInBackgroundAsync(Func<ImportResult> import, string ifcPath, CancellationToken cancellationToken)
    {
        try
        {
            return await Task.Run(import, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested)
        {
            // xBIM wraps exceptions thrown from its progress callback, so the cancellation may not surface as OperationCanceledException.
            return new ImportResult.Failure(ImportError.Cancelled, "Import was cancelled.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "I/O error while reading {Path}", ifcPath);
            return new ImportResult.Failure(ImportError.IoError, $"Cannot read {Path.GetFileName(ifcPath)}: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse {Path}", ifcPath);
            return new ImportResult.Failure(ImportError.ParseFailed, $"Cannot parse {Path.GetFileName(ifcPath)}: {ex.Message}");
        }
    }

    private static string Describe(ImportResult result) => result switch
    {
        ImportResult.Success success => $"{success.Model.Elements.Count} elements, {success.Model.Properties.Count} properties",
        ImportResult.Failure failure => $"{failure.Error}: {failure.Message}",
        _ => throw new UnreachableException(),
    };
}
