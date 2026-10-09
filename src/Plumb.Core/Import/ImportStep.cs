namespace Plumb.Core.Import
{
    public enum ImportStep
    {
        Validating,
        ReadingModel,
        ConvertingGeometry,
        WritingPackage,
        Finalizing,
        OpeningPackage,
    }
}
