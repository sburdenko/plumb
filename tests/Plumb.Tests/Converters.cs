namespace Plumb.Tests;

internal static class Converters
{
    public static string IfcConvert
    {
        get
        {
            var name = OperatingSystem.IsWindows() ? "IfcConvert.exe" : "IfcConvert";
            var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "tools", "ifcconvert", name);
            return File.Exists(path)
                ? path
                : throw new FileNotFoundException("IfcConvert missing. Run tools/fetch-ifcconvert.sh, then rebuild.", path);
        }
    }
}
