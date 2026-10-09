using Plumb.Core.Import;

namespace Plumb.Import;

/// <summary>
/// Maps a component's own 0..100 progress onto a slice of the overall import, on the reporting thread.
/// </summary>
internal sealed class StepProgress(IProgress<ImportProgress> target, ImportStep step, int from, int to) : IProgress<int>
{
    public void Report(int value) =>
        target.Report(new ImportProgress(step, from + (Math.Clamp(value, 0, 100) * (to - from) / 100)));
}
