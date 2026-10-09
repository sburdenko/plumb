using System.Security.Cryptography;

namespace Plumb.Import;

internal static class SourceHash
{
    private const int BlockSize = 1024 * 1024;

    /// <returns>Lowercase hex SHA-256 of the file, read in blocks so cancellation is checked as it goes.</returns>
    public static string Sha256(string path, CancellationToken cancellationToken)
    {
        using var stream = File.OpenRead(path);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[BlockSize];

        int read;
        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            hash.AppendData(buffer, 0, read);
        }

        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }
}
