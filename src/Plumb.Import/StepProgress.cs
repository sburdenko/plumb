using Plumb.Core.Import;

namespace Plumb.Import;

/// <summary>
/// Forwards a component's own 0..100 progress as one step of the import, on the reporting thread.
/// </summary>
internal sealed class StepProgress(IProgress<ImportProgress> target, ImportStep step) : IProgress<int>
{
    public void Report(int value) => target.Report(new ImportProgress(step, value));
}
