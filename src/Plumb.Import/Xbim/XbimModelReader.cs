using Plumb.Core.Import;
using Plumb.Core.Model;
using Xbim.Ifc4.Interfaces;
using Xbim.IO.Memory;

namespace Plumb.Import.Xbim;

/// <summary>
/// Reads an IFC STEP file fully into memory. Uses <see cref="MemoryModel"/> directly because
/// the default IfcStore provider may pick the Esent store, which only runs on Windows.
/// </summary>
internal static class XbimModelReader
{
    private const int ParseShare = 80;

    /// <exception cref="ModelParseException">The file has no readable IfcProject.</exception>
    /// <exception cref="OperationCanceledException">Cancellation was requested.</exception>
    public static IfcModelData Read(string ifcPath, IProgress<ImportProgress> progress, CancellationToken cancellationToken)
    {
        var lastPercent = -1;
        void Report(ImportStep step, int percent)
        {
            if (percent != lastPercent)
            {
                lastPercent = percent;
                progress.Report(new ImportProgress(step, percent));
            }
        }

        Report(ImportStep.ReadingModel, 0);
        using var model = MemoryModel.OpenRead(ifcPath, (percent, _) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            Report(ImportStep.ReadingModel, Scale(percent, 0, ParseShare));
        });
        cancellationToken.ThrowIfCancellationRequested();

        var project = model.Instances.FirstOrDefault<IIfcProject>()
            ?? throw new ModelParseException("The file contains no IfcProject.");

        Report(ImportStep.CollectingData, ParseShare);
        var entries = SpatialStructureReader.Read(project, cancellationToken);

        var extractor = new PropertyExtractor(ProjectUnits.From(project));
        var properties = new List<PropertyRecord>();
        for (var i = 0; i < entries.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            properties.AddRange(extractor.Extract(entries[i].Source));
            Report(ImportStep.CollectingData, Scale((i + 1) * 100 / entries.Count, ParseShare, 100));
        }

        return new IfcModelData(
            Path.GetFileName(ifcPath),
            string.Join(",", model.Header.FileSchema.Schemas),
            entries.Select(entry => entry.Record).ToList(),
            properties);
    }

    private static int Scale(int percent, int from, int to) =>
        from + (Math.Clamp(percent, 0, 100) * (to - from) / 100);
}
