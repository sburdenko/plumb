namespace Plumb.Core.Import
{
    /// <param name="Percent">Overall progress of the whole import, 0..100.</param>
    public sealed record ImportProgress(ImportStep Step, int Percent);
}
