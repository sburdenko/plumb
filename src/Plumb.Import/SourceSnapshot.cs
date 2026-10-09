using System.Security.Cryptography;

namespace Plumb.Import;

/// <summary>
/// A private copy of the source IFC file. xBIM and IfcConvert both read the copy, so the model, the
/// geometry and the manifest hash describe the same bytes even if the original changes during import.
/// </summary>
internal static class SourceSnapshot
{
    private const int BlockSize = 1024 * 1024;

    /// <returns>Lowercase hex SHA-256 of the copied bytes, computed while copying.</returns>
    public static string Copy(string source, string destination, CancellationToken cancellationToken)
    {
        using var input = File.OpenRead(source);
        using var output = File.Create(destination);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[BlockSize];

        int read;
        while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            hash.AppendData(buffer, 0, read);
            output.Write(buffer, 0, read);
        }

        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }
}
