using System.Text;
using Plumb.Core.Import;

namespace Plumb.Ifc;

public static class IfcFileValidator
{
    private const string IfcExtension = ".ifc";
    private const string StepMagic = "ISO-10303-21";
    private const char ByteOrderMark = '\uFEFF';

    /// <returns>A failure, or null when the file looks like a readable IFC STEP file.</returns>
    public static ImportResult.Failure? Validate(string ifcPath)
    {
        if (Directory.Exists(ifcPath))
        {
            return new ImportResult.Failure(
                ImportError.NotIfc,
                $"{Path.GetFileName(Path.TrimEndingDirectorySeparator(ifcPath))} is a folder, not an IFC file or a .plumb package.");
        }

        if (string.IsNullOrWhiteSpace(ifcPath) || !File.Exists(ifcPath))
        {
            return new ImportResult.Failure(ImportError.FileNotFound, $"File not found: {ifcPath}");
        }

        if (!string.Equals(Path.GetExtension(ifcPath), IfcExtension, StringComparison.OrdinalIgnoreCase))
        {
            return new ImportResult.Failure(ImportError.NotIfc, $"Not an .ifc file: {Path.GetFileName(ifcPath)}");
        }

        try
        {
            return HasStepHeader(ifcPath)
                ? null
                : new ImportResult.Failure(ImportError.NotIfc, $"{Path.GetFileName(ifcPath)} is not an IFC STEP file.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new ImportResult.Failure(ImportError.IoError, $"Cannot read {Path.GetFileName(ifcPath)}: {ex.Message}");
        }
    }

    private static bool HasStepHeader(string ifcPath)
    {
        using var stream = new FileStream(ifcPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var buffer = new byte[256];
        var read = stream.Read(buffer, 0, buffer.Length);
        var head = Encoding.UTF8.GetString(buffer, 0, read).TrimStart(ByteOrderMark, ' ', '\t', '\r', '\n');
        return head.StartsWith(StepMagic, StringComparison.Ordinal);
    }
}
