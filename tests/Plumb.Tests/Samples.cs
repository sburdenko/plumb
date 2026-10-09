namespace Plumb.Tests;

internal static class Samples
{
    public static string Duplex
    {
        get
        {
            var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Samples", "Duplex.ifc");
            return File.Exists(path)
                ? path
                : throw new FileNotFoundException("Sample model missing. Run samples/fetch-samples.sh, then rebuild.", path);
        }
    }
}
