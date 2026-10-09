namespace Plumb.Core.Import
{
    public enum ImportStep
    {
        Validating,
        Snapshot,
        ReadingModel,
        ConvertingGeometry,
        WritingPackage,
        Finalizing,
        OpeningPackage,
    }
}
