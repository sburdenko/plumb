using Plumb.Core.Model;
using Xbim.Ifc4.Interfaces;
using Xbim.IO.Memory;

namespace Plumb.Ifc;

/// <summary>
/// Reads an IFC STEP file fully into memory. Uses <see cref="MemoryModel"/> directly because
/// the default IfcStore provider may pick the Esent store, which only runs on Windows.
/// </summary>
public static class IfcModelReader
{
    private const int ParseShare = 80;

    /// <param name="progress">Receives 0..100 for this read only.</param>
    /// <exception cref="OperationCanceledException">Cancellation was requested.</exception>
    /// <exception cref="Exception">The file cannot be parsed or has no IfcProject.</exception>
    public static IfcModelData Read(string ifcPath, IProgress<int> progress, CancellationToken cancellationToken)
    {
        var lastPercent = -1;
        void Report(int percent)
        {
            if (percent != lastPercent)
            {
                lastPercent = percent;
                progress.Report(percent);
            }
        }

        Report(0);
        using var model = MemoryModel.OpenRead(ifcPath, (percent, _) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            Report(Scale(percent, 0, ParseShare));
        });
        cancellationToken.ThrowIfCancellationRequested();

        var project = model.Instances.FirstOrDefault<IIfcProject>()
            ?? throw new IfcParseException("The file contains no IfcProject.");

        var entries = SpatialStructureReader.Read(project, cancellationToken);

        var extractor = new PropertyExtractor(ProjectUnits.From(project));
        var properties = new List<PropertyRecord>();
        for (var i = 0; i < entries.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            properties.AddRange(extractor.Extract(entries[i].Source));
            Report(Scale((i + 1) * 100 / entries.Count, ParseShare, 100));
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
