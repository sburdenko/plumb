using System.Security.Cryptography;

namespace Plumb.Import;

internal static class SourceHash
{
    public static string Sha256(string path, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var stream = File.OpenRead(path);
        return Convert.ToHexStringLower(SHA256.HashData(stream));
    }
}
