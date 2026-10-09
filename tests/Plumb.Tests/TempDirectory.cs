namespace Plumb.Tests;

internal sealed class TempDirectory : IDisposable
{
    public TempDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "plumb-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string WriteFile(string name, string contents)
    {
        var filePath = System.IO.Path.Combine(Path, name);
        File.WriteAllText(filePath, contents);
        return filePath;
    }

    public void Dispose() => Directory.Delete(Path, recursive: true);
}
